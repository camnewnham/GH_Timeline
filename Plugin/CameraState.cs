using Rhino.Display;
using Rhino.Geometry;
using System;

namespace Plugin
{
    public struct CameraState : IEquatable<CameraState>
    {
        public enum CameraProjection
        {
            Parallel,
            TwoPointPerspective,
            Perspective
        }

        private Point3d Location;
        private Point3d Target;
        private Vector3d Up;
        private double LensLength;
        private CameraProjection Projection;

        public static CameraState CreateFromViewport(RhinoViewport viewport)
        {
            return new CameraState()
            {
                Location = viewport.CameraLocation,
                Target = viewport.CameraTarget,
                Up = viewport.CameraUp,
                LensLength = viewport.Camera35mmLensLength,
                Projection = viewport.IsPerspectiveProjection ? CameraProjection.Perspective :
                    viewport.IsTwoPointPerspectiveProjection ? CameraProjection.TwoPointPerspective :
                    CameraProjection.Parallel
            };
        }

        public bool Equals(CameraState other)
        {
            return Location.Equals(other.Location) &&
                Target.Equals(other.Target) &&
                Up.Equals(other.Up) &&
                LensLength.Equals(other.LensLength) &&
                Projection.Equals(other.Projection);
        }
    }
}
