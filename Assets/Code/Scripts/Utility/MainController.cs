using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainController : MonoBehaviour
{

    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
}
