using System;
using UnityEditor.Experimental.GraphView;

    public class PID
    {
        public float pCoef, iCoef, dCoef, kCoef;
        public float pTerm, iTerm, dTerm;
        public float windupCap, oldTime, oldError;
        
        public PID(float p, float i, float d, float k, float windup)
        {
            pCoef = p;
            iCoef = i;
            dCoef = d;
            kCoef = k;
            windupCap = windup;
            oldTime = 0;
            oldError = 0;
            iTerm = 0;
        }
        
        public float Control(float target, float current, float time)
        {
            float error = target - current;
            float dt = time - oldTime;

            pTerm = error;
            iTerm += error * dt;
            iTerm = iTerm > windupCap ? windupCap : iTerm < -windupCap ? -windupCap : iTerm;
            if (dt == 0) dTerm = 0;
            else dTerm = (error - oldError) / dt;

            oldError = error;
            oldTime = time;
            return (pCoef * pTerm + iCoef * iTerm + dCoef * dTerm) * kCoef;
        }
    }
