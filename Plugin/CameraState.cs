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

        public Point3d Location;
        public Point3d Target;
        public Vector3d Up;
        public double LensLength;
        public CameraProjection Projection;

        public CameraState(Point3d location, Point3d target, Vector3d up, double lensLength, CameraProjection projection)
        {
            Location = location;
            Target = target;
            LensLength = lensLength;
            Projection = projection;
            Up = up;
        }

        public CameraState(Point3d location, Point3d target, double rotation, double lensLength, CameraProjection projection)
        {
            Location = location;
            Target = target;
            LensLength = lensLength;
            Projection = projection;
            Up = ComputeUpVector(location, target, rotation);
        }

        public CameraState(RhinoViewport viewport)
        {
            Location = viewport.CameraLocation;
            Target = viewport.CameraTarget;
            Up = viewport.CameraUp;
            Projection = GetProjection(viewport);
            LensLength = Projection == CameraProjection.Parallel ? 50 : viewport.Camera35mmLensLength;
        }


        public double ComputeRotation()
        {
            Vector3d forward = Target - Location;
            _ = forward.Unitize();

            Vector3d right = Vector3d.CrossProduct(forward, Up);
            _ = right.Unitize();

            Plane cameraPlane = new Plane(Location, Up, right);
            Vector3d newUp = cameraPlane.Origin + Vector3d.ZAxis - cameraPlane.Origin;

            return -Vector3d.VectorAngle(cameraPlane.XAxis, newUp, cameraPlane);
        }

        private static Vector3d ComputeUpVector(Point3d location, Point3d target, double rotation)
        {
            Plane plane = new Plane(location, target - location);
            // Align the plane to be upward
            Point3d pt = plane.ClosestPoint(plane.Origin + Vector3d.ZAxis);
            Vector3d up = pt - plane.Origin;
            double angle = Vector3d.VectorAngle(plane.XAxis, up, plane);
            _ = plane.Rotate(angle, plane.ZAxis);
            _ = plane.Rotate(rotation, plane.ZAxis);
            return plane.XAxis;
        }

        public static CameraProjection GetProjection(RhinoViewport viewport)
        {
            return viewport.IsPerspectiveProjection ? CameraProjection.Perspective :
                    viewport.IsTwoPointPerspectiveProjection ? CameraProjection.TwoPointPerspective :
                    CameraProjection.Parallel;
        }

        public void ApplyToViewport(RhinoViewport viewport)
        {
            SetProjection(viewport);
            viewport.SetCameraLocations(Target, Location);
            viewport.CameraUp = Up;

            if (GetProjection(viewport) != CameraProjection.Parallel)
            {
                viewport.Camera35mmLensLength = LensLength;
            }
        }

        private void SetProjection(RhinoViewport viewport)
        {
            switch (Projection)
            {
                case CameraProjection.TwoPointPerspective:
                    if (!viewport.IsTwoPointPerspectiveProjection)
                    {
                        _ = viewport.ChangeToTwoPointPerspectiveProjection(LensLength);
                    }
                    break;
                case CameraProjection.Parallel:
                    if (!viewport.IsParallelProjection)
                    {
                        _ = viewport.ChangeToParallelProjection(false);
                    }
                    break;
                case CameraProjection.Perspective:
                    if (!viewport.IsPerspectiveProjection)
                    {
                        _ = viewport.ChangeToPerspectiveProjection(false, LensLength);
                    }
                    break;
            }
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
