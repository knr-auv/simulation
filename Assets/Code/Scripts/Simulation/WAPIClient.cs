using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Utf8Json;
using System.Collections.Concurrent;
using System.Reflection;

public class WAPIClient
{
    public enum PacketType : byte
    {
        SET_MTR = 0xA0,
        GET_SENS = 0xB0,
        GET_DEPTH = 0xB1,
        GET_DEPTH_BYTES = 0xB2,
        GET_VIDEO_BYTES = 0xB3,
        SET_SIM = 0xC0,
        ACK = 0xC1,
        GET_ORIEN = 0xC2,
        SET_ORIEN = 0xC3,
        RST_SIM = 0xC4,
        PING = 0xC5,
        GET_CPS = 0xC6,
        HIT_NGZ = 0xC7,
        HIT_FZ = 0xC8,
        REC_STRT = 0xD0,
        REC_ST = 0xD1,
        REC_RST = 0xD2,
        GET_REC = 0xD3,
        GET_DETE = 0xDE
    }

    [Flags]
    public enum Flag
    {
        None = 0,
        SERVER_ECHO = 1,
        DO_NOT_LOG_PACKET = 2,
        TEST = 128
    }

    public struct Packet
    {
        public PacketType packetType;
        public Flag flag;
        public byte[] bytes;
        public int length;
        public bool rented;

        public Packet(PacketType packetType, Flag flag, byte[] bytes, int length, bool rented)
        {
            this.packetType = packetType;
            this.flag = flag;
            this.bytes = bytes;
            this.length = length;
            this.rented = rented;
        }
    }

    static int clientId = 0;
    public static volatile int connectedClients = 0;
    readonly TcpClient client;
    NetworkStream stream;
    public int id;
    readonly Thread talking;
    readonly SimulationController simulationControllerInstance;
    bool clientConnected;
    readonly StringBuilder sb = new StringBuilder();
    private readonly ConcurrentQueue<Packet> toSend;

    public WAPIClient(TcpClient client, SimulationController simulationControllerInstance)
    {
        this.client = client;
        id = clientId++;
        connectedClients++;
        this.simulationControllerInstance = simulationControllerInstance;
        clientConnected = true;

        toSend = new ConcurrentQueue<Packet>();

        talking = new Thread(HandleJsonClient)
        {
            IsBackground = true
        };
        talking.Start();
    }

    private void HandleJsonClient()
    {
        stream = client.GetStream();
        stream.ReadTimeout = 1000 * 60 * 5;//TODO
        Flag packetFlag;
        byte[] dataLenBytes = new byte[4];
        string jsonFromClient = "";
        RobotController rc = simulationControllerInstance.robotController;
        try
        {
            while (clientConnected)
            {
                if (stream.DataAvailable)
                {
                    #region JSON_recv
                    var packetType = (PacketType)ReadByteFromStream(stream);
                    packetFlag = (Flag)ReadByteFromStream(stream);
                    ReadAllFromStream(stream, dataLenBytes, 4);
                    int dataLength = System.BitConverter.ToInt32(dataLenBytes, 0);
                    byte[] dataFromClient = ArrayPool<byte>.Shared.Rent(dataLength);
                    ReadAllFromStream(stream, dataFromClient, dataLength);
                    jsonFromClient = Encoding.ASCII.GetString(dataFromClient, 0, dataLength);
                   
                    switch (packetType)
                    {
                        case PacketType.RST_SIM:
                            MainThreadUpdateWorker resetWorker = new MainThreadUpdateWorker()
                            {
                                action = () => { GameObject.FindGameObjectWithTag("SimulationController").GetComponent<SimulationController>().PlaceRobotInStartZone(); }
                            };
                            simulationControllerInstance.mainThreadUpdateWorkers.Enqueue(resetWorker);
                            break;
                        case PacketType.GET_CPS:
                            string ret = "[";
                            MainThreadUpdateWorker checkpointWorker = new MainThreadUpdateWorker()
                            {
                                action = () => {
                                    foreach (var obj in GameObject.FindGameObjectsWithTag("Checkpoint"))
                                        ret += "{\"id\":\"" + obj.GetComponent<CheckpointController>().id + "\",reached:" + obj.GetComponent<CheckpointController>().reached + "}";
                                    EnqueuePacket(PacketType.GET_CPS, packetFlag, ret + "]");
                                }
                            };
                            simulationControllerInstance.mainThreadUpdateWorkers.Enqueue(checkpointWorker);
                            break;
                        case PacketType.SET_MTR:
                            if (!packetFlag.HasFlag(Flag.DO_NOT_LOG_PACKET)) Debug.Log("From client: " + jsonFromClient);
                            var motors = JsonSerializer.Deserialize<JSON.Motors>(jsonFromClient);
                            rc.motorFLH.fill = motors.FLH;
                            rc.motorFLV.fill = motors.FLV;
                            rc.motorBLV.fill = motors.BLV;
                            rc.motorBLH.fill = motors.BLH;
                            rc.motorFRH.fill = motors.FRH;
                            rc.motorFRV.fill = motors.FRV;
                            rc.motorBRV.fill = motors.BRV;
                            rc.motorBRH.fill = motors.BRH;
                            break;
                        case PacketType.GET_ORIEN:
                            EnqueuePacket(PacketType.GET_ORIEN, packetFlag, JsonSerializer.ToJsonString(rc.orientation.Get()));
                            break;
                        case PacketType.SET_ORIEN:
                            rc.orientation.Set(JsonSerializer.Deserialize<JSON.Orientation>(jsonFromClient));
                            break;
                        case PacketType.GET_SENS:
                            EnqueuePacket(PacketType.GET_SENS, packetFlag, JsonSerializer.ToJsonString(rc.allSensors.Get()));
                            break;
                        case PacketType.PING:
                            JSON.Ping ping = JsonSerializer.Deserialize<JSON.Ping>(jsonFromClient);
                            long clientTimestamp = ping.timestamp;
                            ping.timestamp = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
                            ping.ping = ping.timestamp - clientTimestamp;
                            EnqueuePacket(PacketType.PING, packetFlag, JsonSerializer.ToJsonString(ping));
                            break;
                        case PacketType.GET_DETE:
                            JSON.Detection detection = new JSON.Detection();
                            MainThreadUpdateWorker detectionWorker = new MainThreadUpdateWorker()
                            {
                                action = () => { 
                                    detection = simulationControllerInstance.GetDetection();
                                    EnqueuePacket(PacketType.GET_DETE, packetFlag, JsonSerializer.ToJsonString(detection));
                                    if (!packetFlag.HasFlag(Flag.DO_NOT_LOG_PACKET)) Debug.Log(JsonSerializer.ToJsonString(detection));
                                }
                            };
                            simulationControllerInstance.mainThreadUpdateWorkers.Enqueue(detectionWorker);
                            break;
                        case PacketType.ACK:
                            EnqueuePacket(PacketType.ACK, packetFlag | Flag.TEST, "{\"info\":\"ack ack\"}");
                            break;
                        case PacketType.GET_DEPTH:
                            MainThreadUpdateWorker depthWorker = new MainThreadUpdateWorker()
                            {
                                action = () => {
                                    byte[] map = simulationControllerInstance.GetDepthMap();
                                    EnqueuePacket(PacketType.GET_DEPTH, packetFlag, "{\"depth\":\"" + System.Convert.ToBase64String(map) + "\"}");
                                }
                            };
                            simulationControllerInstance.mainThreadUpdateWorkers.Enqueue(depthWorker);
                            break;
                        case PacketType.GET_DEPTH_BYTES:
                            depthWorker = new MainThreadUpdateWorker()
                            {
                                action = () => {
                                    byte[] map = simulationControllerInstance.GetDepthMap();
                                    EnqueuePacket(PacketType.GET_DEPTH_BYTES, packetFlag, map, map.Length, false);
                                }
                            };
                            simulationControllerInstance.mainThreadUpdateWorkers.Enqueue(depthWorker);
                            break;
                        case PacketType.GET_VIDEO_BYTES:
                            depthWorker = new MainThreadUpdateWorker()
                            {
                                action = () =>
                                {
                                    byte[] map = simulationControllerInstance.GetVideo();
                                    /*  UnityEngine.Rendering.AsyncGPUReadback.Request(new ComputeBuffer(1,1), (req) =>
                                      {
                                          int w = 1280, h = 720;
                                          var newTex = new Texture2D
                                          (
                                              w,
                                              h,
                                              TextureFormat.RGB24,
                                              false
                                          );

                                          newTex.LoadRawTextureData(req.GetData<uint>());

                                          newTex.Apply();

                                          map = ImageConversion.EncodeToPNG(newTex);
                                      });*/
                                    EnqueuePacket(PacketType.GET_VIDEO_BYTES, packetFlag, map, map.Length, false);
                                }
                            };
                            simulationControllerInstance.mainThreadUpdateWorkers.Enqueue(depthWorker);
                            break;
                        case (PacketType)0xFF:
                            Debug.Log("got illegal 0xFF packet type");
                            clientConnected = false;
                            break;
                        default:
                            Debug.LogWarning("Unknown dataframe type " + System.BitConverter.ToString(new byte[] { (byte)packetType }));
                            SendJson(PacketType.ACK, "{'info':'Something went wrong. You shouldn't get this packet. Unknown dataframe packet!', 'fromClient':'" + jsonFromClient + "'}");
                            break;
                    }

                    if (packetFlag.HasFlag(Flag.SERVER_ECHO)) EnqueuePacket(PacketType.ACK, Flag.None, "{'fromClient':'" + jsonFromClient + "'}");
                    ArrayPool<byte>.Shared.Return(dataFromClient);
                    #endregion
                }
                
                while (!toSend.IsEmpty) if (toSend.TryDequeue(out Packet packet)) Send(packet.packetType, packet.flag, packet.bytes);
            }
        }
        catch (Exception exp)
        {
            Debug.LogError("Json client exception\n" + jsonFromClient + "\n" + exp.Message + '\n' + exp.StackTrace);
            clientConnected = false;
        }

        Debug.Log("Json client disconnected");
        stream?.Dispose();
        client?.Dispose();
        connectedClients--;
    }

    public void EnqueuePacket(PacketType packetType, Flag packetFlag, string json)
    {
        byte[] bytes = ArrayPool<byte>.Shared.Rent(Encoding.ASCII.GetMaxByteCount(json.Length));
        int len = Encoding.ASCII.GetBytes(json, 0, json.Length, bytes, 0);
        toSend.Enqueue(new Packet(packetType, packetFlag, bytes, len, true));
    }

    public void EnqueuePacket(PacketType packetType, Flag packetFlag, byte[] bytes, int len, bool rented) => toSend.Enqueue(new Packet(packetType, packetFlag, bytes, len, rented));
    
    void Send(PacketType packetType, Flag packetFlag, string json)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(json);
        stream.WriteByte((byte)packetType);
        stream.WriteByte((byte)packetFlag);
        stream.Write(BitConverter.GetBytes(bytes.Length), 0, 4);
        stream.Write(bytes, 0, bytes.Length);
        if (!packetFlag.HasFlag(Flag.DO_NOT_LOG_PACKET))
            Debug.Log(Enum.GetName(typeof(PacketType), packetType) + " len: " + bytes.Length.ToString());
    }

    void Send(PacketType packetType, Flag packetFlag, byte[] bytes)
    {
        stream.WriteByte((byte)packetType);
        stream.WriteByte((byte)packetFlag);
        stream.Write(BitConverter.GetBytes(bytes.Length), 0, 4);
        stream.Write(bytes, 0, bytes.Length);
        if (!packetFlag.HasFlag(Flag.DO_NOT_LOG_PACKET))
            Debug.Log(Enum.GetName(typeof(PacketType), packetType) + " len: " + bytes.Length.ToString());
    }

    void SendJson(PacketType packetType, string json)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(json);
        stream.WriteByte((byte)packetType);
        stream.WriteByte((byte)0);
        stream.Write(BitConverter.GetBytes(bytes.Length), 0, 4);
        stream.Write(bytes, 0, bytes.Length);
        Debug.Log(Enum.GetName(typeof(PacketType), packetType) + " len: " + bytes.Length.ToString());
    }

    void SendBytes(PacketType packetType, byte[] bytes)
    {
        stream.WriteByte((byte)packetType);
        stream.WriteByte((byte)0);
        stream.Write(BitConverter.GetBytes(bytes.Length), 0, 4);
        stream.Write(bytes, 0, bytes.Length);
        Debug.Log(Enum.GetName(typeof(PacketType), packetType) + " len: " + bytes.Length.ToString());
    }

    public void Stop()
    {
        clientConnected = false;
        talking.Abort();
    }

    private void ReadAllFromStream(NetworkStream stream, byte[] buffer, int len)
    {
        int current = 0;
        while (current < len)
            current += stream.Read(buffer, current, len - current > len ? len : len - current);
    }

    private static byte ReadByteFromStream(NetworkStream stream)
    {
        int ret;
        do ret = stream.ReadByte();
        while (ret == -1);
        return (byte)ret;
    }

   
}