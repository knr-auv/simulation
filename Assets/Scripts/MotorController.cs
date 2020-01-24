using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotorController : MonoBehaviour
{
    Rigidbody rb;
    public float maxForce = 22.4f;
    public float fill = 0f;
    public float name;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

 
    void FixedUpdate()
    {
        if(Mathf.Abs(fill)<=1f && transform.position.y < 0) rb.AddForce(transform.forward*maxForce*fill, ForceMode.Force);
    }
}
