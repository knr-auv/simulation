using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointController : MonoBehaviour
{
    [SerializeField]
    public string id;

    [SerializeField]
    public bool reached;


    void Start() => reached = false;

    void OnTriggerEnter(Collider collider)
    {
        if (collider.tag != "Robot") return;
        if (reached) return;
        reached = true;
        Debug.Log("checkpoint " + id + " reached");
    }
    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(GetComponent<Collider>().bounds.center, GetComponent<Collider>().bounds.size);
    }
}
