using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Config
{
    //Start
    public string mode { get; set; }
    public bool loggingEnabled { get; set; }
    public SimulationOptions simulationOptions { get; set; }
    public DatasetOptions datasetOptions { get; set; }
    
    public Config()
    {
        LoadDefaults();
    }

    public void Check(StartController instance)
    {//TODO: Check config
        if (mode != "simulation" && mode != "dataset")
            throw new Exception("Unknown " + mode + " mode, possible modes: 'simulation' or 'dataset'");
        if (mode == "simulation")
        {
            if (simulationOptions.videoPort <= 0 || simulationOptions.videoPort > 65536)
                throw new Exception("Video Port must be in range [0-65536], not " + simulationOptions.videoPort);
            if (simulationOptions.webAPIPort <= 0 || simulationOptions.webAPIPort > 65536)
                throw new Exception("Video Port must be in range [0-65536], not " + simulationOptions.webAPIPort);
            if (simulationOptions.webAPIPort == simulationOptions.videoPort)
                throw new Exception("Video port cannot be equal to WebAPI port");
        }
        else if (mode == "dataset")
        {
            if (datasetOptions.datasetDirPath == null)
                throw new Exception("datasetFilePath cannot be null");
            if (Directory.Exists(Settings.config.datasetOptions.datasetDirPath))
            {
                while (Directory.Exists(Settings.config.datasetOptions.datasetDirPath))
                    Settings.config.datasetOptions.datasetDirPath += "_";
                instance.LogWarning("dataset directory already exists, creating new: " + Settings.config.datasetOptions.datasetDirPath);
                //throw new Exception("datasetDirPath directory exist, set overrideDataset to true to override existing dataset (Override will delete previous dataset)[OVERRIDE NOT WORKING, CHANGE NAME]");
            }
        }
    }

    public void LoadDefaults()
    {
        mode = "dataset";
        loggingEnabled = false;

        simulationOptions = new SimulationOptions()
        {
            videoPort = 44209,
            videoQuality = 60,
            webAPIPort = 44210
        };

        datasetOptions = new DatasetOptions()
        {
            minCameraDistanceToObject = 0,
            maxCameraDistanceToObject = 30,
            minObjectFill = .005f,
            objectFillOffset = .01f,
            datasetDirPath = "dataset",
            overrideDataset = true,
            cameraLookAtObjectPitchOffset = 30,
            cameraLookAtObjectYawOffset = 30,
            cameraLookAtObjectRollOffset = 30,
            cameraLookRandomlyPitchOffset = 45,
            cameraLookRandomlyRollOffset = 45,
            testPointsNum = 1000,
            percentClosest = 0.05f,
            closestMaxDistanceOffset = 1f,
            minObjectColorPercentVisible = 0.15f,
            checkFogVisibility = true
        };

        datasetOptions.graphicsOptions = new GraphicsOptions()
        {
            fogEnabled = true,
            randomizeFogDensity = true,
            fogDensity = 0.15f,
            fogColorRandomized = true,
            fogColor = new List<float>() { 0f, 0.64f, 1f },
            cameraBackgroundColor = new List<float>() { 0f, 0.64f, 1f },
            bloomIntensity = 1.89f,
            ambientOcclusionIntensity = 0.26f,
            mixerRedOutRedIn = 62,
            mixerGreenOutGreenIn = 100,
            mixerBlueOutBlueIn = 100,
            grainIntesity = 0.3f,
            randomizeLightIntesity = true,
            lightIntesity = 1f,
            ambientLightColor = new List<float>() { 0f, 0.64f, 1f },
            ambientLightIntesity = 0.1f,
            cameraFocalLength = 2.550718f,
            cameraSensorSize = new List<float>() { 4.8f, 3.6f }
        };

        datasetOptions.debugOptions = new DatasetOptions.DebugOptions()
        {
            drawDetectionRect = true,
            drawPostProcessDebug = false,
            disableScreenshot = false,
            logDetection = true
        };

        //TODO
    }
}

public class SimulationOptions
{
    public int videoPort { get; set; }
    public int webAPIPort { get; set; }
    public int videoQuality { get; set; }
    public string selectedWaterContainer { get; set; }
    public string selectedRobot { get; set; }
    public List<string> selectedRandomObjects { get; set; }
    public float fixedDeltaTime { get; set; }
    public int depthMapQuality { get; set; }
    public float depthMapScale { get; set; }
    public int videoFeedQuality { get; set; }
    public float videoFeedScale { get; set; }
} 

public class DatasetOptions
{
    public List<string> classNames { get; set; }
    public List<string> selectedObjects { get; set; }
    public string selectedWaterContainer { get; set; }
    public int classDetectedFrameNum { get; set; }
    public int classMultipleFrameNum { get; set; }
    public int classBlankFrameNum { get; set; }
    public int classObstacleFrameNum { get; set; }
    public int minVisibleMultipleObjectsNum { get; set; }
    public int minCameraDistanceToObject { get; set; }
    public int maxCameraDistanceToObject { get; set; }
    public float minObjectFill { get; set; }
    public float minObjectColorPercentVisible { get; set; }
    public bool checkFogVisibility { get; set; }
    public float percentClosest { get; set; }
    public float closestMaxDistanceOffset { get; set; }
    public float objectFillOffset { get; set; }
    public string datasetDirPath { get; set; }
    public bool overrideDataset { get; set; }
    public float cameraLookAtObjectPitchOffset { get; set; }
    public float cameraLookAtObjectYawOffset { get; set; }
    public float cameraLookAtObjectRollOffset { get; set; }
    public float cameraLookRandomlyPitchOffset { get; set; }
    public float cameraLookRandomlyRollOffset { get; set; }
    public int testPointsNum { get; set; }

    public GraphicsOptions graphicsOptions { get; set; }
    public DebugOptions debugOptions { get; set; }
    
    public class DebugOptions
    {
        public bool drawDetectionRect { get; set; }
        public bool drawPostProcessDebug { get; set; }
        public bool disableScreenshot { get; set; }
        public bool logDetection { get; set; }
    }
}

public class GraphicsOptions
{
    //Fog
    public bool fogColorRandomized { get; set; }
    public bool fogEnabled { get; set; }
    public bool randomizeFogDensity { get; set; }
    public float fogDensity { get; set; }
    public List<float> fogColor { get; set; }

    //PP
    public float bloomIntensity { get; set; }
    public float mixerRedOutRedIn { get; set; }
    public float mixerGreenOutGreenIn { get; set; }
    public float mixerBlueOutBlueIn { get; set; }
    public float grainIntesity { get; set; }
    public float ambientOcclusionIntensity { get; set; }

    //Lights
    public bool randomizeLightIntesity { get; set; }
    public float lightIntesity { get; set; }
    public List<float> ambientLightColor { get; set; }
    public float ambientLightIntesity { get; set; }

    //Camera
    public List<float> cameraBackgroundColor { get; set; }
    public float cameraFocalLength { get; set; }
    public List<float> cameraSensorSize { get; set; }
}

/*
 * - Config
 *   - Simulation (Aquarium|Syulacja)
 *     - ...
 *   - Dataset Generation (|Generacja   )
 *     - Generation
 *       - Objects
 *         - Selected class names
 *         - Selected objects
 *         - Selected water container
 *       - Quantity
 *         - Visible single objects frame num
 *         - Visible multiple objects frane num
 *         - Blank frame num
 *       - Object detection 
 *         - Min Screen fill
 *         - Min/max distance to camera for single mode
 *         - Bounding box offset
 *     - Graphics
 *       - Fog
 *         - Randomized
 *         - Color
 *         - Density
 *       - Post Processing
 *         - Color grading
 *           - Red io
 *           - Green io
 *           - Blue io
 *         - Bloom
 *           - Intesity
 *         - Grain
 *           - Intesity
 *       - Lights
 *         - Randomized
 *         - Intesity
 *       - Ambient Light
 *         - Intesity
 *       - Camera
 *         - Focal length
 *         - Sensor size
 *         - background color
 */