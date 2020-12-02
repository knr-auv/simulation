using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.AccessControl;
using Unity.Profiling;
using UnityEngine;

public class RobotController : MonoBehaviour
{
    [SerializeField] public Camera colorCamera;
    [SerializeField] public Camera depthCamera;
    [SerializeField] public Vector3 checkerPosition;
    [SerializeField] public Vector3 checkerRotation;
    public Rigidbody rb;

    public Motor motorFL, motorFR, motorML, motorMR, motorB;
    public Accelerometer accelerometer;
    public Gyroscope gyroscpe;
    public Barometer barometer;

    public AllSensors allSensors;
    public Orientation orientation;

    public ConcurrentQueue<Action> operations;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        operations = new ConcurrentQueue<Action>();

        orientation = new Orientation(this);
        accelerometer = new Accelerometer(this);
        gyroscpe = new Gyroscope(this);
        barometer = new Barometer(this);
        allSensors = new AllSensors(this);
        motorFL = new Motor(this) { position = new Vector3(-1f, 0f, 1), rotation = Quaternion.Euler(new Vector3(-90f,0f,0f)) };
        motorFR = new Motor(this) { position = new Vector3(1f, 0f, 1), rotation = Quaternion.Euler(new Vector3(-90f,0f,0f)) };
        motorML = new Motor(this) { position = new Vector3(-1f, 0f, .5f), rotation = Quaternion.Euler(new Vector3(0f,0f,0f)) };
        motorMR = new Motor(this) { position = new Vector3(1f, 0f, .5f), rotation = Quaternion.Euler(new Vector3(0f,0f,0f)) };
        motorB = new Motor(this)  { position = new Vector3(0f, 0f, -1f), rotation = Quaternion.Euler(new Vector3(-90f,0f,0f)) };
    }

    void FixedUpdate()
    {
        while (operations.Count > 0)
        {
            operations.TryDequeue(out Action action);
            action.Invoke();
        }
        Module.FixedUpdateAll();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.TransformPoint(checkerPosition), 0.01f);
        Gizmos.DrawSphere(transform.TransformPoint(checkerPosition + Quaternion.Euler(checkerRotation) * Vector3.forward * 0.02f), 0.005f);
        Gizmos.DrawSphere(transform.TransformPoint(checkerPosition + Quaternion.Euler(checkerRotation) * Vector3.forward * 0.03f), 0.004f);
        Module.DrawGizmosAll();
    } 
}

abstract public class Module
{
    public static readonly List<Module> all = new List<Module>();
    public readonly string name;
    public Vector3 position;
    public Quaternion rotation;
    public static RobotController rc;
    abstract public void FixedUpdate();
    virtual public void DrawGizmos()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(rc.transform.TransformPoint(position), rc.transform.TransformPoint(position + rotation * Vector3.forward * 0.25f));
        Gizmos.DrawCube(rc.transform.TransformPoint(position), Vector3.one * 0.05f);
    }
    public Module(RobotController _rc)
    {
        all.Add(this);
        rc = _rc;
    }
    
    public Module()
    {
        all.Add(this);
    }

    public static void FixedUpdateAll()
    {
        for (int i = 0; i < all.Count; i++) all[i].FixedUpdate();
    }

    public static void DrawGizmosAll()
    {
        Gizmos.color = Color.blue;
        for (int i = 0; i < all.Count; i++)
            all[i].DrawGizmos();
    }
}

public class Motor : Module
{
    public float fill;
    readonly float a = 1.66352902e-8f, b = -0.00003994119f, c = 0.00752234546f, d = 22.4126993334f;
    float Force()
    {
        if (rc.transform.TransformPoint(position).y >= 0) return 0f;
        float x = Map(fill, -1f, 1f, 1100f, 1900f);
        if (fill > 0) return Mathf.Max(0f, a * x * x * x + b * x * x + c * x + d);
        else return -Mathf.Max(0f, a * x * x * x + b * x * x + c * x + d);
    }

    public override void FixedUpdate()
    {
        if(fill != 0f && rc.transform.TransformPoint(position).y < 0)
            rc.rb.AddForceAtPosition(rc.transform.TransformDirection(rotation * Vector3.forward * Force()), rc.transform.TransformPoint(position), ForceMode.Force);
    }

    override public void DrawGizmos()
    {
        Gizmos.color = Color.blue;
        if (Force() > 0) Gizmos.color = Color.red;
        if (Force() < 0) Gizmos.color = Color.green;
        if (rc.transform.TransformPoint(position).y >= 0) Gizmos.color = Color.gray;
        Gizmos.DrawLine(rc.transform.TransformPoint(position), rc.transform.TransformPoint(position - rotation * Vector3.forward * Force()/20f));
        Gizmos.DrawCube(rc.transform.TransformPoint(position), Vector3.one * 0.05f);
    }

    float Map(float x, float a, float b, float c, float d) => c + (x - a) * (d - c) / (b - a);
    public Motor(RobotController _rc) : base(_rc) { }
}

public class Accelerometer : Module
{
    public JSON.Accel accel;
    Vector3 acceleration = new Vector3(), lastVelocity = new Vector3();

    public Accelerometer(RobotController _rc) : base(_rc) {
        accel = new JSON.Accel();
    }

    public override void FixedUpdate()
    {
        acceleration = (rc.rb.velocity - lastVelocity) / Time.fixedDeltaTime;
        lastVelocity = rc.rb.velocity;
        accel.x = acceleration.x;
        accel.y = acceleration.y;
        accel.z = acceleration.z;
    }
}

public class Gyroscope : Module
{
    public JSON.Gyro gyro;
    public JSON.Rot_speed rotSpeed;
    public JSON.Angular_accel angularAccel;
    public Vector3 angular_acceleration = new Vector3(), lastAngularVelocity = new Vector3();

    public Gyroscope(RobotController _rc) : base(_rc) {
        gyro = new JSON.Gyro();
        rotSpeed = new JSON.Rot_speed();
        angularAccel = new JSON.Angular_accel();
    }
    public override void FixedUpdate()
    {
        gyro.x = rc.orientation.jsonOrientation.rot.x;
        gyro.y = rc.orientation.jsonOrientation.rot.y;
        gyro.z = rc.orientation.jsonOrientation.rot.z;

        rotSpeed.x = rc.rb.angularVelocity.x;
        rotSpeed.y = rc.rb.angularVelocity.y;
        rotSpeed.z = rc.rb.angularVelocity.z;

        angular_acceleration = (rc.rb.angularVelocity - lastAngularVelocity) / Time.fixedDeltaTime;
        lastAngularVelocity = rc.rb.angularVelocity;
        angularAccel.x = angular_acceleration.x;
        angularAccel.y = angular_acceleration.y;
        angularAccel.z = angular_acceleration.z;
    }
}

public class Barometer : Module
{
    public JSON.Baro baro;
    public Barometer(RobotController _rc) : base(_rc) {
        baro = new JSON.Baro();
    }
    public override void FixedUpdate()
    {
        baro.pressure = -(rc.transform.TransformPoint(position)).y * 9800f;
    }
}

public class AllSensors : Module
{
    public JSON.Sensors sensors;
    public AllSensors(RobotController _rc) : base(_rc) {
        sensors = new JSON.Sensors();
        sensors.baro = rc.barometer.baro;
        sensors.gyro = rc.gyroscpe.gyro;
        sensors.accel = rc.accelerometer.accel;
        sensors.angular_accel = rc.gyroscpe.angularAccel;
        sensors.rot_speed = rc.gyroscpe.rotSpeed;
    }

    public JSON.Sensors Get() => sensors;

    public override void FixedUpdate() { }
}

public class Orientation : Module
{
    public JSON.Orientation jsonOrientation;
    public Orientation(RobotController _rc) : base(_rc) {
        jsonOrientation = new JSON.Orientation();
    }

    public void Set(JSON.Orientation newOrientation)
    {
        rc.operations.Enqueue(() =>
        {   
            rc.transform.position = new Vector3(newOrientation.pos.x, newOrientation.pos.y, newOrientation.pos.z);
            rc.transform.rotation = Quaternion.Euler(newOrientation.rot.x, newOrientation.rot.y, newOrientation.rot.z);
            rc.rb.velocity = Vector3.zero;
            rc.rb.angularVelocity = Vector3.zero;
        });
    }

    public JSON.Orientation Get() => jsonOrientation;

    public override void FixedUpdate()
    {
        jsonOrientation.pos.x = rc.transform.position.x;
        jsonOrientation.pos.y = rc.transform.position.y;
        jsonOrientation.pos.z = rc.transform.position.z;
        jsonOrientation.rot.x = rc.transform.rotation.eulerAngles.x;
        jsonOrientation.rot.y = rc.transform.rotation.eulerAngles.y;
        jsonOrientation.rot.z = rc.transform.rotation.eulerAngles.z;
    }
}