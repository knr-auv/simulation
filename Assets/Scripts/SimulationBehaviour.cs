using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using static JSON;
//https://www.alanzucconi.com/2015/10/11/how-to-write-native-plugins-for-unity/ TODO
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
        GET_POS = 0xC2,
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
            while (client.Connected)
            {
                do
                {
                    try
                    {
                        switch ((Packet)nwStream.ReadByte())
                        {
                            case Packet.SET_MTR://motors Json
                                byte[] len = new byte[4];
                                nwStream.Read(len, 0, 4);
                                int dataLen = System.BitConverter.ToInt32(len, 0);
                                byte[] jByte = new byte[dataLen];
                                nwStream.Read(jByte, 0, dataLen);
                                string json = Encoding.ASCII.GetString(jByte, 0, dataLen);
                                JsonUtility.FromJsonOverwrite(json, _motors);
                                okonController.FL.fill = _motors.FL;
                                okonController.FR.fill = _motors.FR;
                                okonController.B.fill = _motors.B;
                                okonController.ML.fill = _motors.ML;
                                okonController.MR.fill = _motors.MR;    
                                break;
                            case Packet.GET_POS:
                                json = (JsonUtility.ToJson(okonController.GetOrientation()));
                                jByte = Encoding.ASCII.GetBytes(json);
                                nwStream.WriteByte(0x34);
                                nwStream.Write(System.BitConverter.GetBytes(jByte.Length), 0, 4);
                                nwStream.Write(jByte, 0, jByte.Length);
                                Debug.Log(json);
                                break;
                            case Packet.GET_SENS:
                                json = (JsonUtility.ToJson(okonController.GetSensors()));
                                jByte = Encoding.ASCII.GetBytes(json);
                                nwStream.WriteByte((byte)Packet.GET_SENS);
                                nwStream.Write(System.BitConverter.GetBytes(jByte.Length), 0, 4);
                                nwStream.Write(jByte, 0, jByte.Length);
                                Debug.Log(json);
                                break;
                            default:
                                Debug.LogWarning("Wrong dataframe type");
                                break;
                        }
                    }
                    catch
                    {
                        Debug.Log("Json client crashed");
                        client.Close();
                        break;
                    }
                } while (nwStream.DataAvailable);
            }
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
                    default:
                        Debug.Log(System.BitConverter.ToString(new byte[] { buffer[0] }));
                        break;
                }

            }
        }
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
