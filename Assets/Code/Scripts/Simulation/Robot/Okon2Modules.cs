using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Okon2Modules : MonoBehaviour
{
    bool done = false;
    public void Update()
    {
        if (!done)
        {
            RobotController rc = GetComponent<RobotController>();
          /*  rc.motorFL.position = new Vector3(-0.1639f, -0.0559f, 0.0969f);
            rc.motorFR.position = new Vector3(0.1639f, -0.0559f, 0.0969f);
            rc.motorML.position = new Vector3(-0.1938f, 0.0594f, 0.008f);
            rc.motorMR.position = new Vector3(0.1938f, 0.0594f, 0.008f);
            rc.motorB.position = new Vector3(0f, 0.025f, -.39f);*/
          //  rc.colorCamera.transform.localPosition = new Vector3(0f, 0.029f, 0.248f);
          //  rc.depthCamera.transform.localPosition = new Vector3(0f, 0.029f, 0.248f);
            done = true;
        }
    }
}
