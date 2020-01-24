using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogEnabled : MonoBehaviour
{
    public bool enableFog = false;
    bool previousFogState;
    void OnPreRender()
    {
        previousFogState = RenderSettings.fog;
        RenderSettings.fog = enableFog;
    }
    void OnPostRender()
    {
        RenderSettings.fog = previousFogState;
    }
}
