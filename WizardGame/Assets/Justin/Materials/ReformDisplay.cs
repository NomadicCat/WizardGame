using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReformDisplay : MonoBehaviour
{
    [Header("Target Vertical Resolution (in pixels)")]
    public int targetVerticalResolution = 180;

    [Header("Optional RawImage or Material Display")]
    public Renderer displayRenderer; // If using a Quad in front of another camera
    public UnityEngine.UI.RawImage displayImage; // If using UI

    private Camera cam;
    private RenderTexture renderTex;
    private int lastScreenWidth, lastScreenHeight;

    void Awake()
    {
        cam = GetComponent<Camera>();
        SetupRenderTexture();
    }

    void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            SetupRenderTexture();
        }
    }

    void SetupRenderTexture()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        // Compute target resolution dynamically (based on screen aspect)
        float aspect = (float)Screen.width / Screen.height;
        int targetWidth = Mathf.RoundToInt(targetVerticalResolution * aspect);

        // If resolution matches, skip reallocation
        if (renderTex != null && renderTex.width == targetWidth && renderTex.height == targetVerticalResolution)
            return;

        // Release and recreate only when necessary
        if (renderTex != null)
        {
            renderTex.Release();
        }

        renderTex = new RenderTexture(targetWidth, targetVerticalResolution, 24)
        {
            filterMode = FilterMode.Point,
            useMipMap = false,
            autoGenerateMips = false
        };

        cam.targetTexture = renderTex;

        // Assign to output display (RawImage or Quad)
        if (displayImage != null)
            displayImage.texture = renderTex;
        if (displayRenderer != null)
            displayRenderer.material.mainTexture = renderTex;
    }
}
