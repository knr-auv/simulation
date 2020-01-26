using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static JSON;

public class OkonController : MonoBehaviour
{
    private Rigidbody rb;
    public GameObject motorFL, motorFR, motorML, motorMR, motorB;//ForwardLefy MiddleLeft
    public MotorController FL, FR, ML, MR, B;

    public Vector3 mFlpos, mFRpos, mMLpos, mMRpos, mBpos, mFldir, mFRdir, mMLdir, mMRdir, mBdir;
    public Orientation orientation = new Orientation();
    public Sensors sensors = new Sensors();
    public Vector3 acceleration = new Vector3(), lastVelocity = new Vector3();
    void Start()
    {
        ML = motorML.GetComponent<MotorController>();
        MR = motorMR.GetComponent<MotorController>();
        FL = motorFL.GetComponent<MotorController>();
        FR = motorFR.GetComponent<MotorController>();
        B = motorB.GetComponent<MotorController>();
        rb = GetComponent<Rigidbody>();

    }
    
    void FixedUpdate()
    {
        orientation.pos.x = transform.position.x;
        orientation.pos.y = transform.position.y;
        orientation.pos.z = transform.position.z;
        orientation.rot.x = transform.rotation.eulerAngles.x;
        orientation.rot.y = transform.rotation.eulerAngles.y;
        orientation.rot.z = transform.rotation.eulerAngles.z;

        acceleration = (rb.velocity - lastVelocity) / Time.fixedDeltaTime;//TODO correct XYZ axis order
        lastVelocity = rb.velocity;
        sensors.accel.x = acceleration.x;
        sensors.accel.y = acceleration.y;
        sensors.accel.z = acceleration.z;

        sensors.baro.pressure = -transform.position.y*1000f*9.8f;

        sensors.gyro.x = orientation.rot.x;
        sensors.gyro.y = orientation.rot.y;
        sensors.gyro.z = orientation.rot.z;
        /*return;;
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
        #endregion*/
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(mFlpos, 0.04f);       
    }

    public Sensors GetSensors()
    {
        return sensors;
    }

    public Orientation GetOrientation()
    {
        return orientation;
    }
}
