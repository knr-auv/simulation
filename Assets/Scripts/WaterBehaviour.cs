using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterBehaviour : MonoBehaviour
{
    public float volume, K1, K2, K1Rot, K2Rot, waterDensity, gravityConstant;
    public Vector3 massCenterOffset = Vector3.zero, volumeCenterOffset = Vector3.zero;
    public bool drawGizmos = true;
    private Rigidbody rigidbody;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        rigidbody.centerOfMass = massCenterOffset;
        gravityConstant = Physics.gravity.magnitude;
    }

    void FixedUpdate()
    {
        rigidbody.centerOfMass = massCenterOffset;
        if (transform.position.y < 0)
        {
            Vector3 buoyancyForce = new Vector3(0, waterDensity * gravityConstant * volume, 0);
            Vector3 T1 = rigidbody.velocity.normalized * -1f * K1 * rigidbody.velocity.magnitude;
            Vector3 T2 = rigidbody.velocity.normalized * -1f * K2 * rigidbody.velocity.sqrMagnitude;
            float reynoldsNumber = T2.magnitude / T1.magnitude;
            Vector3 dragForce = reynoldsNumber < 1 ? T1 : T2;

            Vector3 volumeCenterForce = Vector3.zero;
            volumeCenterForce += buoyancyForce * Mathf.Min(transform.position.y / -0.2f, 1f);//partial submersion
            volumeCenterForce += dragForce;
            rigidbody.AddForceAtPosition(volumeCenterForce, transform.position + transform.rotation * volumeCenterOffset, ForceMode.Force);

            Vector3 T1rot = rigidbody.angularVelocity.normalized * -1f * K1Rot * rigidbody.angularVelocity.magnitude;
            Vector3 T2rot = rigidbody.angularVelocity.normalized * -1f * K2Rot * rigidbody.angularVelocity.sqrMagnitude;
            float reynoldsNumberRot = T2rot.magnitude / T1rot.magnitude;
            Vector3 dragTorque = reynoldsNumberRot < 1 ? T1rot : T2rot;
            rigidbody.AddTorque(dragTorque, ForceMode.Force);//TODO:
        }
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position + transform.rotation * volumeCenterOffset, .02f);
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(transform.position + transform.rotation * massCenterOffset, .02f);
    }
}
