using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DetectionRectDraw : MonoBehaviour
{
    public Material material;
    public bool doDrawRect;

    Detector detector;
    GUIStyle style;

    void Start()
    {
        detector = new Detector();
        style = new GUIStyle()
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = Color.magenta;
    }

    void OnPostRender()
    {
        if (!doDrawRect) return;
        foreach (var info in detector.Detect(Camera.main))
        {
            if (!info.visibleInFrame) continue;
            GL.PushMatrix();
            GL.LoadOrtho();
            GL.Begin(GL.LINES);
            material.SetPass(0);
            GL.Color(new Color(0f, 0f, 0f, 1f));
            GL.Vertex(new Vector3(info.min.x, info.min.y, 0));
            GL.Vertex(new Vector3(info.min.x, info.max.y, 0));
            GL.Vertex(new Vector3(info.min.x, info.max.y, 0));
            GL.Vertex(new Vector3(info.max.x, info.max.y, 0));
            GL.Vertex(new Vector3(info.max.x, info.max.y, 0));
            GL.Vertex(new Vector3(info.max.x, info.min.y, 0));
            GL.Vertex(new Vector3(info.max.x, info.min.y, 0));
            GL.Vertex(new Vector3(info.min.x, info.min.y, 0));
            GL.End();
            GL.PopMatrix();
        }
    }

    void OnGUI()
    {
        if (!doDrawRect) return;
        
        foreach (var info in detector.Detect(Camera.main))
        {
            if (!info.visibleInFrame) continue;
            GUI.Label(new Rect(info.min.x * Camera.main.pixelWidth, Camera.main.pixelHeight - info.max.y * Camera.main.pixelHeight, 200, 200), info.className + " b: " + (Math.Round(info.fill * 100f, 2) + "%").ToString(), style);
            GUI.Label(new Rect(info.min.x * Camera.main.pixelWidth, Camera.main.pixelHeight - info.max.y * Camera.main.pixelHeight+ style.fontSize, 200, 200), "d: " + Math.Round(info.distance, 1) + "m", style);
            GUI.Label(new Rect(info.min.x * Camera.main.pixelWidth, Camera.main.pixelHeight - info.max.y * Camera.main.pixelHeight+ style.fontSize*2, 200, 200), "v: " + info.visible.ToString(), style);
            GUI.Label(new Rect(info.min.x * Camera.main.pixelWidth, Camera.main.pixelHeight - info.max.y * Camera.main.pixelHeight+ style.fontSize*3, 200, 200), "c: " + Math.Round(info.colorPercentVisible*100f, 2) + "%", style);
            GUI.Label(new Rect(info.min.x * Camera.main.pixelWidth, Camera.main.pixelHeight - info.max.y * Camera.main.pixelHeight+ style.fontSize*4, 200, 200), "i: " + info.includeInDataset.ToString(), style);
            GUI.Label(new Rect(info.min.x * Camera.main.pixelWidth, Camera.main.pixelHeight - info.max.y * Camera.main.pixelHeight+ style.fontSize*5, 200, 200), "f: " + RenderSettings.fogDensity.ToString(), style);
        }
    }

   
}
