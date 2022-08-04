using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("explosioon");
        StartCoroutine(StartTimer());
    }

    private IEnumerator StartTimer()
    {
        Debug.Log("waiting");
        yield return new WaitForSeconds(1);
        Debug.Log("destroyed");
        Destroy(gameObject);
    }
}
