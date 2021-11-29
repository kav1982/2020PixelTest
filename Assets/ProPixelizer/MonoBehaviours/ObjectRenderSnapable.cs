// Copyright Elliot Bentine, 2018-
using System.Collections.Generic;
using UnityEngine;

public class ObjectRenderSnapable : AbstractRenderSnapable
{
    /// <summary>
    /// Should angles be snapped
    /// </summary>
    [Tooltip("Should angles be snapped?")]
    public bool shouldSnapAngles = true;

    public override bool ShouldSnapAngles
    { get { return shouldSnapAngles; } }

    [Tooltip("Resolution to which angles should be snapped")]
    public float angleResolution = 30f;

    public override float AngleResolution
    { get { return angleResolution; } }

    public override float OffsetBias => 0.5f;

    // Should we use a pixel grid aligned to the root entity's position? 
    public bool UseRootPixelGrid = false;
    private bool _updatePixelGridLocation = false;
    private Renderer _renderer;

    public List<int> PixelGridMaterialIndices;

    public override void Start()
    {
        base.Start();

        PixelGridMaterialIndices = new List<int>();

        _renderer = GetComponent<Renderer>();
        if (_renderer == null)
            return;

        for (int i = 0; i < _renderer.materials.Length; i++)
        {
            if (_renderer.materials[i].HasProperty("_PixelGridOrigin"))
            {
                _renderer.materials[i] = new Material(_renderer.materials[i]);
                if (UseRootPixelGrid)
                {
                    _renderer.materials[i].DisableKeyword("USE_OBJECT_POSITION_ON");
                    _renderer.materials[i].DisableKeyword("USE_OBJECT_POSITION");
                    PixelGridMaterialIndices.Add(i);
                    _updatePixelGridLocation = true;
                }
                else
                {
                    _renderer.materials[i].EnableKeyword("USE_OBJECT_POSITION_ON");
                    _renderer.materials[i].EnableKeyword("USE_OBJECT_POSITION");
                }
            }
        }
    }

    public override void OnSnap()
    {
        if (_updatePixelGridLocation)
        {
            var origin = transform.root.position;
            var origin4 = new Vector4(origin.x, origin.y, origin.z, 1.0f);
            foreach (int i in PixelGridMaterialIndices)
                _renderer.materials[i].SetVector("_PixelGridOrigin", origin4);
        }
    }
}