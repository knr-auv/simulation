using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraStream : MonoBehaviour
{
    Queue<AsyncGPUReadbackRequest> _requests = new Queue<AsyncGPUReadbackRequest>();
    /*
    void Update()
    {
        while (_requests.Count > 0)
        {
            var req = _requests.Peek();

            if (req.hasError)
            {
                Debug.Log("GPU readback error detected.");
                _requests.Dequeue();
            }
            else if (req.done)
            {
                var buffer = req.GetData<Color32>();

                if (Time.frameCount % 60 == 0)
                {
                    var camera = GetComponent<Camera>();
                    SaveBitmap(buffer, camera.pixelWidth, camera.pixelHeight);
                }

                _requests.Dequeue();
            }
            else
            {
                break;
            }
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (_requests.Count < 8)
            _requests.Enqueue(AsyncGPUReadback.Request(source));
        else
            Debug.Log("Too many requests.");

        Graphics.Blit(source, destination);
    }

    void SaveBitmap(NativeArray<Color32> buffer, int width, int height)
    {
        Debug.Log(width + " " + height + "\n" + buffer.Length);
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.SetPixels32(buffer.ToArray());
        tex.Apply();
        
       // File.WriteAllBytes("test.png", ImageConversion.EncodeToPNG(tex));
        Destroy(tex);
    }*//*
    byte[] newImage;
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        var tempRT = RenderTexture.GetTemporary(source.width, source.height);
        Graphics.Blit(source, tempRT);

        var tempTex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        tempTex.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, false);
        tempTex.Apply();

        newImage = tempTex.EncodeToJPG(80);
        Destroy(tempTex);
        RenderTexture.ReleaseTemporary(tempRT);

        Graphics.Blit(source, destination);
    }*///not wurking


}
