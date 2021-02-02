using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Detector
{
    public List<VisibilityInfo> detection;

    public List<VisibilityInfo> Detect(Camera camera)
    {
        detection = new List<VisibilityInfo>();
        var objectsInSceneArray = GameObject.FindGameObjectsWithTag("ToDetect");
        for (int i = 0; i < objectsInSceneArray.Length; i++)
            detection.Add(objectsInSceneArray[i].GetComponent<DatasetObjectInfo>().CheckVisibility(camera));
        return detection;
    }
}
