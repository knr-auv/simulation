using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class LoggerController : MonoBehaviour
{

    private ConcurrentQueue<string> toLog = new ConcurrentQueue<string>();
    private ConcurrentQueue<string> toLogSmall = new ConcurrentQueue<string>();
    private ConcurrentQueue<string> toLogError = new ConcurrentQueue<string>();
    void Awake()
    {
        Application.logMessageReceivedThreaded += HandleLogMessage;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        while (!toLog.IsEmpty)
        {
            toLog.TryDequeue(out string text);
            File.AppendAllText("debug.log", text);
        }
        while (!toLogSmall.IsEmpty)
        {
            toLogSmall.TryDequeue(out string text);
            File.AppendAllText("debugSmall.log", text);
        }
        while (!toLogError.IsEmpty)
        {
            toLogError.TryDequeue(out string text);
            File.AppendAllText("error.log", text);
        }
    }

    void HandleLogMessage(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error)
            toLogError.Enqueue("[" + DateTime.Now.ToString("dddd, dd MMMM yyyy HH:mm:ss") + "] " + logString + "\n" + stackTrace + "\n");
        if (Settings.config != null && Settings.config.loggingEnabled && type == LogType.Log)
        {
            toLog.Enqueue("[" + DateTime.Now.ToString("dddd, dd MMMM yyyy HH:mm:ss") + "] " + logString + "\n" + stackTrace + "\n");
            toLogSmall.Enqueue("[" + DateTime.Now.ToString("dddd, dd MMMM yyyy HH:mm:ss") + "] " + logString + "\n");
        }
    }
}
