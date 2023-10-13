using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;

namespace Plugin
{
    public enum Easing : int
    {
        None = 0,
        Linear = 1,
        Square = 2,
        Cubic = 3,
        Quartic = 4,
        Quintic = 5
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
            return Lerp(EaseIn(t, startEase), EaseOut(t, endEase), t);
        }

        public static double Lerp(double a, double b, double t)
        {
            return a + ((b - a) * t);
        }

        public static string GetName(this IGH_DocumentObject docObj)
        {
            if (docObj is GH_NumberSlider slider)
            {
                return slider.ImpliedNickName;
            }
            return !string.IsNullOrEmpty(docObj.NickName) ? docObj.NickName : docObj.Name;
        }
    }
}
