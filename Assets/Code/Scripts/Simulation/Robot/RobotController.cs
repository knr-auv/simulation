using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.AccessControl;
using Unity.Profiling;
using UnityEngine;
using Utf8Json;

public class RobotController : MonoBehaviour
{
    [SerializeField] public Camera colorCamera;
    [SerializeField] public Camera depthCamera;
    [SerializeField] public Vector3 checkerPosition;
    [SerializeField] public Vector3 checkerRotation;
    public Rigidbody rb;

    public Motor motorFLH, motorFLV, motorBLV, motorBLH, motorFRH, motorFRV, motorBRV, motorBRH;
    public Accelerometer accelerometer;
    public Gyroscope gyroscpe;
    public Barometer barometer;

    public AllSensors allSensors;
    public Orientation orientation;

    public ConcurrentQueue<Action> operations;
    
    public bool motorsArmed = true;
    public string motorsControlMode = "stable";
    public PID pitchPID, rollPID, yawPID, depthPID;
    public Vector3 targetRotationSpeed = Vector3.zero;
    public Vector3 targetRotation = new Vector3();
    public float targetDepth = 1;
    public Vector3 velocity = new Vector3();
    public PID rollSpeedPID, pitchSpeedPID, yawSpeedPID;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        operations = new ConcurrentQueue<Action>();

        orientation = new Orientation(this);
        accelerometer = new Accelerometer(this);
        gyroscpe = new Gyroscope(this);
        barometer = new Barometer(this);
        allSensors = new AllSensors(this);
        motorFLH = new Motor(this) { position = new Vector3(-0.193f, 0.0937f, 0.2389f), rotation = Quaternion.Euler(new Vector3(0f,30f,0f)), isClockwise = false};
        motorBLH = new Motor(this) { position = new Vector3(-0.193f, 0.0937f, -0.2151f), rotation = Quaternion.Euler(new Vector3(0f, 150f, 0f)), isClockwise = false };
        motorFRH = new Motor(this) { position = new Vector3(0.193f, 0.0937f, 0.2389f), rotation = Quaternion.Euler(new Vector3(0f, -30f, 0f)), isClockwise = true };
        motorBRH = new Motor(this) { position = new Vector3(0.193f, 0.0937f, -0.2151f), rotation = Quaternion.Euler(new Vector3(0f, -150f, 0f)), isClockwise = true };

        motorFLV = new Motor(this) { position = new Vector3(-0.1709f, 0f, 0.1195f), rotation = Quaternion.Euler(new Vector3(-90f, 0f, 0f)), isClockwise = true };
        motorBLV = new Motor(this) { position = new Vector3(-0.1709f, 0f, -0.0963f), rotation = Quaternion.Euler(new Vector3(-90f, 0f, 0f)), isClockwise = true };
        motorFRV = new Motor(this) { position = new Vector3(0.1709f, 0f, 0.1195f), rotation = Quaternion.Euler(new Vector3(-90f, 0f, 0f)), isClockwise = false };
        motorBRV = new Motor(this) { position = new Vector3(0.1709f, 0f, -0.0963f), rotation = Quaternion.Euler(new Vector3(-90f, 0f, 0f)), isClockwise = false };
       
        rollPID = new PID(.4f, .0f, 0.03f, 1f, 1, .5f);
        pitchPID = new PID(.7f, .0f, 0.1f, 1f, 1, .5f);
        yawPID = new PID(.3f, .0f, 0.04f, 1f, 1, .5f);
        depthPID = new PID(10f, .0f, 4f, 1f, 1, .5f);
        
        rollSpeedPID = new PID(1f, 0.1f, 0.05f, 1f, 1, .5f);
        pitchSpeedPID = new PID(1f, 0.1f, 0.05f, 1f, 1, .5f);
        yawSpeedPID = new PID(1f, 0.1f, 0.05f, 1f, 1, .5f);
        JsonSerializer.ToJsonString<JSON.PIDs>(new JSON.PIDs()); //No idea why first first call takes 1 second, leave it for performance
    }

    void FixedUpdate()
    {
        while (operations.Count > 0)
        {
            operations.TryDequeue(out Action action);
            action.Invoke();
        }

        if (motorsArmed)
        {
            if (motorsControlMode == "stable")
            {
                /*
                 * public static Quaternion ShortestRotation(Quaternion a, Quaternion b)
                    {
	                    if (Quaternion.Dot(a, b) < 0)
	                    {
		                    return a * Quaternion.Inverse(Multiply(b, -1));
	                    }
	                    else return a * Quaternion.Inverse(b);
                    }

                    public static Quaternion Multiply(Quaternion input, float scalar)
                    {
	                    return new Quaternion(input.x * scalar, input.y * scalar, input.z * scalar, input.w * scalar);
                    }
                 */
                float t = 50;
                var target = Quaternion.Euler(targetRotation);
                if (Quaternion.Dot(gameObject.transform.rotation , target) < 0)
                {
                    target.w *= -1;
                    target.x *= -1;
                    target.y *= -1;
                    target.z *= -1;
                }
                target.x *= -1;
                target.y *= -1;
                target.z *= -1;
                var error = target * gameObject.transform.rotation;
                float pitchTarget = error.x * t;
                float yawTarget = error.y * t;
                float rollTarget = error.z * t;
                Vector3 cur = transform.InverseTransformDirection(rb.angularVelocity); //localAngularVelocity
                var rollControl = rollPID.ControlWithLimits(rollTarget, cur.z, Time.fixedTime);
                var pitchControl = pitchPID.ControlWithLimits(pitchTarget, cur.x, Time.fixedTime);
                var yawControl = yawPID.ControlWithLimits(yawTarget, cur.y, Time.fixedTime);
                var depthControl = targetDepth == 0f ? 0 : depthPID.ControlWithLimits(-targetDepth, transform.position.y, Time.fixedTime);
                SetMotors(rollControl, pitchControl, yawControl, depthControl, velocity.z, velocity.y, velocity.x);
            }
            else if (motorsControlMode == "acro")
            {
                Vector3 localangularvelocity = transform.InverseTransformDirection(rb.angularVelocity);
                var rollControl = rollSpeedPID.ControlWithLimits(targetRotationSpeed.z, localangularvelocity.z, Time.fixedTime);
                var pitchControl = pitchSpeedPID.ControlWithLimits(targetRotationSpeed.x, localangularvelocity.x, Time.fixedTime);
                var yawControl = yawSpeedPID.ControlWithLimits(targetRotationSpeed.y, localangularvelocity.y, Time.fixedTime);
                SetMotors(rollControl, pitchControl, yawControl, 0, velocity.z, velocity.y, velocity.x);
            }
        }

        Module.FixedUpdateAll();
    }

    public void SetRawMotors(float FLH, float FLV, float BLV, float BLH, float FRH, float FRV, float BRV, float BRH)
    {
        if (!motorsArmed) return;
        motorFLH.fill = FLH;
        motorFLV.fill = FLV;
        motorBLV.fill = BLV;
        motorBLH.fill = BLH;
        motorFRH.fill = FRH;
        motorFRV.fill = FRV;
        motorBRV.fill = BRV;
        motorBRH.fill = BRH;
    }

    public void SetMotors(float roll, float pitch, float yaw, float globalUp, float localForward, float localUp, float localRight)
    {
        if (!motorsArmed) return;
        var y = new Vector3(0, globalUp, 0);
        float y0 = y.x;
        float y1 = y.y;
        float y2 = y.z;
        var l = transform.TransformDirection(motorFLH.rotation * Vector3.forward);
        var r = transform.TransformDirection(motorFRH.rotation * Vector3.forward);
        var v = transform.TransformDirection(motorFLV.rotation * Vector3.forward);
        float l0 = l.x, l1 = l.y, l2 = l.z;
        float r0 = r.x, r1 = r.y, r2 = r.z;
        float v0 = v.x, v1 = v.y, v2 = v.z;
        float vC = -(l0 * r1 * y2 - l0 * r2 * y1 - l1 * r0 * y2 + l1 * r2 * y0 + l2 * r0 * y1 - l2 * r1 * y0) /
                   (-l0 * r1 * v2 + l0 * r2 * v1 + l1 * r0 * v2 - l1 * r2 * v0 - l2 * r0 * v1 + l2 * r1 * v0);
        float lC = -(r0 * v1 * y2 - r0 * v2 * y1 - r1 * v0 * y2 + r1 * v2 * y0 + r2 * v0 * y1 - r2 * v1 * y0) /
                   (-l0 * r1 * v2 + l0 * r2 * v1 + l1 * r0 * v2 - l1 * r2 * v0 - l2 * r0 * v1 + l2 * r1 * v0);
        float rC = -(-l0 * v1 * y2 + l0 * v2 * y1 + l1 * v0 * y2 - l1 * v2 * y0 - l2 * v0 * y1 + l2 * v1 * y0) /
             (-l0 * r1 * v2 + l0 * r2 * v1 + l1 * r0 * v2 - l1 * r2 * v0 - l2 * r0 * v1 + l2 * r1 * v0);
        /*vC /= 4;
        lC /= 2; //scale up for dual motors
        rC /= 2;*/
        vC /= 2;
        
        float GetFill(float x)
        {
            float ret=0;
            x = x > 1f ? 1f : x < -1f ? -1f : x;
            x = Map(x, -1f, 1f, -4.752412520000007f, 4.752412520000007f);
            if (x < 0) ret = Inverse(x, -0.0000180414f, 0.0591933468f, -48.035f-x, false);
            else ret= Inverse(x, 0.0000281376f, -0.0799177935f, 56.449759077f-x, true);
            return Map(ret, 1100f, 1900f, -1f, 1f);
        }

        float Inverse(float x, float a, float b, float c, bool bigger)
        {
            if (a == 0) return 0;
            float y11 = -((float)Math.Sqrt(-4*a*c + 4*a*x + b*b) + b)/(2*a);
            float y22 = ((float)Math.Sqrt(4*a*(x - c) + b*b) - b)/(2*a);
            return bigger ? (float)Math.Max(y11,y22) : (float)Math.Min(y11,y22);
        }

        motorFLH.fill = 0 - yaw + localForward + localRight + lC;
        motorFRH.fill = 0 + yaw + localForward - localRight + rC;
        motorBLH.fill = 0 + yaw - localForward + localRight - rC;
        motorBRH.fill = 0 - yaw - localForward - localRight - lC;
        motorFLV.fill = 0 + roll + pitch + localUp + vC;
        motorFRV.fill = 0 - roll + pitch + localUp + vC;
        motorBLV.fill = 0 + roll - pitch + localUp + vC;
        motorBRV.fill = 0 - roll - pitch + localUp + vC;

        float max = 1f;
        max = Math.Max(max, Math.Abs(motorFLH.fill));
        max = Math.Max(max, Math.Abs(motorFRH.fill));
        max = Math.Max(max, Math.Abs(motorBLH.fill));
        max = Math.Max(max, Math.Abs(motorBRH.fill));
        max = Math.Max(max, Math.Abs(motorFLV.fill));
        max = Math.Max(max, Math.Abs(motorFRV.fill));
        max = Math.Max(max, Math.Abs(motorBLV.fill));
        max = Math.Max(max, Math.Abs(motorBRV.fill));
        
        motorFLH.fill /= max;
        motorFRH.fill /= max;
        motorBLH.fill /= max;
        motorBRH.fill /= max;
        motorFLV.fill /= max;
        motorFRV.fill /= max;
        motorBLV.fill /= max;
        motorBRV.fill /= max;
        
        motorFLH.fill = GetFill(motorFLH.fill);
        motorFRH.fill = GetFill(motorFRH.fill);
        motorBLH.fill = GetFill(motorBLH.fill);
        motorBRH.fill = GetFill(motorBRH.fill);
        motorFLV.fill = GetFill(motorFLV.fill);
        motorFRV.fill = GetFill(motorFRV.fill);
        motorBLV.fill = GetFill(motorBLV.fill);
        motorBRV.fill = GetFill(motorBRV.fill);
    }
    private float Map(float x, float a, float b, float c, float d) => c + (x - a) * (d - c) / (b - a);
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
    public float radius = 0.05f;
    public bool isClockwise = false;

    public float Force()
    {
        if (rc.transform.TransformPoint(position).y >= 0) return 0f;
        fill = fill > 1f ? 1f : fill < -1f ? -1f : fill;
        float x = Map(fill, -1f, 1f, 1100f, 1900f);
       //  if (fill > 0) return Mathf.Max(0f, a * x * x * x + b * x * x + c * x + d);
       //  else return -Mathf.Max(0f, a * x * x * x + b * x * x + c * x + d);
        if (x < 1472) return x * x * -0.0000180414f + x * 0.0591933468f - 48.035f;
        if (x > 1528) return x * x * 0.0000281376f - x * 0.0799177935f + 56.449759077f;
        return 0f;
    }
    
    public override void FixedUpdate()
    {
        if (!rc.motorsArmed) fill = 0f;
        if (fill == 0f || rc.transform.TransformPoint(position).y > 0) return;
        float force = Force();
        rc.rb.AddForceAtPosition(rc.transform.TransformDirection(rotation * Vector3.forward * force), rc.transform.TransformPoint(position), ForceMode.Force);
      //  Vector3 torque = new Vector3(0, 0, 2f * force * radius * (isClockwise ? 1f : -1f));
       // rc.rb.AddRelativeTorque(rotation * torque, ForceMode.Force); //TODO momentum
    }

    override public void DrawGizmos()
    {
        Gizmos.color = Color.blue;
        if (Force() > 0) Gizmos.color = Color.green;
        if (Force() < 0) Gizmos.color = Color.red;
        if (rc.transform.TransformPoint(position).y >= 0) Gizmos.color = Color.gray;
        Gizmos.DrawLine(rc.transform.TransformPoint(position), rc.transform.TransformPoint(position - rotation * Vector3.forward * Force()/20f));
        Gizmos.DrawSphere(rc.transform.TransformPoint(position), 0.05f / 2f);
    }

    float Map(float x, float a, float b, float c, float d) => c + (x - a) * (d - c) / (b - a);
    public Motor(RobotController _rc) : base(_rc) { }
}

public class Accelerometer : Module
{
    public JSON.Vec3 accel;
    Vector3 acceleration = new Vector3(), lastVelocity = new Vector3();

    public Accelerometer(RobotController _rc) : base(_rc) {
        accel = new JSON.Vec3();
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
    public JSON.Vec3 gyro;
    public JSON.Vec3 rotSpeed;
    public JSON.Vec3 angularAccel;
    public Vector3 angular_acceleration = new Vector3(), lastAngularVelocity = new Vector3();

    public Gyroscope(RobotController _rc) : base(_rc) {
        gyro = new JSON.Vec3();
        rotSpeed = new JSON.Vec3();
        angularAccel = new JSON.Vec3();
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