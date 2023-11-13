using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;
using System;

namespace GH_Timeline
{
    public enum Easing : int
    {
        None = 0,
        Linear = 1,
        Exponential = 2
    }
    public static class MathUtils
    {
        public static double Remap(double value, double low1, double high1, double low2, double high2)
        {
            return low2 + ((value - low1) * (high2 - low2) / (high1 - low1));
        }

        public static double EaseIn(double t, Easing easing)
        {
            return Math.Pow(t, (double)easing);
        }

        public static double EaseOut(double t, Easing easing)
        {
            return 1 - EaseIn(1 - t, easing);
        }
        public static double EaseInOut(double t, Easing startEase, Easing endEase)
        {
            if (startEase == Easing.None && endEase == Easing.None)
            {
                return 0;
            }
            else if (startEase == Easing.None)
            {
                return EaseOut(t, endEase);
            }
            else if (endEase == Easing.None)
            {
                return EaseIn(t, startEase);
            }

            return Lerp(EaseIn(t, startEase), EaseOut(t, endEase), t);
        }
        public static double EaseInOut(double t, double from, double to, Easing startEase, Easing endEase)
        {
            t = EaseInOut(t, startEase, endEase);
            return ((1 - t) * from) + (t * to);
        }

        public static Vector3d EaseInOut(double t, Vector3d from, Vector3d to, Easing startEase, Easing endEase)
        {
            t = EaseInOut(t, startEase, endEase);
            return ((1 - t) * from) + (t * to);
        }

        public static Point3d EaseInOut(double t, Point3d from, Point3d to, Easing startEase, Easing endEase)
        {
            t = EaseInOut(t, startEase, endEase);
            return ((1 - t) * from) + (t * to);
        }

        public static CameraState EaseInOut(double t, CameraState from, CameraState to, Easing startEase, Easing endEase)
        {
            return Lerp(from, to, EaseInOut(t, startEase, endEase));
        }

        /// <summary>
        /// Wraps rotation angles around PI to ensure the shortest rotation distance
        /// </summary>
        private static void MinimizeRotation(ref double a, ref double b)
        {
            while (Math.Abs(b - a) > Math.PI)
            {
                if (b > a)
                {
                    b -= Math.PI * 2;
                }
                else
                {
                    b += Math.PI * 2;
                }
            }
        }

        public static double Lerp(double a, double b, double t)
        {
            return a + ((b - a) * t);
        }

        public static CameraState Lerp(CameraState from, CameraState to, double scaledTime)
        {
            Point3d location = Lerp(from.Location, to.Location, scaledTime);
            Point3d target = Lerp(from.Target, to.Target, scaledTime);

            double rA = from.ComputeRotation();
            double rB = to.ComputeRotation();
            MinimizeRotation(ref rA, ref rB);

            double rotation = Lerp(rA, rB, scaledTime);
            double lens = Lerp(from.LensLength, to.LensLength, scaledTime);
            return new CameraState(location, target, rotation, lens, from.Projection);
        }


        public static Point3d Lerp(Point3d a, Point3d b, double t)
        {
            return a + ((b - a) * t);
        }

        public static string GetName(this IGH_DocumentObject docObj)
        {
            return docObj is GH_NumberSlider slider
                ? slider.ImpliedNickName
                : !string.IsNullOrEmpty(docObj.NickName) ? docObj.NickName : docObj.Name;
        }
    }
}
