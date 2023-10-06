using System.Drawing;

namespace Plugin
{
    internal static class TimelineRenderer
    {
        public static void RenderCurrentTime(this Timeline timeline, Graphics graphics, RectangleF rect)
        {
            using (Pen pen = new Pen(Color.Azure))
            {
                float x = (float)Remap(timeline.Time, 0, 1, rect.X, rect.X + rect.Width);
                graphics.DrawLine(pen, x, rect.Bottom, x, rect.Top);

            }
        }

        private static double Remap(double value, double low1, double high1, double low2, double high2)
        {
            return low2 + (value - low1) * (high2 - low2) / (high1 - low1);
        }
    }
}
