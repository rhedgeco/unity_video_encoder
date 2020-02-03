using System;
using System.Collections;
using NatCorder;
using NatCorder.Clocks;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class RecordCameraTest : MonoBehaviour
{
    private Camera mainCamera;

    [SerializeField] private int fps = 60;
    [SerializeField] private AudioClip clip;

    private void Awake()
    {
        mainCamera = GetComponent<Camera>();
    }

    // Creates a child camera with the same properties and an attached RenderTexture
    private Camera CreateRenderingCamera()
    {
        GameObject g = new GameObject("TextureRenderer");
        Transform mainCameraTransform = mainCamera.transform;
        g.transform.position = mainCameraTransform.position;
        g.transform.rotation = mainCameraTransform.rotation;
        g.transform.parent = mainCameraTransform;
        Camera targetRender = g.AddComponent<Camera>();
        targetRender.CopyFrom(mainCamera);
        RenderTexture targetTexture = new RenderTexture(mainCamera.pixelWidth, mainCamera.pixelHeight, 24)
            {name = "default_render_texture"};
        targetRender.targetTexture = targetTexture;
        return targetRender;
    }

    public void RenderVideo(float length)
    {
        StartCoroutine(RenderRoutine(length));
    }

    private IEnumerator RenderRoutine(float length)
    {
        // Calculate audioData
        int audioSamples = clip.frequency;
        int channels = clip.channels;
        float[] samples = new float[clip.samples * channels];
        clip.GetData(samples, 0);
        int samplesPerFrame = audioSamples / fps;

        // Create output rendering camera
        Camera renderCam = CreateRenderingCamera();
        RenderTexture tex = renderCam.targetTexture;

        // Create native recorder
        MP4Recorder recorder =
            new MP4Recorder(tex.width, tex.height, fps, audioSamples, channels, s => { Debug.Log(s); });
        FixedIntervalClock clock = new FixedIntervalClock(fps);

        // Loop each rendering frame to grab and commit frame and samples
        for (int frame = 0; frame < length * fps; frame++)
        {
            yield return new WaitForEndOfFrame();
            long timestamp = clock.Timestamp;
            Texture2D fTex = RenderTextureToTexture2D(tex);
            float[] commitSamples = GetPartialSampleArray(samples, samplesPerFrame * frame, samplesPerFrame);
            recorder.CommitFrame(fTex.GetPixels32(), timestamp);
            recorder.CommitSamples(commitSamples, timestamp);
            DestroyImmediate(fTex);
            Debug.Log($"Generated Frame {frame}/{(int) (length * fps) - 1}");
        }

        // Complete render and dispose the native recorder
        // Disposing also finishes the file encoding
        recorder.Dispose();
    }
    
    // Copies a smaller section of the sample array
    private float[] GetPartialSampleArray(float[] samples, int startIndex, int length)
    {
        float[] partial = new float[length];
        Array.Copy(samples, startIndex, partial, 0, length);
        return partial;
    }

    // Converts a RenderTexture to Texture2D
    private Texture2D RenderTextureToTexture2D(RenderTexture rendTex)
    {
        Texture2D tex = new Texture2D(rendTex.width, rendTex.height, TextureFormat.RGBA32, false);
        RenderTexture.active = rendTex;
        tex.ReadPixels(new Rect(0, 0, rendTex.width, rendTex.height), 0, 0);
        tex.Apply();
        return tex;
    }
}