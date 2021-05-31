using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionpointController : MonoBehaviour
{
    [SerializeField]
    public string id = "AP";

    [SerializeField]
    public bool active = false;
    
    void OnTriggerStay(Collider other)
    {
        if (other.tag != "Robot") return;
        if(!active)Debug.Log("activepoint " + id + " active");
        active = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag != "Robot") return;
        active = false;
        if(!active)Debug.Log("activepoint " + id + " inactive");
    }

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(GetComponent<Collider>().bounds.center, GetComponent<Collider>().bounds.size);
    }
}
