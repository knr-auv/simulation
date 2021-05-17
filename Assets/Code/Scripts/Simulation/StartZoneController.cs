using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartZoneController : MonoBehaviour
{
    [SerializeField]
    public string id;

    [SerializeField]
    public float angle;

    [SerializeField]
    public float fov;

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.1f);
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(Mathf.Sin(Mathf.Deg2Rad * angle), 0, Mathf.Cos(Mathf.Deg2Rad * angle)) / 2);
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(Mathf.Sin(Mathf.Deg2Rad * (angle + fov / 2)), 0, Mathf.Cos(Mathf.Deg2Rad * (angle + fov / 2))));
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(Mathf.Sin(Mathf.Deg2Rad * (angle - fov / 2)), 0, Mathf.Cos(Mathf.Deg2Rad * (angle - fov / 2))));
    }
}
