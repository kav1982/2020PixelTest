// Copyright Elliot Bentine, 2018-
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Camera behaviour that snaps the positions of all RenderSnapables before rendering and releases them afterwards.
/// </summary>
[
    RequireComponent(typeof(Camera)),
    RequireComponent(typeof(CameraRenderSnapable))
    ]
public class CameraSnapSRP : MonoBehaviour
{

    IEnumerable<AbstractRenderSnapable> _snapables;
    Camera _camera;

    private void Start()
    {
        _camera = GetComponent<Camera>();
        if (!_camera.orthographic)
        {
            Debug.LogWarning("Camera snap is designed to prevent pixel creep in orthographic projection. It is not possible to fix creep using perspective projection, as object pixel size can change.");
            return;
        }
    }

    /// <summary>
    /// Size of a pixel in world units
    /// </summary>
    public float PixelSize = 0.032f;

    public void Update()
    {
        _camera.orthographicSize = _camera.scaledPixelHeight / 2f * PixelSize;
    }

    public void OnEnable()
    {
        RenderPipelineManager.beginFrameRendering += BeginCameraRendering;
        RenderPipelineManager.endFrameRendering += EndCameraRendering;
    }

    public void Unsubscribe()
    {
        RenderPipelineManager.beginFrameRendering -= BeginCameraRendering;
        RenderPipelineManager.endFrameRendering -= EndCameraRendering;
    }

    public void BeginCameraRendering(ScriptableRenderContext context, Camera[] camera) => Snap();

    public void EndCameraRendering(ScriptableRenderContext context, Camera[] camera) => Release();

    public void Snap()
    {
        if (_camera == null)
        {
            // This happens if the camera object gets deleted - but RPM still exists.
            Unsubscribe();
            return;
        }

        float renderScale = (QualitySettings.renderPipeline as UniversalRenderPipelineAsset).renderScale;

        //Find all objects required to snap and release.
        _snapables = new List<AbstractRenderSnapable>(FindObjectsOfType<AbstractRenderSnapable>());

        //Sort snapables so that parents are snapped before their children.
        ((List<AbstractRenderSnapable>)_snapables).Sort((comp1, comp2) => comp1.TransformDepth.CompareTo(comp2.TransformDepth));

        foreach (AbstractRenderSnapable snapable in _snapables)
            snapable.SaveTransform();

        foreach (AbstractRenderSnapable snapable in _snapables)
            snapable.SnapAngles();

        //Determine the size of a square screen pixel in world units.
        float pixelLength = (2f * _camera.orthographicSize) / (_camera.pixelHeight * renderScale);

        //The basis vectors cx,cy,cz correspond to world-space vectors aligned to the screen (x,y,z) axes.
        Vector3 cx, cy, cz;
        Matrix4x4 mat = _camera.cameraToWorldMatrix;
        cx = mat * new Vector3(1, 0, 0);
        cy = mat * new Vector3(0, 1, 0);
        cz = mat * new Vector3(0, 0, 1);

        foreach (AbstractRenderSnapable snapable in _snapables)
        {
            snapable.SnapPositionToGrid(cx, cy, cz, pixelLength);
            snapable.OnSnap();
        }
    }

    public void Release()
    {
        //Note: the `release' loop is run in reverse.
        //This prevents shaking and movement from occuring when
        //parent and child transforms are both Snapable.
        // eg A>B. Snap A, then B. Unsnap B, then A.
        // Doing Unsnap A, then unsnap B will produce jerking.
        //
        // This nesting should still be avoided.
        foreach (AbstractRenderSnapable snapable in _snapables.Reverse())
            snapable.RestoreTransform();
    }

    // In-built support
    public void OnPreRender()
    {
        Snap();
    }

    public void OnPostRender()
    {
        Release();
    }
}