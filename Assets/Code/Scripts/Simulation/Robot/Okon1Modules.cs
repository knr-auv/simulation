using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Okon1Modules : MonoBehaviour
{
    bool done = false;
    public void Update()
    {
        if (!done)
        {
            RobotController rc = GetComponent<RobotController>();
          /*  rc.motorFL.position = new Vector3(.28f, 0.025f, .111f);
            rc.motorFR.position = new Vector3(-.28f, 0.025f, .111f);
            rc.motorML.position = new Vector3(-0.1758f, 0.0551f, -0.0341f);
            rc.motorMR.position = new Vector3(0.1758f, 0.0551f, -0.0341f);
            rc.motorB.position = new Vector3(0f, 0.025f, -.355f);*/
            rc.colorCamera.transform.localPosition= new Vector3(0f, 0.032f, 0.365f);
            rc.depthCamera.transform.localPosition = new Vector3(0f, 0.032f, 0.365f);
            done = true;
        }
    }
}
