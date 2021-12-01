
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using Utf8Json;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq; //"C:\Program Files\Unity\Editor\Data\MonoBleedingEdge\lib\mono\unityjit/*.dll"
using System.Net.Mime;
using System.Runtime.InteropServices;
using static WAPIClient;
using Random = UnityEngine.Random;

public class SimulationController : MonoBehaviour
{
    /*[SerializeField] 
    GameObject cameraObject;
    Camera camera;*/

    [Header("Post processing")]
    PostProcessDebug postDebug;
    PostProcessLayer postLayer;
    PostProcessVolume postVolume;

    [Header("Lights")]
    [SerializeField]
    Light light1, light2;

    [SerializeField]
    public List<GameObject> knownObjects;

    [SerializeField]
    public GameObject robotPrefab;

    GameObject waterContainer;
    GameObject robot;
    
    public RobotController robotController;

    //WAPI
    public byte[] depthMapBytes;
    public bool getDepthImage, depthImageReady, acceptNewClients;

    //readonly object jsonDictionaryLock = new object();
    public readonly ConcurrentDictionary<int, WAPIClient> wapiClients = new ConcurrentDictionary<int, WAPIClient>();
    Thread wapiThread;
    //

    public ConcurrentQueue<MainThreadUpdateWorker> mainThreadUpdateWorkers;

    void Start()
    {
        QualitySettings.vSyncCount = 0;  // VSync must be disabled
        Application.targetFrameRate = 35;
        
        
        mainThreadUpdateWorkers = new ConcurrentQueue<MainThreadUpdateWorker>();
        if (Settings.config == null || Settings.config.mode == null)
        {
            SceneManager.LoadScene("Start");
            return;//TODO take config
        }

        string selectedWaterContainer = Settings.config.simulationOptions.selectedWaterContainer;
        if (TryGetObjectByTypeName(selectedWaterContainer, out GameObject waterContainerPrefab)) waterContainer = Instantiate(waterContainerPrefab, Vector3.zero, Quaternion.identity);

        robot = Instantiate(robotPrefab, new Vector3(0, 2, 0), Quaternion.identity);
        robotController = robot.GetComponent<RobotController>();
        PlaceRobotInStartZone();

        List<string> selectedRandomObjectsNames = Settings.config.simulationOptions.selectedRandomObjects;
        foreach (string randomObjectName in selectedRandomObjectsNames)
        {
            if (TryGetObjectByTypeName(randomObjectName, out GameObject randomObjectPrefab))
            {
                var randomObject = Instantiate(randomObjectPrefab);
                randomObject.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0f, 0f));
                randomObject.transform.position = DatasetObjectInfo.GetRandomWorldPosistionInBoundary(randomObject, waterContainer);
            }
            else
            {
                Debug.LogError(randomObjectName);
            }
        }
        Time.fixedDeltaTime = Settings.config.simulationOptions.fixedDeltaTime;
        /*
        camera = Camera.main;
        postDebug = cameraObject.GetComponent<PostProcessDebug>();
        postLayer = cameraObject.GetComponent<PostProcessLayer>();
        postVolume = cameraObject.GetComponent<PostProcessVolume>();
        SetGraphics();*/

        bool TryGetObjectByTypeName(string typeName, out GameObject obj)
        {
            obj = null;
            for (int i = 0; i < knownObjects.Count; i++)
            {
                if (knownObjects[i].GetComponent<DatasetObjectInfo>().typeName.ToLower() == typeName.ToLower())
                {
                    obj = knownObjects[i];
                    return true;
                }
            }
            return false;
        }

        StartCoroutine(StartCapture());

        wapiThread = new Thread(WAPIRecv) { IsBackground = true };
        wapiThread.Start();
        JsonSerializer.ToJsonString(GetDetection()); //No idea why first first call takes 1 second, leave it for performance
    }

    public void PlaceRobotInStartZone()
    {
        var startZones = GameObject.FindGameObjectsWithTag("StartZone");
        if (startZones.Length > 0)
        {
            System.Random r = new System.Random();
            int i = r.Next(0, startZones.Length);
            var startZone = startZones[i];

            DatasetObjectInfo elementInfo = robot.GetComponent<DatasetObjectInfo>();
            var elementSize = elementInfo.GetBoundarySize();
            var bounds = startZone.GetComponent<Collider>().bounds;

            var containerMin = bounds.min;
            var containerMax = bounds.max;

            Vector3 transformedCenterOffset = robot.transform.TransformDirection(elementInfo.center);
            float x = Random.Range(containerMin.x + elementSize.x / 2f - transformedCenterOffset.x, containerMax.x - elementSize.x / 2f - transformedCenterOffset.x);
            float y = Random.Range(containerMin.y + elementSize.y / 2f - transformedCenterOffset.y, containerMax.y - elementSize.y / 2f - transformedCenterOffset.y);
            float z = Random.Range(containerMin.z + elementSize.z / 2f - transformedCenterOffset.z, containerMax.z - elementSize.z / 2f - transformedCenterOffset.z);
            if (-(containerMin.y + elementSize.y / 2f - transformedCenterOffset.y) + (containerMax.y - elementSize.y / 2f - transformedCenterOffset.y) < 0)
                y = containerMin.y + elementSize.y / 2f - transformedCenterOffset.y;
            robot.transform.position = new Vector3(x, 1, z);
            float direction = startZone.GetComponent<StartZoneController>().angle;
            float fov = startZone.GetComponent<StartZoneController>().fov;
            robot.transform.rotation = Quaternion.Euler(0, Random.Range(direction - fov / 2, direction + fov / 2), 0);
        }

        foreach(var cp in GameObject.FindGameObjectsWithTag("Checkpoint"))
            cp.GetComponent<CheckpointController>().reached = false;

        robotController.rb.velocity = Vector3.zero;
        robotController.rb.angularVelocity = Vector3.zero;
    }

    IEnumerator StartCapture()
    {
        yield return new WaitUntil(() => robotController && robotController.depthCamera.targetTexture && robotController.colorCamera.targetTexture);
        float depthScale = Settings.config.simulationOptions.depthMapScale;
        float videoScale = Settings.config.simulationOptions.videoFeedScale;
        
        if (robotController.depthCamera.targetTexture != null) robotController.depthCamera.activeTexture.Release();
        robotController.depthCamera.targetTexture = new RenderTexture((int) Mathf.Round(1280 * depthScale), (int) Mathf.Round(720 * depthScale), 32, RenderTextureFormat.BGRA32);
        if (robotController.colorCamera.targetTexture != null) robotController.colorCamera.activeTexture.Release();
        robotController.colorCamera.targetTexture = new RenderTexture( (int) Mathf.Round(1280 * videoScale), (int) Mathf.Round(720 * videoScale), 32, RenderTextureFormat.BGRA32);
        
        yield return new WaitUntil(() => robotController.depthCamera.activeTexture != null && robotController.depthCamera.activeTexture != null);

        Texture2D texColor, texDepth;
        AsyncGPUReadbackRequest requestColor = UnityEngine.Rendering.AsyncGPUReadback.Request(robotController.colorCamera.activeTexture);
        AsyncGPUReadbackRequest requestDepth = UnityEngine.Rendering.AsyncGPUReadback.Request(robotController.depthCamera.activeTexture);
        yield return new WaitUntil(() => requestColor.done && requestDepth.done);
        texColor = new Texture2D(requestColor.width, requestColor.height, TextureFormat.RGBA32, false);
        texDepth = new Texture2D(requestDepth.width, requestDepth.height, TextureFormat.RGBA32, false);
        
        
        ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
        System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
        EncoderParameters myEncoderParameters = new EncoderParameters(1);
        EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 30L);
        myEncoderParameters.Param[0] = myEncoderParameter;
        
        System.Drawing.Imaging.Encoder myEncoder2 = System.Drawing.Imaging.Encoder.Quality;
        EncoderParameters myEncoderParameters2 = new EncoderParameters(1);
        EncoderParameter myEncoderParameter2 = new EncoderParameter(myEncoder, 100L);
        myEncoderParameters2.Param[0] = myEncoderParameter2;
        
        Stream stream = new MemoryStream();
        Bitmap depthBmp = new Bitmap( requestDepth.width, requestDepth.height, PixelFormat.Format32bppArgb); 
        Bitmap videoBmp = new Bitmap( requestColor.width, requestColor.height, PixelFormat.Format32bppArgb);
        
        while (true)
        {
            requestDepth = UnityEngine.Rendering.AsyncGPUReadback.Request(robotController.depthCamera.activeTexture);
            requestColor = UnityEngine.Rendering.AsyncGPUReadback.Request(robotController.colorCamera.activeTexture);

            yield return new WaitUntil(() => requestDepth.done);
            if (!requestDepth.hasError)
            {
                byte[] arr = requestDepth.GetData<byte>().ToArray();
                BitmapData bmpData = depthBmp.LockBits(
                    new Rectangle(0, 0, depthBmp.Width, depthBmp.Height),   
                    ImageLockMode.ReadWrite, depthBmp.PixelFormat);
                Marshal.Copy(arr, 0, bmpData.Scan0, arr.Length);
                depthBmp.UnlockBits(bmpData);
                depthBmp.RotateFlip(RotateFlipType.RotateNoneFlipY );
                
                stream.SetLength(0);
                depthBmp.Save(stream, jpgEncoder, myEncoderParameters2);
                
                int len = (int)stream.Length;
                var buffer = new byte[len];
                stream.Position = 0;
                int read = stream.Read(buffer, 0, len);
                if(len - read != 0)Debug.LogError("Wrong len read JPEG");
                depthBytes = buffer;
            }
            
            yield return new WaitUntil(() => requestColor.done);
            if (!requestColor.hasError)
            {
                byte[] arr = requestColor.GetData<byte>().ToArray();
                BitmapData bmpData = videoBmp.LockBits(
                    new Rectangle(0, 0, videoBmp.Width, videoBmp.Height),   
                    ImageLockMode.ReadWrite, videoBmp.PixelFormat);
                Marshal.Copy(arr, 0, bmpData.Scan0, arr.Length);
                videoBmp.UnlockBits(bmpData);
                videoBmp.RotateFlip(RotateFlipType.RotateNoneFlipY );
                
                stream.SetLength(0);
                videoBmp.Save(stream, jpgEncoder, myEncoderParameters);
                
                int len = (int)stream.Length;
                var buffer = new byte[len];
                stream.Position = 0;
                int read = stream.Read(buffer, 0, len);
                if(len - read != 0)Debug.LogError("Wrong len read JPEG");
                colorBytes = buffer;
            }
        }
    }
    
    
    private ImageCodecInfo GetEncoder(ImageFormat format)
    {

        ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

        foreach (ImageCodecInfo codec in codecs)
        {
            if (codec.FormatID == format.Guid)
            {
                return codec;
            }
        }
        return null;
    }
    public volatile byte[] colorBytes, depthBytes;
    private NativeArray<byte> arr = new NativeArray<byte>();
    void Update()
    {
        while (mainThreadUpdateWorkers.Count > 0)
        {
            mainThreadUpdateWorkers.TryDequeue(out MainThreadUpdateWorker worker);
            worker.action.Invoke();
            worker.done = true; 
        }

        if (Application.targetFrameRate != 65)
        {
            QualitySettings.vSyncCount = 0; // VSync must be disabled
            Application.targetFrameRate = 65;
        }
    }

    public byte[] GetDepthMap()
    {/*
        byte[] ret;

        if (robotController.depthCamera.targetTexture.width != (int)Math.Round(1280 * Settings.config.simulationOptions.depthMapScale))
        {
            if (robotController.depthCamera.targetTexture != null) robotController.depthCamera.activeTexture.Release();
            robotController.depthCamera.targetTexture = new RenderTexture((int)Math.Round(1280 * Settings.config.simulationOptions.depthMapScale), (int)Math.Round(720 * Settings.config.simulationOptions.depthMapScale), 24);
        }

        int w = robotController.depthCamera.activeTexture.width, h = robotController.depthCamera.activeTexture.height;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        RenderTexture.active = robotController.depthCamera.activeTexture;
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        RenderTexture.active = null;
        ret = tex.EncodeToJPG(Settings.config.simulationOptions.depthMapQuality);
        /*
        byte[] x = tex.GetRawTextureData();
        Debug.Log(x.Length);
        Debug.Log(x.Length / (w*h));*/
        //Destroy(tex);
        // return ret;*/

        return depthBytes;
    }

    public byte[] GetVideo()
    {
        /* byte[] ret;

         if (robotController.colorCamera.targetTexture.width != (int)Math.Round(1280 * Settings.config.simulationOptions.videoFeedScale))
         {
             if (robotController.colorCamera.targetTexture != null) robotController.colorCamera.activeTexture.Release();
             robotController.colorCamera.targetTexture = new RenderTexture((int)Math.Round(1280 * Settings.config.simulationOptions.videoFeedScale), (int)Math.Round(720 * Settings.config.simulationOptions.videoFeedScale), 24);
         }

         int w = robotController.colorCamera.activeTexture.width, h = robotController.colorCamera.activeTexture.height;
         Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
         RenderTexture.active = robotController.colorCamera.activeTexture;
         tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
         RenderTexture.active = null;
         
         if (Settings.config.simulationOptions.videoFeedQuality >= 100) ret = tex.EncodeToPNG();
         else ret = tex.EncodeToJPG(Settings.config.simulationOptions.videoFeedQuality);
         Destroy(tex);
         return ret;*/
        return colorBytes;
    }

    public List<JSON.DetectedObject> GetDetection()
    {
        var detector = new Detector();
        var detected = new List<JSON.DetectedObject>();
        foreach (var info in detector.Detect(Camera.main))
        {
            if(info.includeInDataset)detected.Add(new JSON.DetectedObject()
            {
                visibleInFrame = info.visible,
                min = new JSON.Vec2() { x = info.min.x, y = info.min.y },
                max = new JSON.Vec2() { x = info.max.x, y = info.max.y },
                fill = info.fill,
                className = info.className,
                distance = info.distance,
                colorPercentVisible = info.colorPercentVisible
            });
        }
        return detected;
    }

    void SetGraphics()
    {
        /*
        RenderSettings.ambientLight = new Color(Settings.config.datasetOptions.ambientLightColor[0], Settings.config.datasetOptions.ambientLightColor[1], Settings.config.datasetOptions.ambientLightColor[2], 1f);
        RenderSettings.ambientIntensity = Settings.config.datasetOptions.ambientLightIntesity;

        camera.backgroundColor = new Color(Settings.config.datasetOptions.cameraBackgroundColor[0], Settings.config.datasetOptions.cameraBackgroundColor[1], Settings.config.datasetOptions.cameraBackgroundColor[2], 1f);

        RenderSettings.fog = Settings.config.datasetOptions.fogEnabled;
        if (RenderSettings.fog)
        {
            RenderSettings.fogDensity = Settings.config.datasetOptions.fogDensity;

            RenderSettings.fogColor = camera.backgroundColor;

        }*/
    }

    void WAPIRecv()
    {
        int wapiPort = Settings.config.simulationOptions.webAPIPort;
        TcpListener wapiSocket = new TcpListener(IPAddress.Any, wapiPort);
        wapiSocket.Start();
        acceptNewClients = true;
        while (acceptNewClients)
        {
            TcpClient client = wapiSocket.AcceptTcpClient();
            WAPIClient wapiClient = new WAPIClient(client, this);
            if (wapiClients.TryAdd(wapiClient.id, wapiClient)) Debug.Log("Json client connected, connected: " + WAPIClient.connectedClients.ToString());
            else Debug.LogError("Failed to add a client to wapiCLient dict");
        }
        Debug.Log("acceptNewClients = false");
    }

    public void SendToClients(PacketType packetType, Flag packetFlag)
    {
        foreach (KeyValuePair<int, WAPIClient> item in wapiClients) item.Value.EnqueuePacket(packetType, packetFlag);
    }
    public void SendToClients(PacketType packetType, Flag packetFlag, string json)
    {
        foreach (KeyValuePair<int, WAPIClient> item in wapiClients) item.Value.EnqueuePacket(packetType, packetFlag, json);
    }
    public void SendToClients(PacketType packetType, Flag packetFlag, byte[] bytes)
    {
        foreach (KeyValuePair<int, WAPIClient> item in wapiClients) item.Value.EnqueuePacket(packetType, packetFlag, bytes, bytes.Length, false);
    }

    void OnApplicationQuit()
    {
        acceptNewClients = false;
        foreach (KeyValuePair<int, WAPIClient> pair in wapiClients) pair.Value.Stop();
        wapiThread.Abort();
        Debug.Log("Application ended after " + Time.time + " seconds");
    }
}

public class MainThreadUpdateWorker
{
    public volatile bool done;
    public System.Action action;
    public MainThreadUpdateWorker()
    {
        done = false;
    }
}   