using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JSON
{
    [System.Serializable]
    public class Motors
    {
        public float FL, FR, ML, MR, B;
    }

    [System.Serializable]
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

    [System.Serializable]
    public class Settings
    {
        public int quality;
    }

    [System.Serializable]
    public class Connection
    {
        int timestamp;
        int ping;
    }

    [System.Serializable]
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
}
