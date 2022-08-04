using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TorpedoController : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private GameObject explosion;
    private float timer = 2;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.velocity = Vector3.forward * 10;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        timer -= Time.fixedDeltaTime;
        rb.AddRelativeForce(new Vector3(0,0, 1));
    }

    private void OnCollisionEnter(Collision other)
    {
        if (timer > 0) return;
        Instantiate(explosion, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
