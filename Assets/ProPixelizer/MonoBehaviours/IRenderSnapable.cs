// Copyright Elliot Bentine, 2018-
using UnityEngine;

namespace Pixel3D
{
    /// <summary>
    /// An interface for objects that can be snapped into position/orientation for rendering.
    /// </summary>
    public interface IRenderSnapable
    {
        /// <summary>
        /// Save the current transform.
        /// </summary>
        void SaveTransform();

        /// <summary>
        /// Restore a previously save transform.
        /// </summary>
        void RestoreTransform();

        /// <summary>
        /// Snaps position to points on a grid.
        /// The basis vectors of the grid are given by ex, ey, ez.
        /// The grid spacing is given by spacing.
        /// </summary>
        /// <param name="spacing">distance along vectors (a,b,c) to snap/quantise</param>
        void SnapPositionToGrid(Vector3 ex, Vector3 ey, Vector3 ez, float spacing);

        /// <summary>
        /// Snap object rotations
        /// </summary>
        void SnapAngles();
    }
}
