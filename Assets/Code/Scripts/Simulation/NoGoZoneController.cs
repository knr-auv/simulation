using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoGoZoneController : MonoBehaviour
{
    [SerializeField]
    public string id;

    void OnTriggerEnter(Collider collider)
    {
        if (collider.tag != "Robot") return;
        Debug.Log("entered " + id + "NoGoZone");
        GameObject.FindGameObjectWithTag("SimulationController").GetComponent<SimulationController>().PlaceRobotInStartZone();
        GameObject.FindGameObjectWithTag("SimulationController").GetComponent<SimulationController>().SendToClients(WAPIClient.PacketType.HIT_NGZ, WAPIClient.Flag.None, id);
    }
    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(GetComponent<Collider>().bounds.center, GetComponent<Collider>().bounds.size);
    }
}
