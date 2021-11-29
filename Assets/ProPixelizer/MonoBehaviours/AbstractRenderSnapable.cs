// Copyright Elliot Bentine, 2018-
using UnityEngine;
using Pixel3D;

public abstract class AbstractRenderSnapable : MonoBehaviour, IRenderSnapable
{
    /// <summary>
    /// Real position of the object.
    /// </summary>
    private Vector3 _position;

    /// <summary>
    /// Real local rotation of the object.
    /// </summary>
    private Quaternion _realLocalRotation;

    /// <summary>
    /// Real world-space rotation of the object.
    /// </summary>
    private Quaternion _realWorldRotation;

    /// <summary>
    /// parent of the object
    /// </summary>
    private Transform _parent;

    /// <summary>
    /// Depth of the given transform, in the heirachy. Used for snap ordering.
    /// </summary>
    public int TransformDepth { get; private set; }

    public virtual void Start()
    {
        //Determine depth of the given behaviour's transform
        int depth = 0;
        Transform iter = transform;
        while (iter.parent != null && depth < 100)
        {
            depth++;
            iter = iter.parent;
        }
        TransformDepth = depth;
    }

    /// <summary>
    /// Save the current transform.
    /// </summary>
    public void SaveTransform()
    {
        _position = transform.localPosition;
        _realLocalRotation = transform.localRotation;
        _realWorldRotation = transform.rotation;
    }

    /// <summary>
    /// Restore a previously save transform.
    /// </summary>
    public void RestoreTransform()
    {
        transform.localPosition = _position;
        transform.localRotation = _realLocalRotation;
    }

    /// <summary>
    /// Snaps position to points on a grid.
    /// The basis vectors of the grid are given by ex, ey, ez.
    /// The grid spacing is given by spacing.
    /// </summary>
    /// <param name="spacing">distance along vectors (a,b,c) to snap/quantise</param>
    public void SnapPositionToGrid(Vector3 ex, Vector3 ey, Vector3 ez, float spacing)
    {
        //snap position to integer multiples of basis vectors
        Vector3 pos = transform.position;
        float coeffA = Mathf.RoundToInt(Vector3.Dot(pos, ex) / spacing);
        float coeffB = Mathf.RoundToInt(Vector3.Dot(pos, ey) / spacing);
        float coeffC = Mathf.RoundToInt(Vector3.Dot(pos, ez) / spacing);
        float bias = OffsetBias;
        transform.position = spacing * (coeffA + bias) * ex + spacing * (coeffB + bias) * ey + spacing * (coeffC + bias) * ez;
    }

    public abstract float OffsetBias { get; }

    /// <summary>
    /// Resolution (degrees) to which euler angles are snapped.
    /// </summary>
    public virtual float AngleResolution { get { return 30f; } }

    /// <summary>
    /// Should angles be snapped?
    /// </summary>
    public virtual bool ShouldSnapAngles
    {
        get; private set;
    }

    /// <summary>
    /// Snap euler angles to specified AngleResolution.
    /// </summary>
    public void SnapAngles()
    {
        if (!ShouldSnapAngles)
            return;
        Vector3 angles = _realWorldRotation.eulerAngles;
        Vector3 snapped = new Vector3(
            Mathf.Round(angles.x / AngleResolution) * AngleResolution,
            Mathf.Round(angles.y / AngleResolution) * AngleResolution,
            Mathf.Round(angles.z / AngleResolution) * AngleResolution);
        transform.eulerAngles = snapped;
    }

    public abstract void OnSnap();
}
