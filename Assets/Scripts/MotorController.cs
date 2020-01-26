using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotorController : MonoBehaviour
{
    Rigidbody rb;
    public float maxForce = 22.4f;
    public float fill = 0f;
    public float name;
    private float a = 1.66352902e-8f, b = -0.00003994119f, c = 0.00752234546f, d = 22.4126993334f;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        //if(Mathf.Abs(fill)<=1f && transform.position.y < 0) rb.AddForce(transform.forward*maxForce*fill, ForceMode.Force); //linear force
        if(Mathf.Abs(fill)<=1f && transform.position.y < 0) rb.AddForce(transform.forward*f(fill)*9.8065f, ForceMode.Force); //aprox. force
    }

    private float f(float val)
    {
        float x = Map(val, -1f, 1f, 1100f, 1900f);
        if(val > 0) return Mathf.Max(0f, a * x * x * x + b * x * x + c * x + d);
        else return -Mathf.Max(0f, a * x * x * x + b * x * x + c * x + d);
    }

    private float Map(float x, float a, float b, float c, float d) => c + (x - a) * (d - c) / (b - a);
}
/*
Equations for motor thrust aproximation for 18V
a*1100^3+b*1100^2+c*1100+d=4.5
a*1472^3+b*1472^2+c*1472+d=0
a*1528^3+b*1528^2+c*1528+d=0
a*1852^3+b*1852^2+c*1852+d=5.02

solve a*1100^3+b*1100^2+c*1100+d=4.5 and a*1472^3+b*1472^2+c*1472+d=0 and a*1528^3+b*1528^2+c*1528+d=0 and a*1852^3+b*1852^2+c*1852+d=5.02

a = 127721/7677713952000
b = -3066571/76777139520
c = 94990817/12627819000
d = 26887233496/1199642805
*/