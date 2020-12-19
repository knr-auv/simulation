using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Utf8Json;
public class WAPIClient
{
    private enum Packet : byte
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
        REC_STRT = 0xD0,
        REC_ST = 0xD1,
        REC_RST = 0xD2,
        GET_REC = 0xD3,
        PING = 0xC5,
        GET_DETE = 0xDE
    }

    [Flags]
    private enum Flag
    {
        None = 0,
        SERVER_ECHO = 1,
        DO_NOT_LOG_PACKET = 2,
        TEST = 128
    }

    static int clientId = 0;
    public static int connectedClients = 0;
    readonly TcpClient client;
    NetworkStream stream;
    public int id;
    readonly Thread talking;
    readonly SimulationController simulationControllerInstance;
    bool clientConnected;
    readonly StringBuilder sb = new StringBuilder();

    public WAPIClient(TcpClient client, SimulationController simulationControllerInstance)
    {
        this.client = client;
        id = clientId++;
        connectedClients++;
        this.simulationControllerInstance = simulationControllerInstance;
        clientConnected = true;

        talking = new Thread(HandleJsonClient)
        {
            IsBackground = true
        };
        talking.Start();
    }

    void HandleJsonClient()
    {
        stream = client.GetStream();
        stream.ReadTimeout = 1000*60*5;//TODO
        Packet packetType;
        Flag packetFlag;
        string jsonFromClient = "";
        RobotController rc = simulationControllerInstance.robotController;
        while (clientConnected)
        {
            try
            {
                do
                {
                    #region JSON_recv
                    packetType = (Packet)ReadByteFromStream(stream);
                    packetFlag = (Flag)ReadByteFromStream(stream);
                    byte[] dataLenBytes = new byte[4];
                    ReadAllFromStream(stream, dataLenBytes, 4);
                    int dataLength = System.BitConverter.ToInt32(dataLenBytes, 0);
                    byte[] dataFromClient = new byte[dataLength];
                    ReadAllFromStream(stream, dataFromClient, dataLength);
                    jsonFromClient = Encoding.ASCII.GetString(dataFromClient, 0, dataLength);
                    
                    switch (packetType)
                    {
                        case Packet.SET_MTR:
                            if (!packetFlag.HasFlag(Flag.DO_NOT_LOG_PACKET)) Debug.Log("From client: " + jsonFromClient);
                            var motors = JsonSerializer.Deserialize<JSON.Motors>(jsonFromClient);
                            rc.motorFL.fill = motors.FL;
                            rc.motorFR.fill = motors.FR;
                            rc.motorB.fill = motors.B;
                            rc.motorML.fill = motors.ML;
                            rc.motorMR.fill = motors.MR;
                            break;
                        case Packet.GET_ORIEN:
                            SendJson(Packet.GET_ORIEN, packetFlag, JsonSerializer.ToJsonString(rc.orientation.Get()));
                            break;
                        case Packet.SET_ORIEN:
                            rc.orientation.Set(JsonSerializer.Deserialize<JSON.Orientation>(jsonFromClient));
                            break;
                        case Packet.GET_SENS:
                            SendJson(Packet.GET_SENS, packetFlag, JsonSerializer.ToJsonString(rc.allSensors.Get()));
                            break;
                        case Packet.PING:
                            JSON.Ping ping = JsonSerializer.Deserialize<JSON.Ping>(jsonFromClient);
                            long clientTimestamp = ping.timestamp;
                            ping.timestamp = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
                            ping.ping = ping.timestamp - clientTimestamp;
                            SendJson(Packet.PING, packetFlag, JsonSerializer.ToJsonString(ping));
                            break;
                        /*case Packet.SET_SIM:
                            Settings settings = new Settings();
                            TryJsonToObjectState(jsonFromClient, settings);
                            quality = settings.quality;
                            break;*/
                        case Packet.GET_DETE:
                            JSON.Detection detection = new JSON.Detection();
                            MainThreadUpdateWorker detectionWorker = new MainThreadUpdateWorker()
                            {
                                action = () => { detection = simulationControllerInstance.GetDetection(); }
                            };
                            simulationControllerInstance.mainThreadUpdateWorkers.Enqueue(detectionWorker);
                            while (!detectionWorker.done) Thread.Sleep(1);
                            if (!packetFlag.HasFlag(Flag.DO_NOT_LOG_PACKET)) Debug.Log(JsonSerializer.ToJsonString(detection));
                            SendJson(Packet.GET_DETE, packetFlag, JsonSerializer.ToJsonString(detection));
                            break;
                        case Packet.ACK:
                            SendJson(Packet.ACK, packetFlag | Flag.TEST, "{\"info\":\"ack ack\"}");
                            break;
                        case Packet.GET_DEPTH:
                            byte[] map = new byte[1];
                            MainThreadUpdateWorker depthWorker = new MainThreadUpdateWorker() {
                                action = () => { map = simulationControllerInstance.GetDepthMap(); }
                            };
                            simulationControllerInstance.mainThreadUpdateWorkers.Enqueue(depthWorker);
                            while (!depthWorker.done) Thread.Sleep(10);
                            sb.Clear();
                            sb.Append("{\"depth\":\"");
                            sb.Append(System.Convert.ToBase64String(map));
                            sb.Append("\"}");
                            SendJson(Packet.GET_DEPTH, packetFlag, sb.ToString());
                            break;
                        case Packet.GET_DEPTH_BYTES:
                            map = new byte[1];
                            depthWorker = new MainThreadUpdateWorker() {
                                action = () => { map = simulationControllerInstance.GetDepthMap(); }
                            };
                            simulationControllerInstance.mainThreadUpdateWorkers.Enqueue(depthWorker);
                            while (!depthWorker.done) Thread.Sleep(10);
                            SendBytes(Packet.GET_DEPTH_BYTES, packetFlag, map);
                            break;
                        case Packet.GET_VIDEO_BYTES:
                            map = new byte[1];
                            depthWorker = new MainThreadUpdateWorker() {
                                action = () => { map = simulationControllerInstance.GetVideo(); }
                            };
                            simulationControllerInstance.mainThreadUpdateWorkers.Enqueue(depthWorker);
                            while (!depthWorker.done) Thread.Sleep(10);
                            SendBytes(Packet.GET_VIDEO_BYTES, packetFlag, map);
                            break;
                        case (Packet)0xFF:
                            clientConnected = false;
                            break;
                        default:
                            Debug.LogWarning("Unknown dataframe type " + System.BitConverter.ToString(new byte[] { (byte)packetType }));
                            SendJson(Packet.ACK, "{'info':'Something went wrong. You shouldn't get this packet. Unknown dataframe packet!', 'fromClient':'"+jsonFromClient+"'}");
                            break;
                    }

                    if(packetFlag.HasFlag(Flag.SERVER_ECHO)) SendJson(Packet.ACK, "{'fromClient':'"+ jsonFromClient + "'}");
                    #endregion
                } while (clientConnected && stream.DataAvailable);
            }
            catch(Exception exp)    
            {
                Debug.LogError("Json client exception\n" + jsonFromClient + "\n"  + exp.Message + '\n' + exp.StackTrace);
                clientConnected = false;
            }
        }
        Debug.Log("Json client disconnected");
        stream?.Dispose();
        client?.Dispose();
        connectedClients--;
    }

    void SendJson(Packet packetType, Flag packetFlag, string json)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(json);
        stream.WriteByte((byte)packetType);
        stream.WriteByte((byte)packetFlag);
        stream.Write(BitConverter.GetBytes(bytes.Length), 0, 4);
        stream.Write(bytes, 0, bytes.Length);
        Debug.Log(Enum.GetName(typeof(Packet), packetType) + " len: " + bytes.Length.ToString());
    }

    void SendBytes(Packet packetType, Flag packetFlag, byte[] bytes)
    {
        stream.WriteByte((byte)packetType);
        stream.WriteByte((byte)packetFlag);
        stream.Write(BitConverter.GetBytes(bytes.Length), 0, 4);
        stream.Write(bytes, 0, bytes.Length);
        Debug.Log(Enum.GetName(typeof(Packet), packetType) + " len: " + bytes.Length.ToString());
    }

    void SendJson(Packet packetType, string json)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(json);
        stream.WriteByte((byte)packetType);
        stream.WriteByte((byte)0);
        stream.Write(BitConverter.GetBytes(bytes.Length), 0, 4);
        stream.Write(bytes, 0, bytes.Length);
        Debug.Log(Enum.GetName(typeof(Packet), packetType) + " len: " + bytes.Length.ToString());
    }

    void SendBytes(Packet packetType, byte[] bytes)
    {
        stream.WriteByte((byte)packetType);
        stream.WriteByte((byte)0);
        stream.Write(BitConverter.GetBytes(bytes.Length), 0, 4);
        stream.Write(bytes, 0, bytes.Length);
        Debug.Log(Enum.GetName(typeof(Packet), packetType) + " len: " + bytes.Length.ToString());
    }

    public void Stop()
    {
        clientConnected = false;
        talking.Abort();
    }

    public void ReadAllFromStream(NetworkStream stream, byte[] buffer, int len)
    {
        int current = 0;
        while (current < buffer.Length) 
            current += stream.Read(buffer, current, len - current > buffer.Length? buffer.Length : len - current);
    }

    private static byte ReadByteFromStream(NetworkStream stream)
    {
        int ret;
        do ret = stream.ReadByte();
        while (ret == -1);
        return (byte)ret;
    }
}