using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoGoZoneController : MonoBehaviour
{
    [SerializeField]
    public string id;

    void OnTriggerEnter(Collider collider)//TODO check if its AUV
    {
        Debug.Log("entered " + id + "NoGoZone");
    }
}
