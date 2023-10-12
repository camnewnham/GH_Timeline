using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System.Collections.Generic;
using System.Drawing;

namespace Plugin
{

    internal class SequenceLayout
    {
        public Sequence Owner;
        public IGH_DocumentObject Component;
        public RectangleF Bounds;
        public RectangleF NameBounds;

        public List<KeyframeLayout> KeyframeLayouts = new List<KeyframeLayout>();

        public int NickNameWidth => GH_FontServer.StringWidth(Component.NickName, GH_FontServer.Small);

        public SequenceLayout(Sequence owner, IGH_DocumentObject referenceObject)
        {
            Owner = owner;
            Component = referenceObject;
        }

        public void Layout(RectangleF bounds, RectangleF nameBounds)
        {
            Bounds = bounds;
            NameBounds = nameBounds;
            LayoutKeyframes();
        }

        private void LayoutKeyframes()
        {
            KeyframeLayouts.Clear();

            float y = (Bounds.Bottom + Bounds.Top) / 2;

            foreach (Keyframe keyframe in Owner.OrderedKeyframes)
            {
                float x = (float)MathUtils.Remap(keyframe.Time, 0, 1, Bounds.Left, Bounds.Right);
                KeyframeLayout keyframeLayout = new KeyframeLayout(keyframe);
                keyframeLayout.Layout(x, y);
                KeyframeLayouts.Add(keyframeLayout);
            }
        }

        public void Render(Graphics graphics)
        {
            if (GH_Canvas.ZoomFadeLow < 1)
            {
                return;
            }

            graphics.TextRenderingHint = GH_TextRenderingConstants.GH_SmoothText;
            graphics.DrawString(Component.NickName, GH_FontServer.Small, Brushes.Black, NameBounds, GH_TextRenderingConstants.FarCenter);

            foreach (KeyframeLayout keyframe in KeyframeLayouts)
            {
                keyframe.Render(graphics);
            }
        }
    }
}
