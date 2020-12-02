using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomizeTransform : MonoBehaviour
{
    public bool randomizePosition;
    public Vector3 positionOffset;
    public Vector3 positionSpeed;

    public bool randomizeRotation;
    public Vector3 rotationOffset;
    public Vector3 rotationSpeed;

    public bool randomizeScale;
    public Vector3 scaleOffset;
    public Vector3 scaleSpeed;

    Vector3 orginalPosition;
    Vector3 orginalRotation;
    Vector3 orginalScale;

    void Start()
    {
        orginalPosition = transform.localPosition;
        orginalRotation = transform.localRotation.eulerAngles;
        orginalScale = transform.localScale;
    }

    void Update()
    {
        if (randomizePosition)
        {
            Vector3 posOffset = new Vector3(Mathf.Sin(Time.time * positionSpeed.x), Mathf.Sin(Time.time * positionSpeed.y), Mathf.Sin(Time.time * positionSpeed.z));
            posOffset.Scale(positionOffset);
            transform.localPosition = orginalPosition + posOffset;
        }
        else
        {
            transform.localPosition = orginalPosition;
        }
        if (randomizeRotation)
        {
            Vector3 rotOffset = new Vector3(Mathf.Sin(Time.time * rotationSpeed.x), Mathf.Sin(Time.time * rotationSpeed.y), Mathf.Sin(Time.time * rotationSpeed.z));
            rotOffset.Scale(rotationOffset);
            transform.localRotation= Quaternion.Euler( orginalRotation + rotOffset);
        }
        else
        {
            transform.localPosition = orginalPosition;
        }
    }
}
