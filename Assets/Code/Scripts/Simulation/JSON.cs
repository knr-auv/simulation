using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JSON : MonoBehaviour
{
    public class PID
    {
        public float p, i, d, windupCap, gain, limit;
    }

    public class PIDs
    {
        public PID roll = new PID();
        public PID pitch = new PID();
        public PID yaw = new PID();
        public PID depth = new PID();
        public PID rollSpeed = new PID();
        public PID pitchSpeed = new PID();
        public PID yawSpeed = new PID();
    }
    
    public class AcroOptions
    {
        public Vec3 rotSpeed = new Vec3();
        public Vec3 vel = new Vec3();
    }
    
    public class StableOptions
    {
        public Vec3 rot = new Vec3();
        public Vec3 vel = new Vec3();
        public float depth = 0;
    }
    
    public class Motors
    {
        public float FLH, FLV, BLV, BLH, FRH, FRV, BRV, BRH;
    }

    public class Sensors
    {
        public Vec3 rot = new Vec3();
        public Vec3 rotSpeed = new Vec3();
        public Vec3 accel = new Vec3();
        public Vec3 angularAccel = new Vec3();
        public Baro baro = new Baro();
    }

    public class Baro
    {
        public float pressure;
    }

    public class Settings
    {
        public int quality;
    }

    public class Connection
    {
        int timestamp;
        int ping;
    }

    public class Orientation
    {
        public Vec3 rot = new Vec3();
        public Vec3 pos = new Vec3();
    }
    
    public class Ping
    {
        public long timestamp, ping;
    }

    public class Detection
    {
        public List<DetectedObject> detected;
    }   
    
    public class DetectedObject
    {
        public bool visibleInFrame;
        public Vec2 min, max;
        public float fill;
        public string className;
        public float distance;
        public float colorPercentVisible;
    }

    public class Vec2
    {
        public float x, y;
    }
    
    public class Vec3
    {
        public float x, y, z;
    }
}
