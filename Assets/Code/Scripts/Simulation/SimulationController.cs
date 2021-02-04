using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public class SimulationController : MonoBehaviour
{
    [SerializeField] GameObject cameraObject;
    Camera camera;

    [Header("Post processing")]
    PostProcessDebug postDebug;
    PostProcessLayer postLayer;
    PostProcessVolume postVolume;

    [Header("Lights")]
    [SerializeField] Light light1;
    [SerializeField] Light light2;

    [SerializeField] List<GameObject> knownObjects;

    GameObject waterContainer;
    GameObject robot;
    public RobotController robotController;

    //WAPI
    public byte[] depthMapBytes;
    public bool getDepthImage, depthImageReady, acceptNewClients;

    readonly object jsonDictionaryLock = new object();
    public readonly Dictionary<int, WAPIClient> wapiClients = new Dictionary<int, WAPIClient>();
    Thread wapiThread;
    //

    public ConcurrentQueue<MainThreadUpdateWorker> mainThreadUpdateWorkers;

    void Start()
    {
        QualitySettings.vSyncCount = 0;  // VSync must be disabled
        Application.targetFrameRate = 65;

        mainThreadUpdateWorkers = new ConcurrentQueue<MainThreadUpdateWorker>();
        if (Settings.config == null || Settings.config.mode == null)
        {
            SceneManager.LoadScene("Start");
            return;//TODO take config
        }/**/

        string selectedWaterContainer = Settings.config.simulationOptions.selectedWaterContainer;
        if (TryGetObjectByTypeName(selectedWaterContainer, out GameObject waterContainerPrefab)) waterContainer = Instantiate(waterContainerPrefab, Vector3.zero, Quaternion.identity);

        string selectedRobotName = Settings.config.simulationOptions.selectedRobot;
        if (TryGetObjectByTypeName(selectedRobotName, out GameObject robotPrefab))
        {
            robot = Instantiate(robotPrefab, new Vector3(0, 2, 0), Quaternion.identity);
            robotController = robot.GetComponent<RobotController>();
        }

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

        wapiThread = new Thread(WAPIRecv);
        wapiThread.IsBackground = true;
        wapiThread.Start();
    }

    void Update()
    {
        while (mainThreadUpdateWorkers.Count > 0)
        {
            mainThreadUpdateWorkers.TryDequeue(out MainThreadUpdateWorker worker);
            worker.action.Invoke();
            worker.done = true; 
        }
    }

    public byte[] GetDepthMap()
    {
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
        Destroy(tex);
        return ret;
    }

    public byte[] GetVideo()
    {
        byte[] ret;

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
        return ret;
    }

    public JSON.Detection GetDetection()
    {
        var detector = new Detector();
        JSON.Detection detection = new JSON.Detection();
        detection.detected = new List<JSON.DetectedObject>();
        foreach (var info in detector.Detect(Camera.main))
        {
            if(info.includeInDataset)detection.detected.Add(new JSON.DetectedObject()
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
        return detection;
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
            lock (jsonDictionaryLock) {
                wapiClients.Add(wapiClient.id, wapiClient);
                Debug.Log("Json client connected, connected: " + WAPIClient.connectedClients.ToString());
            }
        }
        Debug.Log("acceptNewClients = false");
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
    public Action action;
    public MainThreadUpdateWorker()
    {
        done = false;
    }
}   