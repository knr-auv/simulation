using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CCTVController : MonoBehaviour
{
    public GameObject target;
    public Camera cam;
    public float h;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(target.transform.position);
        cam.fieldOfView = Mathf.Atan((h*0.5f)/(target.transform.position - transform.position).magnitude)*180f/Mathf.PI;
    }
}
