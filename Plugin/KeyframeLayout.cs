using Grasshopper.GUI.Canvas;
using System.Drawing;

namespace Plugin
{

    internal class KeyframeLayout
    {
        public Keyframe Owner;
        public RectangleF Bounds;
        public KeyframeLayout(Keyframe keyframe)
        {
            Owner = keyframe;
        }

        public void Layout(float x, float y)
        {
            Bounds = new RectangleF(x - 3, y - 3, 6, 6);
        }

        public void Render(Graphics graphics)
        {
            RenderGripDiamond(graphics, Bounds.X + Bounds.Width / 2, Bounds.Y + Bounds.Width / 2, 4);
        }

        public static void RenderGripDiamond(Graphics graphics, float x, float y, float radius = 4)
        {

            int alpha = GH_Canvas.ZoomFadeLow;
            if (alpha < 5)
            {
                return;
            }

            PointF[] polygon = new PointF[]
            {
                new PointF(x-radius/2,y),
                new PointF(x, y+radius/2),
                new PointF(x+radius/2,y),
                new PointF(x,y-radius/2)
            };

            graphics.FillPolygon(Brushes.White, polygon);
            graphics.DrawPolygon(Pens.Black, polygon);
        }
    }

}
