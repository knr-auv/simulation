using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JSON
{
    public class Motors
    {
        public float FL, FR, ML, MR, B;
    }

    public class Sensors
    {
        [System.Serializable]
        public class Gyro
        {
            public float x, y, z;
        }
        [System.Serializable]
        public class Accel
        {
            public float x, y, z;
        }
        [System.Serializable]
        public class Baro
        {
            public float pressure;
        }
        public Gyro gyro = new Gyro();
        public Accel accel = new Accel();
        public Baro baro = new Baro();
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
        [System.Serializable]
        public class Rot
        {
            public float x, y, z;
        }
        [System.Serializable]
        public class Pos
        {
            public float x, y, z;
        }
        public Rot rot = new Rot();
        public Pos pos = new Pos();
    }

    public class Ping
    {
        public long timestamp, ping;
    }

    public class Init
    {
        public int videoPort, jsonPort, quality;
    }
}
