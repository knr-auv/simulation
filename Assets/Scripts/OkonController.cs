using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static JSON;
/// <summary>
/// Fwyporu
/// opory T1 T2
/// opory rot
/// </summary>
public class OkonController : MonoBehaviour
{
    private Rigidbody _rb;
    public GameObject motorFL, motorFR, motorML, motorMR, motorB;//ForwardLefy MiddleLeft
    public MotorController FL, FR, ML, MR, B;

    UInt16 frame = 0;

    public Vector3 mFlpos, mFRpos, mMLpos, mMRpos, mBpos, mFldir, mFRdir, mMLdir, mMRdir, mBdir;

    void Start()
    {
        ML = motorML.GetComponent<MotorController>();
        MR = motorMR.GetComponent<MotorController>();
        FL = motorFL.GetComponent<MotorController>();
        FR = motorFR.GetComponent<MotorController>();
        B = motorB.GetComponent<MotorController>();
    }

    void FixedUpdate()
    {
        return;
        float verticalInput = Input.GetAxis("Vertical");
        float horizontalInput = Input.GetAxis("Horizontal");

        ML.fill = Math.Max(Math.Min(verticalInput + horizontalInput, 1f), -1f)*0.4f;
        MR.fill = Math.Max(Math.Min(verticalInput - horizontalInput, 1f), -1f)*0.4f;

        #region moc w pionie
        float poV = 0.2f;
        if (Input.GetKey(KeyCode.Space))
        {
            FL.fill = poV;
            FR.fill = poV;
            B.fill = poV;
        }else if (Input.GetKey(KeyCode.LeftShift))
        {
            FL.fill = -poV;
            FR.fill = -poV;
            B.fill = -poV;
        }
        else
        {
            FL.fill = 0f;
            FR.fill = 0f;
            B.fill = 0f;
        }
        #endregion
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(mFlpos, 0.04f);       
    }

    public Sensors GetSensors()
    {
        Sensors s = new Sensors();
        return s;
    }

    public Orientation GetOrientation()
    {
        Orientation o = new Orientation();
        o.rot.x = transform.rotation.ToEulerAngles().x;
        o.rot.y = transform.rotation.ToEulerAngles().y;
        o.rot.z = transform.rotation.ToEulerAngles().z;
        return o;
    }
}
