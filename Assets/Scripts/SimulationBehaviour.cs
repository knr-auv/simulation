using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using static JSON;

public class SimulationBehaviour : MonoBehaviour
{
    [SerializeField]
    private bool _renderEnabled = false;
    [SerializeField]
    private RenderTexture _okonTex;
    [SerializeField]
    private GameObject _okon;
    [SerializeField]
    public int quality = 85;
    private static int _videoPort = 44209;
    private static int _jsonPort = 44210;
    private Thread _videoRecv, _jsonRecv;
    private Texture2D _rtTex;
    private byte[] _image = new byte[1], _newImage;
    private Texture2D _tex;
    private bool _swxh = true;
    private Motors _motors = new Motors();
    private OkonController _okonController;
    private float _fps = 0;

    private enum Packet : byte{
        SET_MTR = 0xA0,
        GET_SENS = 0xB0,
        SET_SIM = 0xC0,
        ACK = 0xC1,
        GET_ORIEN = 0xC2,
        SET_ORIEN = 0xC3,
        REC_STRT = 0xD0,
        REC_ST = 0xD1,
        REC_RST = 0xD2,
        GET_REC = 0xD3,
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
        _tex = new Texture2D(_okonTex.width, _okonTex.height, TextureFormat.RGB24, false);
        _okonController = _okon.GetComponent<OkonController>();
        _videoRecv = new Thread(VideoRecv);
        _jsonRecv = new Thread(JsonRecv);
        _videoRecv.IsBackground = true;
        _jsonRecv.IsBackground = true;
        _videoRecv.Start();
        _jsonRecv.Start();
    }

    private void Update()
    {
        if (!_renderEnabled) return;
        if (_swxh)
        {
            RenderTexture.active = _okonTex;
            _tex.ReadPixels(new Rect(0, 0, _okonTex.width, _okonTex.height), 0, 0);
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
            NetworkStream stream = client.GetStream();
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
                        packet = (Packet)stream.ReadByte();
                        byte[] dataLenBytes = new byte[4];
                        stream.Read(dataLenBytes, 0, 4);
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
                                    int bytesRead = stream.Read(jsonBytes, ptr, dataLength - ptr);
                                    ptr += bytesRead;
                                }
                                else
                                {
                                    int bytesRead = stream.Read(jsonBytes, ptr, client.ReceiveBufferSize);
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
                                _okonController.FL.fill = _motors.FL;
                                _okonController.FR.fill = _motors.FR;
                                _okonController.B.fill = _motors.B;
                                _okonController.ML.fill = _motors.ML;
                                _okonController.MR.fill = _motors.MR;
                                break;
                            case Packet.GET_ORIEN:
                                SendJson(Packet.GET_ORIEN, JsonUtility.ToJson(_okonController.GetOrientation()));
                                break;
                            case Packet.SET_ORIEN:
                                Orientation orientation = new Orientation();
                                TryJsonToObject(jsonFromClient, orientation);
                                _okonController.SetOrientation(orientation);
                                break;
                            case Packet.GET_SENS:
                                SendJson(Packet.GET_SENS, JsonUtility.ToJson(_okonController.GetSensors()));
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
                            case Packet.ACK:
                                SendJson(Packet.ACK, "{\"fps\":" + Mathf.Round(_fps).ToString() + ", \"state\":\"" + state + "\"}");
                                break;
                            case (Packet)0xFF:
                                stream?.Close();
                                stream?.Dispose();
                                client?.Dispose();
                                stream = null;
                                client = null;
                                break;
                            case (Packet)0x00:
                                stream?.Close();
                                stream?.Dispose();
                                client?.Dispose();
                                stream = null;
                                client = null;
                                break;
                            default:
                                Debug.LogWarning("Unknown dataframe type " + System.BitConverter.ToString(new byte[] { (byte)packet }));
                                state = "Unsupported packet " + System.BitConverter.ToString(new byte[] { (byte)packet });
                                break;
                        }
                        if(state != "none") SendJson(Packet.ACK, "{\"fps\":" + Mathf.Round(_fps).ToString() + ", \"state\":\"" + state + "\"}");
                        #endregion
                    } while (stream != null && stream.DataAvailable);
                }
                catch
                {
                    stream?.Close();
                    stream?.Dispose();
                    client?.Close();
                }
            }
            stream?.Dispose();
            client?.Dispose();
            Debug.Log("Json client connection lost");

            void SendJson(Packet packetType, string json)
            {
                byte[] bytes = Encoding.ASCII.GetBytes(json);
                stream.WriteByte((byte)packetType);
                stream.Write(System.BitConverter.GetBytes(bytes.Length), 0, 4);
                stream.Write(bytes, 0, bytes.Length);
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
            NetworkStream stream = client.GetStream();
            byte packetType;
            bool clientConnected = true;
            Debug.Log("Video client connected");
            try
            {
                while (clientConnected)
                {
                    try
                    {
                        packetType = (byte)stream.ReadByte();
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
                            stream.WriteByte(0x69);
                            stream.Write(System.BitConverter.GetBytes(_image.Length), 0, 4);
                            stream.Write(_image, 0, _image.Length);
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
        Thread.Sleep(5);
        Debug.Log("Simulation halted: " + _jsonRecv.ThreadState + " _ " + _videoRecv.ThreadState);
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
