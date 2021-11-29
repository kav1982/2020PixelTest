// Copyright Elliot Bentine, 2018-

/// <summary>
/// Snaps camera to position during rendering.
/// Used to prevent jitter on pixels when moving camera.
/// </summary>
public class CameraRenderSnapable : AbstractRenderSnapable
{
    /// <summary>
    /// Camera angles are never snapped; returns false
    /// </summary>
    public override bool ShouldSnapAngles
    { get { return false; } }

    public override float OffsetBias => 0f;

    public override void OnSnap()
    {
    }
}