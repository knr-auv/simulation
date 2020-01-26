using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using static JSON;

public class SimulationBehaviour : MonoBehaviour
{
    [SerializeField]
    private static int _videoPort = 44209;
    [SerializeField]
    private static int _jsonPort = 44210;
    [SerializeField]
    private int quality;
    [SerializeField]
    private bool onbool = false;
    [SerializeField]
    private RenderTexture okonTex;
    [SerializeField]
    private GameObject Okon;

    private Thread _videoRecv, _jsonRecv;
    private Texture2D _rtTex;
    private byte[] _image = new byte[1], _newImage;
    private Texture2D _tex;
    private bool _swxh = true;
    private Motors _motors = new Motors();
    private OkonController okonController;

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
        GE_REC = 0xD3
    }

    void Start()
    {
        _tex = new Texture2D(okonTex.width, okonTex.height, TextureFormat.RGB24, false);
        okonController = Okon.GetComponent<OkonController>();
        _videoRecv = new Thread(VideoRecv);
        _jsonRecv = new Thread(JsonRecv);
        _videoRecv.IsBackground = true;
        _jsonRecv.IsBackground = true;
        _videoRecv.Start();
        _jsonRecv.Start();
    }

    private void FixedUpdate()
    {
        
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
            string jsonFromClient;
            while (client != null && client.Connected)
            {
                do
                {
                    try
                    {
                        packet = (Packet)nwStream.ReadByte();
                        byte[] dataLenBytes = new byte[4];
                        nwStream.Read(dataLenBytes, 0, 4);
                        int dataLength = System.BitConverter.ToInt32(dataLenBytes, 0);
                        jsonFromClient = "{}";
                        if (dataLength > 0)
                        {
                            byte[] jsonBytes = new byte[dataLength];
                            nwStream.Read(jsonBytes, 0, dataLength);
                            jsonFromClient = Encoding.ASCII.GetString(jsonBytes, 0, dataLength);
                        }
                        
                        switch (packet)
                        {
                            case Packet.SET_MTR://motors Json
                                Debug.Log("From client: " + jsonFromClient);
                                Debug.Log(JsonUtility.ToJson(_motors));
                                TryJsonToObject(jsonFromClient, _motors);
                                okonController.FL.fill = _motors.FL;
                                okonController.FR.fill = _motors.FR;
                                okonController.B.fill = _motors.B;
                                okonController.ML.fill = _motors.ML;
                                okonController.MR.fill = _motors.MR;
                                break;
                            case Packet.GET_ORIEN:
                                string json2send = (JsonUtility.ToJson(okonController.GetOrientation()));
                                Debug.Log(json2send);
                                SendJson(Packet.GET_ORIEN, json2send);
                                break;
                            case Packet.GET_SENS:
                                json2send = (JsonUtility.ToJson(okonController.GetSensors()));
                                Debug.Log(json2send);
                                SendJson(Packet.GET_SENS, json2send);
                                break;
                            case (Packet)0xFF:
                                nwStream.Dispose();
                                client.Dispose();
                                nwStream = null;
                                client = null;
                                break;
                            default:
                                Debug.LogWarning("Unknown dataframe type " + System.BitConverter.ToString(new byte[] { (byte)packet }));
                                break;
                        }
                        SendJson(Packet.ACK, "{}");
                    }
                    catch
                    {
                        Debug.Log("Json client crashed");
                        nwStream?.Close();
                        nwStream?.Dispose();
                        client?.Close();
                        break;
                    }
                } while (nwStream != null && nwStream.DataAvailable);
            }
            nwStream?.Dispose();
            client?.Dispose();
            Debug.Log("Json client disconnected");
            //TODO repair disconnecting
            void SendJson(Packet packetType, string json)
            {
                byte[] bytes = Encoding.ASCII.GetBytes(json);
                nwStream.WriteByte((byte)packetType);
                nwStream.Write(System.BitConverter.GetBytes(bytes.Length), 0, 4);
                nwStream.Write(bytes, 0, bytes.Length);
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
            byte[] buffer = new byte[client.ReceiveBufferSize];
            bool clientConnected = true;
            Debug.Log("Video client connected");
            try
            {
                while (clientConnected)
                {
                    try
                    {
                        int bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize);
                    }
                    catch
                    {
                        Debug.Log("Video client crashed");
                        client.Close();
                        break;
                    }

                    switch (buffer[0])
                    {
                        case 0x69:
                            _image = _newImage;
                            nwStream.WriteByte(0x69);
                            nwStream.Write(System.BitConverter.GetBytes(_image.Length), 0, 4);
                            nwStream.Write(_image, 0, _image.Length);
                            break;
                        case 0x64://Client CLose connection 'd'
                            Debug.Log("Video client disconnected");
                            client.Close();
                            clientConnected = false;
                            break;
                        case 0x66://Stop thread 'f'
                            _videoRecv.Abort();
                            Application.Quit();
                            break;
                        case 0x00:
                            clientConnected = false;
                            break;
                        default:
                            Debug.Log(System.BitConverter.ToString(new byte[] { buffer[0] }));
                            break;
                    }
                }   
            }catch(SocketException exc)
            {
                Debug.Log("Video client disconnected");
            }
            catch(System.Exception exp)
            {
                Debug.LogWarning(exp.Message);
            }
        }
    }

    void OnApplicationQuit()
    {
        _videoRecv.Abort();
        _jsonRecv.Abort();
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
