using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using static JSON;

public class SimulationBehaviour : MonoBehaviour
{
    [SerializeField]
    private bool onbool = false;
    [SerializeField]
    private RenderTexture okonTex;
    [SerializeField]
    private GameObject Okon;

    private int quality = 85;
    private static int _videoPort = 44209;
    private static int _jsonPort = 44210;
    private Thread _videoRecv, _jsonRecv;
    private Texture2D _rtTex;
    private byte[] _image = new byte[1], _newImage;
    private Texture2D _tex;
    private bool _swxh = true;
    private Motors _motors = new Motors();
    private OkonController okonController;
    private float _fps = 0;

    private enum Packet : byte{
        SET_MTR = 0xA0,
        GET_SENS = 0xB0,
        SET_SIM = 0xC0,
        ACK = 0xC1,
        GET_ORIEN = 0xC2,
        SET_POS = 0xC3,
        REC_STRT = 0xD0,
        REC_ST = 0xD1,
        REC_RST = 0xD2,
        GE_REC = 0xD3,
        PING = 0xC5
    }

    void Start()
    {
        try
        {
            string jsonSettings = System.IO.File.ReadAllText("settings.json");
            Init init = new Init();
            TryJsonToObject(jsonSettings, init);
            _videoPort = init.videoPort;
            _jsonPort = init.jsonPort;
            quality = init.quality;
        }
        catch
        {
            _videoPort = 44209;
            _jsonPort = 44210;
        }
        _tex = new Texture2D(okonTex.width, okonTex.height, TextureFormat.RGB24, false);
        okonController = Okon.GetComponent<OkonController>();
        _videoRecv = new Thread(VideoRecv);
        _jsonRecv = new Thread(JsonRecv);
        _videoRecv.IsBackground = true;
        _jsonRecv.IsBackground = true;
        _videoRecv.Start();
        _jsonRecv.Start();
    }

    private void Update()
    {
        if (!onbool) return;
        if (_swxh)
        {
            RenderTexture.active = okonTex;
            _tex.ReadPixels(new Rect(0, 0, okonTex.width, okonTex.height), 0, 0);
            _swxh = false;
        }
        else
        {
            _newImage = _tex.EncodeToJPG(quality);
            _swxh = true;
        }
        _fps = 1f / Time.deltaTime;
    }

    private void JsonRecv()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, _jsonPort);
        listener.Start();
        Debug.Log("Waiting for Jason...");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            NetworkStream nwStream = client.GetStream();
            Debug.Log("Json client connected");
            Packet packet;
            string jsonFromClient, state;
            while (client != null && client.Connected)
            {
                try
                {
                    do
                    {
                        #region JSON_recv
                        packet = (Packet)nwStream.ReadByte();
                        byte[] dataLenBytes = new byte[4];
                        nwStream.Read(dataLenBytes, 0, 4);
                        int dataLength = System.BitConverter.ToInt32(dataLenBytes, 0);
                        jsonFromClient = "{}";
                        if (dataLength > 0)
                        {
                            byte[] jsonBytes = new byte[dataLength];
                            int ptr = 0;
                            do
                            {
                                if (dataLength - ptr < client.ReceiveBufferSize)
                                {
                                    int bytesRead = nwStream.Read(jsonBytes, ptr, dataLength - ptr);
                                    ptr += bytesRead;
                                }
                                else
                                {
                                    int bytesRead = nwStream.Read(jsonBytes, ptr, client.ReceiveBufferSize);
                                    ptr += bytesRead;
                                }
                            } while (ptr != dataLength);
                            jsonFromClient = Encoding.ASCII.GetString(jsonBytes, 0, dataLength);
                        }
                        state = "none";
                        switch (packet)
                        {
                            case Packet.SET_MTR:
                                Debug.Log("From client: " + jsonFromClient);
                                TryJsonToObjectState(jsonFromClient, _motors);
                                okonController.FL.fill = _motors.FL;
                                okonController.FR.fill = _motors.FR;
                                okonController.B.fill = _motors.B;
                                okonController.ML.fill = _motors.ML;
                                okonController.MR.fill = _motors.MR;
                                break;
                            case Packet.GET_ORIEN:
                                SendJson(Packet.GET_ORIEN, JsonUtility.ToJson(okonController.GetOrientation()));
                                break;
                            case Packet.GET_SENS:
                                SendJson(Packet.GET_SENS, JsonUtility.ToJson(okonController.GetSensors()));
                                break;
                            case Packet.PING:
                                JSON.Ping ping = new JSON.Ping();
                                TryJsonToObjectState(jsonFromClient, ping);
                                ping.ping = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond - ping.timestamp;
                                ping.timestamp = System.DateTime.Now.Ticks;
                                SendJson(Packet.PING, JsonUtility.ToJson(ping));
                                break;
                            case Packet.SET_SIM:
                                Settings settings = new Settings();
                                TryJsonToObjectState(jsonFromClient, settings);
                                quality = settings.quality;
                                break;
                            case (Packet)0xFF:
                                nwStream?.Close();
                                nwStream?.Dispose();
                                client?.Dispose();
                                nwStream = null;
                                client = null;
                                break;
                            case (Packet)0x00:
                                nwStream?.Close();
                                nwStream?.Dispose();
                                client?.Dispose();
                                nwStream = null;
                                client = null;
                                break;
                            default:
                                Debug.LogWarning("Unknown dataframe type " + System.BitConverter.ToString(new byte[] { (byte)packet }));
                                state = "Unsupported packet " + System.BitConverter.ToString(new byte[] { (byte)packet });
                                break;
                        }
                        SendJson(Packet.ACK, "{\"fps\":" + Mathf.Round(_fps).ToString() + ", \"state\":\"" + state + "\"}");
                        #endregion
                    } while (nwStream != null && nwStream.DataAvailable);
                }
                catch
                {
                    nwStream?.Close();
                    nwStream?.Dispose();
                    client?.Close();
                }
            }
            nwStream?.Dispose();
            client?.Dispose();
            Debug.Log("Json client connection lost");

            void SendJson(Packet packetType, string json)
            {
                byte[] bytes = Encoding.ASCII.GetBytes(json);
                nwStream.WriteByte((byte)packetType);
                nwStream.Write(System.BitConverter.GetBytes(bytes.Length), 0, 4);
                nwStream.Write(bytes, 0, bytes.Length);
                Debug.Log(json);
            }

            void TryJsonToObjectState(string json, object obj)
            {
                try
                {
                    JsonUtility.FromJsonOverwrite(json, obj);
                }
                catch(System.Exception exp)
                {
                    Debug.Log("Wrong JSON for " + obj.GetType().ToString());
                    state = exp.Message;
                }
            }
        }
    }
    
    private void TryJsonToObject(string json, object obj)
    {
        try
        {
            JsonUtility.FromJsonOverwrite(json, obj);
        }
        catch 
        {
            Debug.Log("Wrong JSON for " + obj.GetType().ToString());
        }
    }

    private void VideoRecv()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, _videoPort);
        listener.Start();
        Debug.Log("Waiting for video client...");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            NetworkStream nwStream = client.GetStream();
            byte packetType;
            bool clientConnected = true;
            Debug.Log("Video client connected");
            try
            {
                while (clientConnected)
                {
                    try
                    {
                        packetType = (byte)nwStream.ReadByte();
                    }
                    catch
                    {
                        Debug.Log("Video client crashed");
                        client.Close();
                        break;
                    }

                    switch (packetType)
                    {
                        case 0x69:
                            _image = _newImage;
                            nwStream.WriteByte(0x69);
                            nwStream.Write(System.BitConverter.GetBytes(_image.Length), 0, 4);
                            nwStream.Write(_image, 0, _image.Length);
                            break;
                        case 0x00:
                            clientConnected = false;
                            break;
                        case 0xFF:
                            clientConnected = false;
                            break;
                        default:
                            Debug.Log(System.BitConverter.ToString(new byte[] { packetType }));
                            break;
                    }
                }   
            }catch(SocketException exc)
            {
                Debug.Log("Video client disconnected" + exc.Message);
            }
            catch(System.Exception exp)
            {
                Debug.LogWarning(exp.Message);
            }
        }
    }

    void OnApplicationQuit()
    {
        _videoRecv?.Abort();
        _jsonRecv?.Abort();
        Debug.Log("Simulation halted");
    }

    /*public void CreateNTestCubes(int n)
    {public GameObject testCube;
        for (int i = 0; i < n; i++)
        {
            GameObject tmp = Instantiate(testCube);
            tmp.transform.localScale = new Vector3(.2f + Random.value * .1f, .2f + Random.value * .1f, .2f + Random.value * .1f);
            tmp.transform.position += new Vector3(Random.value * 1f, Random.value * 20f, Random.value * 1f);
            tmp.GetComponent<WaterBehaviour>().waterDensity = 500 + Random.value * 200f;
            tmp.GetComponent<WaterBehaviour>().volumeCenterOffset = new Vector3(Random.value * .2f - .1f, Random.value * .2f - .1f, Random.value * .2f - .1f);
        }
    }*/
}
