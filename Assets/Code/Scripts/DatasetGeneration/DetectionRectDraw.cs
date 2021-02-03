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
            fontSize = 10,
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = Color.white;
    }

    void OnPostRender()
    {
        if (!doDrawRect) return;
        GL.PushMatrix();
        GL.LoadOrtho();
        GL.Begin(GL.LINES);
        material.SetPass(0);
        GL.Color(new Color(0f, 0f, 0f, 1f));
        foreach (var info in detector.Detect(Camera.main))
        {
            if (!info.visibleInFrame) continue;
            GL.Vertex(new Vector3(info.min.x, info.min.y, 0));
            GL.Vertex(new Vector3(info.min.x, info.max.y, 0));
            GL.Vertex(new Vector3(info.min.x, info.max.y, 0));
            GL.Vertex(new Vector3(info.max.x, info.max.y, 0));
            GL.Vertex(new Vector3(info.max.x, info.max.y, 0));
            GL.Vertex(new Vector3(info.max.x, info.min.y, 0));
            GL.Vertex(new Vector3(info.max.x, info.min.y, 0));
            GL.Vertex(new Vector3(info.min.x, info.min.y, 0));
        }
        GL.End();
        GL.PopMatrix();
    }

    void OnGUI()
    {
        if (!doDrawRect) return;
        int detected = 0;
       
        foreach (var info in detector.Detect(Camera.main))
        {
            if (!info.visibleInFrame) continue;
            GUI.Label(new Rect(info.min.x * Camera.main.pixelWidth, Camera.main.pixelHeight - info.max.y * Camera.main.pixelHeight, 200, 200), info.className, style);
            GUI.Label(new Rect(info.min.x * Camera.main.pixelWidth, Camera.main.pixelHeight - info.max.y * Camera.main.pixelHeight + style.fontSize * 1, 200, 200), "d: " + Math.Round(info.distance, 1) + "m", style);
            GUI.Label(new Rect(info.min.x * Camera.main.pixelWidth, Camera.main.pixelHeight - info.max.y * Camera.main.pixelHeight + style.fontSize * 2, 200, 200), "c: " + Math.Round(info.colorPercentVisible * 100f, 2) + "%", style);
            GUI.Label(new Rect(info.min.x * Camera.main.pixelWidth, Camera.main.pixelHeight - info.max.y * Camera.main.pixelHeight + style.fontSize * 3, 200, 200), "b: " + (Math.Round(info.fill * 100f, 2) + "%").ToString(), style);
            GUI.Label(new Rect(info.min.x * Camera.main.pixelWidth, Camera.main.pixelHeight - info.max.y * Camera.main.pixelHeight + style.fontSize * 4, 200, 200), "v: " + (info.visible && info.fill >= Settings.config.datasetOptions.minObjectFill ? "T" : "F"), style);
            GUI.Label(new Rect(info.min.x * Camera.main.pixelWidth, Camera.main.pixelHeight - info.max.y * Camera.main.pixelHeight + style.fontSize * 5, 200, 200), "i: " + (info.includeInDataset ? "T" : "F"), style);
            if (info.visible && info.includeInDataset && info.fill >= Settings.config.datasetOptions.minObjectFill) detected++;
        }
        GUI.Label(new Rect(0, style.fontSize * 0, 200, 200), "fov: " + Camera.main.fieldOfView + "st", style);
        GUI.Label(new Rect(0, style.fontSize * 1, 200, 200), "f: " + (Math.Round(RenderSettings.fogDensity, 3)).ToString(), style);
        GUI.Label(new Rect(0, style.fontSize * 2, 200, 200), "t: " + detected.ToString(), style);
    }

   
}
