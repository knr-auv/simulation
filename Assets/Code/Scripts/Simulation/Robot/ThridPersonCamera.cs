using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThridPersonCamera : MonoBehaviour
{
    void Update()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.parent.TransformPoint(Vector3.zero), transform.TransformDirection(Vector3.back), out hit, .5f))
        {
            Debug.DrawRay(transform.parent.TransformPoint(Vector3.zero), transform.TransformDirection(Vector3.back) * hit.distance, Color.red);
            transform.localPosition = Vector3.back * hit.distance; 
        }
        else
        {
            Debug.DrawRay(transform.parent.TransformPoint(Vector3.zero), transform.TransformDirection(Vector3.back *.5f), Color.yellow);
            transform.localPosition = Vector3.back * .5f; 
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawCube(transform.parent.TransformPoint(Vector3.zero), Vector3.one * .01f);
    }
}
