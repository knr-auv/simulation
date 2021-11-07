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
using System.Diagnostics;
using System.Reflection;
using System.Timers;
using Debug = UnityEngine.Debug;
using Timer = System.Timers.Timer;

public class WAPIClient
{
    public enum PacketType : byte
    {
        SET_MTR = 0xA0,
        ARM_MTR = 0xA1,
        DISARM_MTR = 0xA2,
        SET_CONTROL_MODE = 0xA3,
        SET_ACRO = 0xA4,
        SET_STABLE = 0xA5,
        SET_PID = 0xA6,
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
        CHK_AP = 0xC9,
        ERROR = 0xCA,
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
    bool clientConnected, stopped;
    readonly StringBuilder sb = new StringBuilder();
    private readonly ConcurrentQueue<Packet> toSend;
    private Thread connectionChecker;
    private volatile bool shouldACK = false;

    public WAPIClient(TcpClient client, SimulationController simulationControllerInstance)
    {
        this.client = client;
        id = clientId++;
        connectedClients++;
        this.simulationControllerInstance = simulationControllerInstance;
        clientConnected = true;
        stopped = false;
        
        Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        
        toSend = new ConcurrentQueue<Packet>();

        talking = new Thread(HandleJsonClient)
        {
            IsBackground = true
        };
        talking.Start();
        connectionChecker = new Thread(CheckConnection) {IsBackground = true};
        connectionChecker.Start();

    }
    
    private void CheckConnection()
    {
        while (clientConnected)
        {
            if (shouldACK) EnqueuePacket(PacketType.ACK, Flag.None);
            else shouldACK = true;
            Thread.Sleep(500);
        }
    }


    private void HandleJsonClient()
    {
        stream = client.GetStream();
        byte[] dataLenBytes = new byte[4];
        string jsonFromClient = "";
        var sb = new StringBuilder();
        RobotController rc = simulationControllerInstance.robotController;
        try
        {
            while (clientConnected)
            {
                if (stream.DataAvailable)
                {
                    #region JSON_recv
                    var packetType = (PacketType)ReadByteFromStream(stream);
                    var packetFlag = (Flag)ReadByteFromStream(stream);
                    ReadAllFromStream(stream, dataLenBytes, 4);
                    int dataLength = System.BitConverter.ToInt32(dataLenBytes, 0);
                    shouldACK = false;
                    byte[] dataFromClient = ArrayPool<byte>.Shared.Rent(dataLength);
                  
                    ReadAllFromStream(stream, dataFromClient, dataLength);
                    jsonFromClient = Encoding.ASCII.GetString(dataFromClient, 0, dataLength);
                    if (jsonFromClient.Length != dataLength) throw new Exception("wut");
                    
                    if (!packetFlag.HasFlag(Flag.DO_NOT_LOG_PACKET))
                        Debug.Log("Received " + Enum.GetName(typeof(PacketType), packetType) + " len: " + dataLength.ToString());
                    
                    switch (packetType)
                    {
                        case PacketType.SET_MTR:
                            if (!packetFlag.HasFlag(Flag.DO_NOT_LOG_PACKET))
                                Debug.Log("From client: " + jsonFromClient);
                            if (dataLength != 0)
                            {
                                var motors = JsonSerializer.Deserialize<JSON.Motors>(jsonFromClient);
                                rc.SetRawMotors(
                                    motors.FLH,
                                    motors.FLV,
                                    motors.BLV,
                                    motors.BLH,
                                    motors.FRH,
                                    motors.FRV,
                                    motors.BRV,
                                    motors.BRH);
                            }
                            else
                            {
                                //TODO
                            }
                            break;
                        case PacketType.ARM_MTR:
                            rc.motorsArmed = true;
                            break;
                        case PacketType.DISARM_MTR:
                            rc.motorsArmed = false;
                            break;
                        case PacketType.SET_CONTROL_MODE:
                            if (dataLength != 0) rc.motorsControlMode = jsonFromClient;
                            else EnqueuePacket(PacketType.SET_CONTROL_MODE, packetFlag, rc.motorsControlMode);
                            break;
                        case PacketType.SET_ACRO:
                            var acro = JsonSerializer.Deserialize<JSON.AcroOptions>(jsonFromClient);
                            rc.targetRotationSpeed.x = acro.rot_speed.x;
                            rc.targetRotationSpeed.y = acro.rot_speed.y;
                            rc.targetRotationSpeed.z = acro.rot_speed.z;
                            rc.velocity.x = acro.vel.x;
                            rc.velocity.y = acro.vel.y;
                            rc.velocity.z = acro.vel.z;
                            break;
                        case PacketType.SET_STABLE:
                            if (dataLength != 0)
                            {
                                var stable = JsonSerializer.Deserialize<JSON.StableOptions>(jsonFromClient);
                                rc.targetRotation.x = stable.rot.x;
                                rc.targetRotation.y = stable.rot.y;
                                rc.targetRotation.z = stable.rot.z;
                                rc.velocity.x = stable.vel.x;
                                rc.velocity.y = stable.vel.y;
                                rc.velocity.z = stable.vel.z;
                                rc.targetDepth = stable.depth;
                            }
                            else
                            {
                                var stable = new JSON.StableOptions();
                                stable.rot.x = rc.targetRotation.x;
                                stable.rot.y = rc.targetRotation.y;
                                stable.rot.z = rc.targetRotation.z;
                                stable.vel.x = rc.velocity.x;
                                stable.vel.y = rc.velocity.y;
                                stable.vel.z = rc.velocity.z;
                                stable.depth = rc.targetDepth;
                                EnqueuePacket(PacketType.SET_STABLE, packetFlag, JsonSerializer.ToJsonString(stable));
                            }
                            break;
                        case PacketType.SET_PID:
                            if (dataLength != 0)
                            {
                                var pids = JsonSerializer.Deserialize<JSON.PIDs>(jsonFromClient);
                               
                                rc.rollPID.SetValues(pids.roll);
                                rc.pitchPID.SetValues(pids.pitch);
                                rc.yawPID.SetValues(pids.yaw);
                                rc.depthPID.SetValues(pids.depth);
                            }
                            else
                            {
                                var pids = new JSON.PIDs();
                                rc.rollPID.GetValues(ref pids.roll);
                                rc.pitchPID.GetValues(ref pids.pitch);
                                rc.yawPID.GetValues(ref pids.yaw);
                                rc.depthPID.GetValues(ref pids.depth);
                                var str = JsonSerializer.ToJsonString<JSON.PIDs>(pids);
                                EnqueuePacket(PacketType.SET_PID, packetFlag, str);
                            }
                            
                            break;
                        
                        case PacketType.GET_SENS:
                            EnqueuePacket(PacketType.GET_SENS, packetFlag, JsonSerializer.ToJsonString(rc.allSensors.Get()));
                            break;
                        case PacketType.GET_DEPTH:
                            MainThreadUpdateWorker depthWorker = new MainThreadUpdateWorker()
                            {
                                action = () => {
                                    byte[] map = simulationControllerInstance.GetDepthMap();
                                    EnqueuePacket(PacketType.GET_DEPTH, packetFlag, System.Convert.ToBase64String(map));
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
                                    EnqueuePacket(PacketType.GET_VIDEO_BYTES, packetFlag, map, map.Length, false);
                                }
                            };
                            simulationControllerInstance.mainThreadUpdateWorkers.Enqueue(depthWorker);
                            break;
                        
                        case PacketType.ACK:
                            EnqueuePacket(PacketType.ACK, packetFlag | Flag.TEST, "{\"info\":\"ack ack\"}");
                            break;
                        case PacketType.GET_ORIEN:
                            EnqueuePacket(PacketType.GET_ORIEN, packetFlag, JsonSerializer.ToJsonString(rc.orientation.Get()));
                            break;
                        case PacketType.SET_ORIEN:
                            rc.orientation.Set(JsonSerializer.Deserialize<JSON.Orientation>(jsonFromClient));
                            break;
                        case PacketType.RST_SIM:
                            MainThreadUpdateWorker resetWorker = new MainThreadUpdateWorker()
                            {
                                action = () => { GameObject.FindGameObjectWithTag("SimulationController").GetComponent<SimulationController>().PlaceRobotInStartZone(); }
                            };
                            simulationControllerInstance.mainThreadUpdateWorkers.Enqueue(resetWorker);
                            simulationControllerInstance.SendToClients(PacketType.RST_SIM, Flag.None);
                            break;
                        case PacketType.PING:
                            EnqueuePacket(PacketType.PING, packetFlag, jsonFromClient);
                            break;
                        case PacketType.GET_CPS:
                            MainThreadUpdateWorker checkpointWorker = new MainThreadUpdateWorker()
                            {
                                action = () => {
                                    string ret = "[";
                                    foreach (var obj in GameObject.FindGameObjectsWithTag("Checkpoint"))
                                        ret += "{\"id\":\"" + obj.GetComponent<CheckpointController>().id + "\",\"reached\":" + obj.GetComponent<CheckpointController>().reached.ToString().ToLower() + "},";
                                    ret += "]";
                                    ret = ret.Replace(",]", "]");
                                    EnqueuePacket(PacketType.GET_CPS, packetFlag, ret);
                                }
                            };
                            simulationControllerInstance.mainThreadUpdateWorkers.Enqueue(checkpointWorker);
                            break;
                        case PacketType.CHK_AP:
                            string id = jsonFromClient;
                            var actionpointWorker = new MainThreadUpdateWorker()
                            {
                                action = () =>
                                {
                                    if (id.Length != 0)
                                    {
                                        bool ret = false;
                                        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Actionpoint"))
                                        {
                                            ActionpointController apc = obj.GetComponent<ActionpointController>();
                                            if (apc.id.Equals(id, StringComparison.Ordinal)) ret = apc.active;
                                        }
                                        if(ret)EnqueuePacket(PacketType.CHK_AP, Flag.None, "true");
                                        else EnqueuePacket(PacketType.CHK_AP, Flag.None, "false");
                                    }
                                    else
                                    {
                                        sb.Clear();
                                        sb.Append('[');
                                        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Actionpoint"))
                                        {
                                            ActionpointController apc = obj.GetComponent<ActionpointController>();
                                            sb.Append(apc.id);
                                            sb.Append(",");
                                        }

                                        if (sb[sb.Length - 1] == ',') sb[sb.Length - 1] = ']';
                                        else sb.Append("]");
                                        EnqueuePacket(PacketType.CHK_AP, Flag.None, sb.ToString());
                                    }
                                }
                            };
                            simulationControllerInstance.mainThreadUpdateWorkers.Enqueue(actionpointWorker);
                            break;
                        
                        case PacketType.GET_DETE:
                            MainThreadUpdateWorker detectionWorker = new MainThreadUpdateWorker()
                            {
                                action = () => { 
                                    var detection = simulationControllerInstance.GetDetection();
                                    EnqueuePacket(PacketType.GET_DETE, packetFlag, JsonSerializer.ToJsonString(detection));
                                }
                            };
                            simulationControllerInstance.mainThreadUpdateWorkers.Enqueue(detectionWorker);
                            break;
                        
                        case (PacketType)0xFF:
                            Debug.LogWarning("got illegal 0xFF packet type");
                            clientConnected = false;
                            break;
                        default:
                            Debug.LogWarning("Unknown dataframe type " + System.BitConverter.ToString(new byte[] { (byte)packetType }));
                            EnqueuePacket(PacketType.ERROR, Flag.None, "{\"info\":\"Something went wrong. You shouldn't get this packet. Unknown dataframe packet!\", \"fromClient\":\"" + jsonFromClient + "\"}");
                            break;
                    }
                    
                    if (packetFlag.HasFlag(Flag.SERVER_ECHO)) EnqueuePacket(PacketType.ACK, Flag.None, "{\"fromClient\":\"" + jsonFromClient + "\"}");
                    ArrayPool<byte>.Shared.Return(dataFromClient);
                    #endregion
                }
                else
                {
                    Thread.Sleep(1);
                }

                while (!toSend.IsEmpty)
                {
                    if (toSend.TryDequeue(out Packet packet))
                    {
                        Send(packet.packetType, packet.flag, packet.bytes, packet.length);
                        if(packet.rented) ArrayPool<byte>.Shared.Return(packet.bytes);
                    }
                }
            }
        }
        catch (Exception exp)
        {
            Debug.LogError("Json client exception\n" + jsonFromClient + "\n" + exp.Message + '\n' + exp.StackTrace);
            clientConnected = false;
        }
        
        Stop();
    }

    public void EnqueuePacket(PacketType packetType, Flag packetFlag, string json)
    {
        byte[] bytes = ArrayPool<byte>.Shared.Rent(Encoding.ASCII.GetMaxByteCount(json.Length));
        int len = Encoding.ASCII.GetBytes(json, 0, json.Length, bytes, 0);
        toSend.Enqueue(new Packet(packetType, packetFlag, bytes, len, true));
    }
    public void EnqueuePacket(PacketType packetType, Flag packetFlag) => toSend.Enqueue(new Packet(packetType, packetFlag, null, 0, false));
    public void EnqueuePacket(PacketType packetType, Flag packetFlag, byte[] bytes, int len, bool rented) => toSend.Enqueue(new Packet(packetType, packetFlag, bytes, len, rented));
    
    void Send(PacketType packetType, Flag packetFlag, byte[] bytes, int length)
    {
        stream.WriteByte((byte)packetType);
        stream.WriteByte((byte)packetFlag);
        if (length == 0 || bytes == null)
        {
            stream.Write(BitConverter.GetBytes(0), 0, 4);
        }
        else
        {
            stream.Write(BitConverter.GetBytes(length), 0, 4);
            stream.Write(bytes, 0, length);
        }
        
        if (!packetFlag.HasFlag(Flag.DO_NOT_LOG_PACKET))
            Debug.Log("Sent " + Enum.GetName(typeof(PacketType), packetType) + " len: " + length.ToString());
    }

    public void Stop()
    {
        if (stopped) return;
        stopped = true;
        clientConnected = false;
        connectedClients--;
        simulationControllerInstance.wapiClients.TryRemove(id, out WAPIClient wc);
        Debug.Log("Json client disconnected");
        talking.Abort();
        connectionChecker.Abort();
        stream?.Dispose();
        client?.Dispose();
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