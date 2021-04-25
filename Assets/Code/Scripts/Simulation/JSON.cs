using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JSON : MonoBehaviour
{
    public class Motors
    {
        public float FLH, FLV, BLV, BLH, FRH, FRV, BRV, BRH;
    }

    public class Sensors
    {
        public Gyro gyro = new Gyro();
        public Rot_speed rot_speed = new Rot_speed();
        public Accel accel = new Accel();
        public Angular_accel angular_accel = new Angular_accel();
        public Baro baro = new Baro();
    }

    public class Gyro
    {
        public float x, y, z;
    }

    public class Accel
    {
        public float x, y, z;
    }

    public class Rot_speed
    {
        public float x, y, z;
    }

    public class Angular_accel
    {
        public float x, y, z;
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
        public Rot rot = new Rot();
        public Pos pos = new Pos();
    }
    public class Rot
    {
        public float x, y, z;
    }

    public class Pos
    {
        public float x, y, z;
    }

    public class Ping
    {
        public long timestamp, ping;
    }

    public class Init
    {
        public int videoPort, jsonPort, quality;
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
}
