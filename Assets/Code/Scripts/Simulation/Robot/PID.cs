using System;
using UnityEditor.Experimental.GraphView;

    public class PID
    {
        public float pCoef, iCoef, dCoef, kCoef, tCoef;
        public float pTerm, iTerm, dTerm;
        public float windupCap, oldTime, oldError;
        public float limit;
        
        public PID(float p, float i, float d, float k, float windup, float limit, float t = 0)
        {
            pCoef = p;
            iCoef = i;
            dCoef = d;
            kCoef = k;
            tCoef = t;
            windupCap = windup;
            this.limit = limit;
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

        public float ControlWithLimits(float target, float current, float time)
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
            float pid = (pCoef * pTerm + iCoef * iTerm + dCoef * dTerm) * kCoef;
            return pid > limit ? limit : pid < -limit ? -limit : pid;
        }
        
        public float ControlWithBack(float target, float current, float time, float min, float max)
        {
            float error = target - current;
            float dt = time - oldTime;

            pTerm = error;
            iTerm += error * dt;
            if (dt == 0) dTerm = 0;
            else dTerm = (error - oldError) / dt;

            oldError = error;
            oldTime = time;
            float pid = (pCoef * pTerm + iCoef * iTerm + dCoef * dTerm) * kCoef;
            float pidCapped = pid > max ? max : pid < min ? min : pid;
            float diff = pidCapped - pid;
            iTerm += tCoef * diff * dt / iCoef; // Warning works only if dt = const (errror-X)*dt => error*dt - X*dt 
            return pidCapped;
        }
        
        public float ControlDt(float target, float current, float dt)
        {
            float error = target - current;

            pTerm = error;
            iTerm += error * dt;
            iTerm = iTerm > windupCap ? windupCap : iTerm < -windupCap ? -windupCap : iTerm;
            if (dt == 0) dTerm = 0;
            else dTerm = (error - oldError) / dt;

            oldError = error;
            return (pCoef * pTerm + iCoef * iTerm + dCoef * dTerm) * kCoef;
        }

        public void SetValues(JSON.PID options)
        {
            pCoef = options.p;
            iCoef = options.i;
            dCoef = options.d;
            kCoef = options.gain;
            limit = options.limit;
            windupCap = options.windupCap;
        }
        public void GetValues(ref JSON.PID options)
        {
            options.p = pCoef;
            options.i = iCoef;
            options.d = dCoef;
            options.gain = kCoef;
            options.limit = limit;
            options.windupCap = windupCap;
        }
    }
