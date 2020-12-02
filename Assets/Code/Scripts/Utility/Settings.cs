using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public static class Settings { 
    public static Config config;
    public static StreamWriter logs;

    public static void Init() {
       /* string fileName = "debug.log";// + DateTime.Now.ToString() + ".log";
        File.Create(fileName);
        
        logs = new StreamWriter(fileName);*/
    }

    public static void Log(string log)
    {
        using (StreamWriter writer = new StreamWriter("debug.log")) writer.WriteLine(log);
    }

    public static void Close() => logs.Close();
}
