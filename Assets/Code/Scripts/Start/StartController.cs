using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YamlDotNet;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public class StartController : MonoBehaviour
{
    [SerializeField]
    Text console;

    private bool configCorrect;
    
    void Start()
    {
        Screen.SetResolution(1280, 720, false, 61);
        //Settings.Init();TODO
        //Settings.Log("Initializing");
        configCorrect = true;
        Log("Parsing config file...");
        try
        {
            if (!File.Exists("config.yaml") || File.ReadAllText("config.yaml").Length < 10){
                var serializer = new SerializerBuilder().Build();
                var yaml = serializer.Serialize(new Config());
                File.Delete("config.yaml");
                File.WriteAllText("config.yaml", yaml);
            }

            var input = new StringReader(File.ReadAllText("config.yaml"));

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            Settings.config = deserializer.Deserialize<Config>(input);
            Settings.config.Check(this);
            
            Log("Done, press ENTER to start " + Settings.config.mode + " mode...\n");
            Log("Press ESC to quit the program");
        }
        catch (Exception e)
        {
            LogError(e.Message + "\n" + e.StackTrace + "\n" + "Config file incorrect. Delete or clear 'config.yaml' to regenerate file. Press ESC key to exit...");
            configCorrect = false;
        }
    }

    void Update()
    {
        if (Input.GetKey("escape"))
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        else if (configCorrect /*&&*/|| Input.GetKey(KeyCode.Return))
        {
            if (Settings.config.mode == "simulation") SceneManager.LoadScene("Simulation");
            if (Settings.config.mode == "dataset") SceneManager.LoadScene("DatasetGeneration");
        }
    }

    public void LogError(string text)
    {
        console.text += "<color=red>[ERROR]: " + text + "</color>" + '\n';
    }

    public void LogWarning(string text)
    {
        console.text += "<color=yellow>[WARN]: " + text + "</color>" + '\n';
    }

    public void Log(string text)
    {
        console.text += "<color=green>[INFO]: " + text + "</color>" + '\n';
    }
}