using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinishZoneController : MonoBehaviour
{
    [SerializeField]
    public string id;
    
    void OnTriggerEnter(Collider collider)
    {
        if (collider.tag != "Robot") return;
        Debug.Log("finished in " + id + " FinishZone");
        GameObject.FindGameObjectWithTag("SimulationController").GetComponent<SimulationController>().PlaceRobotInStartZone();
        GameObject.FindGameObjectWithTag("SimulationController").GetComponent<SimulationController>().SendToClients(WAPIClient.PacketType.HIT_FZ, WAPIClient.Flag.None, "{\"id\":\"" + id + "\"}");
    }
    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(GetComponent<Collider>().bounds.center, GetComponent<Collider>().bounds.size);
    }
}
