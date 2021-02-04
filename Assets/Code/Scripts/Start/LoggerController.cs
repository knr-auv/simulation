using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LoggerController : MonoBehaviour
{
    void Awake()
    {
        Application.logMessageReceivedThreaded += HandleLogMessage;
        DontDestroyOnLoad(gameObject);
    }

    void HandleLogMessage(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error)
            File.AppendAllText("error.log", "[" + DateTime.Now.ToString("dddd, dd MMMM yyyy HH:mm:ss") + "] " + logString + "\n" + stackTrace + "\n");
        if (Settings.config != null && Settings.config.loggingEnabled && type == LogType.Log)
        {
            File.AppendAllText("debug.log", "[" + DateTime.Now.ToString("dddd, dd MMMM yyyy HH:mm:ss") + "] " + logString + "\n" + stackTrace + "\n");
            File.AppendAllText("debugSmall.log", "[" + DateTime.Now.ToString("dddd, dd MMMM yyyy HH:mm:ss") + "] " + logString + "\n");
        }
    }
}
