﻿using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System.Collections.Generic;
using System.Drawing;

namespace Plugin
{

    internal class SequenceLayout
    {
        public TimelineComponentAttributes Owner;
        public Sequence Sequence;
        public RectangleF Bounds;
        public RectangleF NameBounds;

        public List<KeyframeLayout> KeyframeLayouts = new List<KeyframeLayout>();

        public int NickNameWidth => GH_FontServer.StringWidth(Sequence.Name, GH_FontServer.Small);

        public SequenceLayout(TimelineComponentAttributes owner, Sequence sequence)
        {
            Sequence = sequence;
            Owner = owner;
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
            _ = (Bounds.Bottom + Bounds.Top) / 2;

            foreach (Keyframe keyframe in Sequence.OrderedKeyframes)
            {
                _ = (float)MathUtils.Remap(keyframe.Time, 0, 1, Bounds.Left, Bounds.Right);
                KeyframeLayout keyframeLayout = new KeyframeLayout(this, keyframe);
                keyframeLayout.Layout(Bounds);
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
            graphics.DrawString(Sequence.Name, GH_FontServer.Small, Brushes.Black, NameBounds, GH_TextRenderingConstants.FarCenter);

            foreach (KeyframeLayout keyframe in KeyframeLayouts)
            {
                keyframe.Render(graphics);
            }
        }
    }
}
