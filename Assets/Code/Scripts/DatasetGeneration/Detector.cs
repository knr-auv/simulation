using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Detector
{
    public List<VisibilityInfo> detection;

    public List<VisibilityInfo> Detect(Camera camera)
    {
        if(detection != null) detection.Clear();
        else detection = new List<VisibilityInfo>(); //TODO ArrayPool<VisibilityInfo>.Shared.Rent(objectsInSceneArray.Length)?
        var objectsInSceneArray = GameObject.FindGameObjectsWithTag("ToDetect");
        for (int i = 0; i < objectsInSceneArray.Length; i++)
            detection.Add(objectsInSceneArray[i].GetComponent<DatasetObjectInfo>().CheckVisibility(camera));
        return detection;
    }
}
