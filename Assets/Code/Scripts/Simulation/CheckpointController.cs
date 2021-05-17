using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointController : MonoBehaviour
{
    [SerializeField]
    public string id;

    [SerializeField]
    public bool reached;


    void Start()
    {
        reached = false;
    }

    void OnTriggerEnter(Collider collider)//TODO check if its AUV
    {
        if (reached) return;
        reached = true;
        Debug.Log("checkpoint " + id + " reached");
    }
}
