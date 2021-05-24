
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using UnityStandardAssets.Water;

public class DatasetGeneratorController : MonoBehaviour
{
    [SerializeField]
    public GameObject cameraObject;

    [Header("Known objects")]
    [SerializeField]//TODO add list like in sim
    public GameObject flareRed1, flareRed2, gate1, gate2, gate3, gate4, waterpool1, obstacle1, tripod1, diver1, flareYellow1;

    [Header("Lights")]
    [SerializeField]
    public Light light1, light2;

    PostProcessDebug postDebug;
    PostProcessLayer postLayer;
    PostProcessVolume postVolume;

    List<GameObject> selectedObjects;
    List<GameObject> knownObjects;
    GameObject selectedWaterContainer;
    Dictionary<string, int> createdFramesPerClass;
    Dictionary<string, int> objectsNumInClass = new Dictionary<string, int>();
    public int counter = 0;

    enum GenerationState
    {
        SingleObjectsSetup,
        SingleObjects,
        MultipleObjectsSetup,
        MultipleObjects,
        NoObjectsSetup,
        NoObjects,
        Finished,
        Illegal
    };

    void Start()
    {
        if (Settings.config == null || Settings.config.mode == null)
        {
            SceneManager.LoadScene("Start");
            return;//TODO take config
        }

        Application.targetFrameRate = 5;

        postDebug = cameraObject.GetComponent<PostProcessDebug>();
        postLayer = cameraObject.GetComponent<PostProcessLayer>();
        postVolume = cameraObject.GetComponent<PostProcessVolume>();

        createdFramesPerClass = new Dictionary<string, int>();
        selectedObjects = new List<GameObject>();
        knownObjects = new List<GameObject>();

        knownObjects.Add(flareRed1);
        knownObjects.Add(flareRed2);
        knownObjects.Add(gate1);
        knownObjects.Add(gate2);
        knownObjects.Add(gate3);
        knownObjects.Add(gate4);
        knownObjects.Add(waterpool1);
        knownObjects.Add(obstacle1);
        knownObjects.Add(tripod1);
        knownObjects.Add(diver1);
        knownObjects.Add(flareYellow1);

        var waterContainerName = Settings.config.datasetOptions.selectedWaterContainer;

        foreach (string name in Settings.config.datasetOptions.selectedObjects)
            if (TryGetObjectByTypeName(name, out GameObject obj))
            {
                selectedObjects.Add(obj);
                if (!createdFramesPerClass.ContainsKey(obj.GetComponent<DatasetObjectInfo>().className)) createdFramesPerClass.Add(obj.GetComponent<DatasetObjectInfo>().className, 0);
            }
        createdFramesPerClass.Add("blank", 0);
        createdFramesPerClass.Add("multiple", 0);

        if (TryGetObjectByTypeName(waterContainerName, out GameObject waterContainerObject)) selectedWaterContainer = Instantiate(waterContainerObject, Vector3.zero, Quaternion.identity);

        Directory.CreateDirectory(Settings.config.datasetOptions.datasetDirPath);
        System.IO.File.Create(Settings.config.datasetOptions.datasetDirPath + @"\train.txt");

        generationState = GenerationState.SingleObjectsSetup;

        // classes
        
       
        

        foreach (var obj in selectedObjects)
        {
            var info = obj.GetComponent<DatasetObjectInfo>();
            if (IncludeInDataset(obj))
            {
                if (!objectsNumInClass.ContainsKey(info.className)) objectsNumInClass.Add(info.className, 1);
                else objectsNumInClass[obj.GetComponent<DatasetObjectInfo>().className]++;
            }
            else
            {
                if (!objectsNumInClass.ContainsKey("OBSTACLE")) objectsNumInClass.Add("OBSTACLE", 1);
                else objectsNumInClass["OBSTACLE"]++;
            }
        }


    }

    bool TryGetObjectByTypeName(string typeName, out GameObject obj)
    {
        obj = null;
        for (int i = 0; i < knownObjects.Count; i++)
            if (knownObjects[i].GetComponent<DatasetObjectInfo>().typeName == typeName)
            {
                obj = knownObjects[i];
                return true;
            }
        return false;
    }

    GenerationState generationState = GenerationState.Illegal;
    int createdTypeFrames = 0;
    int currentObjectIndex = 0;
    int debugWaitedFrames = 0;
    int debugNumFramesToWait = 0;

    List<GameObject> randomObjectsInScene = new List<GameObject>();

    void Update()
    {
        if (debugWaitedFrames++ == debugNumFramesToWait) debugWaitedFrames = 0;
        else return;

        if (Settings.config.datasetOptions.debugOptions.drawDetectionRect) cameraObject.GetComponent<DetectionRectDraw>().doDrawRect = true;

        switch (generationState)
        {
            case GenerationState.SingleObjectsSetup:
                {
                    ClearSceneFromObjects();
                    if (Settings.config.datasetOptions.classObstacleFrameNum == 0 && Settings.config.datasetOptions.classDetectedFrameNum == 0)
                    {
                        generationState = GenerationState.MultipleObjectsSetup;
                        return;
                    }
                    Instantiate(selectedObjects[currentObjectIndex]);
                    Debug.Log("Clearing scene for S.O. Creating object index: " + currentObjectIndex);
                    generationState = GenerationState.SingleObjects;

                    var info = selectedObjects[currentObjectIndex].GetComponent<DatasetObjectInfo>();
                    if (IncludeInDataset(selectedObjects[currentObjectIndex])) counter = Settings.config.datasetOptions.classDetectedFrameNum / objectsNumInClass[info.className];
                    else counter = Settings.config.datasetOptions.classObstacleFrameNum * Settings.config.datasetOptions.classNames.Count / objectsNumInClass["OBSTACLE"];
                    
                    for (int i = 0; i < selectedObjects.Count; i++)
                    {
                        string className = selectedObjects[i].GetComponent<DatasetObjectInfo>().className;
                        if(IncludeInDataset(selectedObjects[i]))Directory.CreateDirectory(Settings.config.datasetOptions.datasetDirPath + @"\" + className);
                        else Directory.CreateDirectory(Settings.config.datasetOptions.datasetDirPath + @"\" + "obstacle");
                    }

                    break;
                }
            case GenerationState.SingleObjects:
                {
                    GameObject singleObject = GameObject.FindGameObjectWithTag("ToDetect");
                    string className = singleObject.GetComponent<DatasetObjectInfo>().className;
                    if (!IncludeInDataset(singleObject)) className = "obstacle";


                    singleObject.transform.rotation = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 0f));
                    singleObject.transform.position = DatasetObjectInfo.GetRandomWorldPosistionInBoundary(singleObject, selectedWaterContainer);
                    if (Random.value < Settings.config.datasetOptions.percentClosest) cameraObject.transform.position = DatasetObjectInfo.GetRandomWorldPosistionInBoundaryClosest(cameraObject, selectedWaterContainer, singleObject);
                    else cameraObject.transform.position = DatasetObjectInfo.GetRandomWorldPosistionInBoundary(cameraObject, selectedWaterContainer);
                    LookAtTargetRandomly(singleObject, cameraObject, Settings.config.datasetOptions.cameraLookAtObjectPitchOffset, Settings.config.datasetOptions.cameraLookAtObjectYawOffset, Settings.config.datasetOptions.cameraLookAtObjectRollOffset);

                    var detector = new Detector();
                    detector.Detect(cameraObject.GetComponent<Camera>());
                    if (detector.detection[0].visible &&
                        detector.detection[0].fill >= Settings.config.datasetOptions.minObjectFill &&
                        (singleObject.transform.position - cameraObject.transform.position).magnitude >= Settings.config.datasetOptions.minCameraDistanceToObject &&
                        (singleObject.transform.position - cameraObject.transform.position).magnitude <= Settings.config.datasetOptions.maxCameraDistanceToObject &&
                        !DatasetObjectInfo.BoundaryIsColliding(cameraObject, singleObject))
                    {
                        string datasetRootPath = Settings.config.datasetOptions.datasetDirPath;

                        string txtFileName = datasetRootPath + @"/" + className + @"/" + createdFramesPerClass[className].ToString() + ".txt";
                        string pngFileName = datasetRootPath + @"/" + className + @"/" + createdFramesPerClass[className].ToString() + ".png";
                        string trainFileName = datasetRootPath + @"/train.txt";
                        if (IncludeInDataset(detector.detection[0].gameObject)) File.WriteAllText(txtFileName, detector.detection[0].GetTextInfo());
                        else File.WriteAllText(txtFileName, "");
                        File.AppendAllText(trainFileName, pngFileName + System.Environment.NewLine);
                        if (!Settings.config.datasetOptions.debugOptions.disableScreenshot)
                            ScreenCapture.CaptureScreenshot(System.IO.Directory.GetCurrentDirectory() + "/" + pngFileName);
                        createdFramesPerClass[className]++;
                       // Debug.Log("detected " + createdFramesPerClass[className] + "/" + Settings.config.datasetOptions.objectVisibleFrameNum * selectedObjects.Count);
                        Debug.Log("detected " + createdFramesPerClass[className] + "/" + 7 * selectedObjects.Count);
                        createdTypeFrames++;
                        counter--;
                        if (Settings.config.datasetOptions.debugOptions.logDetection) 
                            File.AppendAllText(datasetRootPath + @"/log.dat", detector.detection[0].distance + " " + detector.detection[0].fill + System.Environment.NewLine);
                    }

                    StartCoroutine(RandomizeGraphics(cameraObject.GetComponent<Camera>()));

                   


                    
                    if (counter <= 0)
                    {
                        Debug.Log("Finished object index:" + currentObjectIndex + "  " + (currentObjectIndex + 1) + "/" + selectedObjects.Count);
                        generationState = GenerationState.SingleObjectsSetup;
                        if (++currentObjectIndex >= selectedObjects.Count)
                        {
                            Debug.Log("Finished all single objects");
                            generationState = GenerationState.MultipleObjectsSetup;
                        }
                    }
                }
                break;
            case GenerationState.MultipleObjectsSetup:
                {
                    ClearSceneFromObjects();
                   // if (Settings.config.datasetOptions.objectMultipleFrameNum == 0)
                 /*   if (1 == 0)
                    {
                        generationState = GenerationState.NoObjectsSetup;
                        return; TODO
                    }*/
                    Debug.Log("Cleared scene for M.O.");
                    randomObjectsInScene = new List<GameObject>();
                    int TODOrandomObjectsNum = 20;
                    for (int i = 0; i < TODOrandomObjectsNum; i++)
                    {
                        var randomObject = Instantiate(selectedObjects[Random.Range(0, selectedObjects.Count)]);
                        randomObject.transform.rotation = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 0f));
                        randomObject.transform.position = DatasetObjectInfo.GetRandomWorldPosistionInBoundary(randomObject, selectedWaterContainer);
                        randomObjectsInScene.Add(randomObject);
                    }
                    counter = Settings.config.datasetOptions.classMultipleFrameNum * Settings.config.datasetOptions.classNames.Count;
                    if (!Directory.Exists(Settings.config.datasetOptions.datasetDirPath + @"/multiple")) Directory.CreateDirectory(Settings.config.datasetOptions.datasetDirPath + @"/multiple");
                    generationState = GenerationState.MultipleObjects;
                }
                break;
            case GenerationState.MultipleObjects:
                {
                  if (counter <= 0)
                    {
                        generationState = GenerationState.NoObjectsSetup;
                        return;
                    }
                    //
                    int detectedVisibleObjectsNum = 0;
                    foreach (var obj in randomObjectsInScene)
                    {
                        obj.SetActive(Random.value > 0.5f);
                        if (!obj.activeSelf) continue;
                        obj.transform.rotation = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 0f));
                        obj.transform.position = DatasetObjectInfo.GetRandomWorldPosistionInBoundary(obj, selectedWaterContainer);
                    }

                    cameraObject.transform.position = DatasetObjectInfo.GetRandomWorldPosistionInBoundary(cameraObject, selectedWaterContainer);
                    LookRandomly(cameraObject, 20f, 180f, 30f);
                    StartCoroutine(RandomizeGraphics(cameraObject.GetComponent<Camera>()));
                    var detector = new Detector();

                    string detectedObjectsText = "";
                    foreach (var info in detector.Detect(cameraObject.GetComponent<Camera>()))
                    {
                        if (!info.visible ||
                            info.fill < Settings.config.datasetOptions.minObjectFill ||
                            DatasetObjectInfo.BoundaryIsColliding(cameraObject, info.gameObject)) continue;//TODO sprawdz nową kolizję

                        if(IncludeInDataset(info.gameObject)) detectedObjectsText += info.GetTextInfo() + System.Environment.NewLine;
                        detectedVisibleObjectsNum++;
                    }

                    if(detectedVisibleObjectsNum >= Settings.config.datasetOptions.minVisibleMultipleObjectsNum)
                    {
                        string datasetRootPath = Settings.config.datasetOptions.datasetDirPath;
                        string txtFileName = datasetRootPath + @"/" + "multiple" + @"/" + createdFramesPerClass["multiple"].ToString() + ".txt";
                        string pngFileName = datasetRootPath + @"/" + "multiple" + @"/" + createdFramesPerClass["multiple"].ToString() + ".png";
                        string trainFileName = datasetRootPath + @"/train.txt";
                        File.WriteAllText(txtFileName, detectedObjectsText);
                        File.AppendAllText(trainFileName, pngFileName + System.Environment.NewLine);
                        ScreenCapture.CaptureScreenshot(System.IO.Directory.GetCurrentDirectory() + "/" + pngFileName);
                        createdFramesPerClass["multiple"]++;
                        counter--;
                    }
                    break;
                }
            case GenerationState.NoObjectsSetup:
                {
                    ClearSceneFromObjects();
                    if (Settings.config.datasetOptions.classBlankFrameNum == 0)
                    {
                        generationState = GenerationState.Finished;
                        return;
                    }
                    counter = Settings.config.datasetOptions.classBlankFrameNum * Settings.config.datasetOptions.classNames.Count;
                    generationState = GenerationState.NoObjects;
                    if (!Directory.Exists(Settings.config.datasetOptions.datasetDirPath + @"/blank")) Directory.CreateDirectory(Settings.config.datasetOptions.datasetDirPath + @"/blank");
                    break;
                }
            case GenerationState.NoObjects:
                {
                    StartCoroutine(RandomizeGraphics(cameraObject.GetComponent<Camera>()));
                    cameraObject.transform.position = DatasetObjectInfo.GetRandomWorldPosistionInBoundary(cameraObject, selectedWaterContainer);
                    LookRandomly(cameraObject, Settings.config.datasetOptions.cameraLookRandomlyPitchOffset, Random.Range(0f, 360f), Settings.config.datasetOptions.cameraLookRandomlyRollOffset);

                    string datasetRootPath = Settings.config.datasetOptions.datasetDirPath;
                    string txtFileName = datasetRootPath + @"/" + "blank" + @"/" + createdFramesPerClass["blank"].ToString() + ".txt";
                    string pngFileName = datasetRootPath + @"/" + "blank" + @"/" + createdFramesPerClass["blank"].ToString() + ".png";
                    string trainFileName = datasetRootPath + @"/train.txt";
                    File.WriteAllText(txtFileName, "");
                    File.AppendAllText(trainFileName, pngFileName + System.Environment.NewLine);
                    ScreenCapture.CaptureScreenshot(System.IO.Directory.GetCurrentDirectory() + "/" + pngFileName);
                    createdFramesPerClass["blank"]++;
                }
               // if (createdFramesPerClass["blank"] >= Settings.config.datasetOptions.objectBlankFrameNum)
                if (--counter <= 0)
                {
                    Debug.Log("Finished all no objects frames");
                    generationState = GenerationState.Finished;
                }
                break;
            case GenerationState.Finished:
                {
                    ClearSceneFromObjects();
                    Destroy(selectedWaterContainer);
                    #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
                    #else
                        Application.Quit();
                    #endif
                    break;
                }
            default:

                break;
        }
    }
    
    public bool IncludeInDataset(GameObject obj)
    {
        var info = obj.GetComponent<DatasetObjectInfo>();
        foreach (var className in Settings.config.datasetOptions.classNames)
            if (info.className == className) return true;
        return false;
    }

    public void ClearSceneFromObjects()
    {
        GameObject[] objectsInScene = GameObject.FindGameObjectsWithTag("ToDetect");
        for(int i = objectsInScene.Length-1; i >= 0; i--)
            Destroy(objectsInScene[i]);
    }

    public void LookAtTargetRandomly(GameObject target, GameObject looker, float pitchOffset, float yawOffset, float rollOffset)
    {
        looker.transform.LookAt(target.transform);
        Vector3 rot = looker.transform.rotation.eulerAngles;
        rot.x += Random.Range(-pitchOffset, pitchOffset);
        rot.y += Random.Range(-yawOffset, yawOffset);
        rot.z += Random.Range(-rollOffset, rollOffset);

        looker.transform.rotation = Quaternion.Euler(rot);
    }

    public IEnumerator RandomizeGraphics(Camera camera)
    {
        yield return new WaitForEndOfFrame();
        camera.focalLength = Settings.config.datasetOptions.graphicsOptions.cameraFocalLength;
        camera.sensorSize = new Vector2(Settings.config.datasetOptions.graphicsOptions.cameraSensorSize[0], Settings.config.datasetOptions.graphicsOptions.cameraSensorSize[1]);

        if (Settings.config.datasetOptions.graphicsOptions.randomizeLightIntesity)
        {
            light1.intensity = Random.Range(0.1f, 1.1f);
            light2.intensity = Random.Range(0.1f, 1.1f);
        }
        else
        {
            light1.intensity = Settings.config.datasetOptions.graphicsOptions.lightIntesity;
            light2.intensity = Settings.config.datasetOptions.graphicsOptions.lightIntesity;
        }

        postDebug.enabled = Settings.config.datasetOptions.debugOptions.drawPostProcessDebug;
        postVolume.profile.GetSetting<Bloom>().intensity.value = Settings.config.datasetOptions.graphicsOptions.bloomIntensity;
        postVolume.profile.GetSetting<AmbientOcclusion>().intensity.value = Settings.config.datasetOptions.graphicsOptions.ambientOcclusionIntensity;
        postVolume.profile.GetSetting<ColorGrading>().mixerRedOutRedIn.value = Settings.config.datasetOptions.graphicsOptions.mixerRedOutRedIn;
        postVolume.profile.GetSetting<ColorGrading>().mixerGreenOutGreenIn.value = Settings.config.datasetOptions.graphicsOptions.mixerGreenOutGreenIn;
        postVolume.profile.GetSetting<ColorGrading>().mixerBlueOutBlueIn.value = Settings.config.datasetOptions.graphicsOptions.mixerBlueOutBlueIn;
        postVolume.profile.GetSetting<Grain>().intensity.value = Settings.config.datasetOptions.graphicsOptions.grainIntesity;

        RenderSettings.ambientLight = new Color(Settings.config.datasetOptions.graphicsOptions.ambientLightColor[0], Settings.config.datasetOptions.graphicsOptions.ambientLightColor[1], Settings.config.datasetOptions.graphicsOptions.ambientLightColor[2], 1f);
        if (Settings.config.datasetOptions.graphicsOptions.randomizeLightIntesity) RenderSettings.ambientIntensity = Random.Range(-0.2f, 0.2f);
        else RenderSettings.ambientIntensity = Settings.config.datasetOptions.graphicsOptions.ambientLightIntesity;

        camera.backgroundColor = new Color(Settings.config.datasetOptions.graphicsOptions.cameraBackgroundColor[0], Settings.config.datasetOptions.graphicsOptions.cameraBackgroundColor[1], Settings.config.datasetOptions.graphicsOptions.cameraBackgroundColor[2], 1f);

        RenderSettings.fog = Settings.config.datasetOptions.graphicsOptions.fogEnabled;
        if (RenderSettings.fog)
        {
            if (Settings.config.datasetOptions.graphicsOptions.randomizeFogDensity) RenderSettings.fogDensity = Random.Range(.01f, .5f);
            else RenderSettings.fogDensity = Settings.config.datasetOptions.graphicsOptions.fogDensity;
            if (!Settings.config.datasetOptions.graphicsOptions.fogColorRandomized)
            {
                RenderSettings.fogColor = new Color(Settings.config.datasetOptions.graphicsOptions.fogColor[0], Settings.config.datasetOptions.graphicsOptions.fogColor[1], Settings.config.datasetOptions.graphicsOptions.fogColor[2], 1f);
            }
            else
            {
                Color back = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);
                RenderSettings.fogColor = back;
                camera.backgroundColor = back;
            }
        }
        yield return null;
    }

    public void LookRandomly(GameObject looker, float pitchOffset, float yawOffset, float rollOffset) => looker.transform.rotation = Quaternion.Euler(new Vector3(Random.Range(-pitchOffset, pitchOffset), Random.Range(-yawOffset, yawOffset), Random.Range(-rollOffset, rollOffset)));
}
