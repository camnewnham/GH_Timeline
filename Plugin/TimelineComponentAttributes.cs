﻿using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Plugin
{
    public class TimelineComponentAttributes : GH_ResizableAttributes<TimelineComponent>
    {
        protected override Size MinimumSize => new Size(100, 64);
        protected override Padding SizingBorders => new Padding(6);
        public override bool HasOutputGrip => true;

        public TimelineComponentAttributes(TimelineComponent gradient)
          : base(gradient)
        {
            Bounds = (RectangleF)new Rectangle(0, 0, 250, 64);
        }

        public RectangleF ProgressTextBounds { get; private set; }
        public RectangleF ContentBounds { get; private set; }
        public RectangleF ProgressGrabBarBounds { get; private set; }
        public float CurrentTimeXPosition { get; private set; }

        protected override void Layout()
        {
            Bounds = (RectangleF)(GH_Convert.ToRectangle(new RectangleF(Pivot, Bounds.Size)));

            RectangleF timelineBounds = Bounds;
            timelineBounds.Inflate(-5f, -5f);
            ContentBounds = timelineBounds;

            CurrentTimeXPosition = (float)Remap((double)Owner.CurrentValue, 0, 1, ContentBounds.X, ContentBounds.X + ContentBounds.Width);
            ProgressGrabBarBounds = new RectangleF(CurrentTimeXPosition - 2, ContentBounds.Y, 4, ContentBounds.Height);

            ProgressTextBounds = new RectangleF(CurrentTimeXPosition - 24 / 2f, ContentBounds.Top - 22f, 24, 12);
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            switch (channel)
            {
                case GH_CanvasChannel.Wires:
                    RenderIncomingWires(canvas.Painter, Owner.Sources, Owner.WireDisplay);
                    break;
                case GH_CanvasChannel.Objects:
                    GH_Viewport viewport = canvas.Viewport;
                    RectangleF bounds = Bounds;
                    if (!viewport.IsVisible(ref bounds, 10f))
                    {
                        break;
                    }

                    GH_Palette palette = GH_Palette.Normal;
                    switch (Owner.RuntimeMessageLevel)
                    {
                        case GH_RuntimeMessageLevel.Warning:
                            palette = GH_Palette.Warning;
                            break;
                        case GH_RuntimeMessageLevel.Error:
                            palette = GH_Palette.Error;
                            break;
                    }
                    if (Owner.Locked)
                    {
                        palette = GH_Palette.Locked;
                    }

                    if (Owner.Recording)
                    {
                        palette = GH_Palette.Error; // Use red palette while recording
                    }

                    using (GH_Capsule capsule = GH_Capsule.CreateCapsule(Bounds, palette))
                    {
                        capsule.AddOutputGrip(OutputGrip.Y);
                        graphics.SmoothingMode = SmoothingMode.HighQuality;
                        capsule.Render(graphics, Selected, Owner.Locked, false);
                    }

                    Rectangle rectangle = GH_Convert.ToRectangle(ContentBounds);

                    RenderBackground(graphics);
                    GH_GraphicsUtil.ShadowRectangle(graphics, rectangle);
                    RenderCurrentTime(graphics);
                    RenderSequences(graphics);
                    graphics.DrawRectangle(Pens.Black, rectangle);
                    break;
            }
        }

        private void RenderSequences(Graphics graphics)
        {
            if (Owner.Keyframes.Count == 0)
            {
                return;
            }

            float spacing = ContentBounds.Height / (Owner.Keyframes.Count + 1);
            float offset = spacing;

            foreach (HashSet<TimelineComponent.Keyframe> timeline in Owner.Keyframes.OrderBy(x => x.Key).Select(x => x.Value))
            {
                RenderSequence(graphics, (int)(ContentBounds.Top + offset), timeline.OrderBy(x => x.Time));
                offset += spacing;
            }
        }

        private static double Remap(double value, double low1, double high1, double low2, double high2)
        {
            return low2 + (value - low1) * (high2 - low2) / (high1 - low1);
        }

        public void RenderCurrentTime(Graphics graphics)
        {
            int lineAlpha = GH_Canvas.ZoomFadeLow;
            if (lineAlpha < 5)
            {
                return;
            }

            // Draw Vertical line
            using (Pen pen = new Pen(Color.FromArgb(GH_Canvas.ZoomFadeLow, Color.Black), 1f))
            {
                GH_GraphicsUtil.ShadowVertical(graphics, CurrentTimeXPosition, ContentBounds.Bottom, ContentBounds.Top, 8f, true, 15);
                GH_GraphicsUtil.ShadowVertical(graphics, CurrentTimeXPosition, ContentBounds.Bottom, ContentBounds.Top, 8f, false, 15);
                graphics.DrawLine(pen, CurrentTimeXPosition, ContentBounds.Bottom, CurrentTimeXPosition, ContentBounds.Top);
            }

            if (m_isDraggingSlider || GH_Canvas.ZoomFadeHigh > 0)
            {
                // Draw text capsule tooltip

                string content = (Owner.CurrentValue * 100).ToString("00.0");
                if (Owner.CurrentValue == 1)
                {
                    content = "100";
                }

                int textAlpha = m_isDraggingSlider ? 255 : GH_Canvas.ZoomFadeHigh;

                using (SolidBrush fill = new SolidBrush(Color.FromArgb((int)(textAlpha * (200f / 255f)), Color.Black)))
                {
                    using (GraphicsPath path = GH_CapsuleRenderEngine.CreateRoundedRectangle(ProgressTextBounds, 2f))
                    {
                        path.AddPolygon(new PointF[]
                        {
                        new PointF(CurrentTimeXPosition,ProgressTextBounds.Bottom + 3f),
                        new PointF(CurrentTimeXPosition-3f,ProgressTextBounds.Bottom),
                        new PointF(CurrentTimeXPosition+3f,ProgressTextBounds.Bottom)
                        });

                        path.FillMode = FillMode.Winding;
                        graphics.FillPath(fill, path);
                    }
                }

                using (SolidBrush brush = new SolidBrush(Color.FromArgb(textAlpha, Color.White)))
                {
                    graphics.TextRenderingHint = GH_TextRenderingConstants.GH_SmoothText;
                    graphics.DrawString(content, GH_FontServer.Small, brush, ProgressTextBounds, GH_TextRenderingConstants.CenterCenter);
                }
            }
        }

        private void RenderBackground(Graphics graphics)
        {
            if (GH_Canvas.ZoomFadeLow < 1)
            {
                return;
            }

            const int segmentCount = 10;
            using (Pen pen = new Pen(Color.FromArgb(20 * (int)(GH_Canvas.ZoomFadeLow / 255f), Color.Black), 1f))
            {
                for (int i = 1; i < segmentCount; i++)
                {
                    float x = (float)Remap(i, 0, segmentCount, ContentBounds.Left, ContentBounds.Right);
                    graphics.DrawLine(pen, x, ContentBounds.Bottom, x, ContentBounds.Top);
                }
            }
            if (GH_Canvas.ZoomFadeMedium < 1)
            {
                return;
            }
            using (Pen pen = new Pen(Color.FromArgb(10 * (int)(GH_Canvas.ZoomFadeMedium / 255f), Color.Black), 1f))
            {
                for (int i = 1; i < segmentCount * 5; i++)
                {
                    if (i % 5 == 0)
                    {
                        continue;
                    }

                    float x = (float)Remap(i, 0, segmentCount * 5, ContentBounds.Left, ContentBounds.Right);
                    graphics.DrawLine(pen, x, ContentBounds.Bottom, x, ContentBounds.Top);
                }
            }
        }

        private void RenderSequence(Graphics graphics, float yPos, IEnumerable<TimelineComponent.Keyframe> keyframes)
        {
            if (GH_Canvas.ZoomFadeLow < 1)
            {
                return;
            }


            using (Pen pen = new Pen(Color.FromArgb(40 * (int)(GH_Canvas.ZoomFadeLow / 255f), Color.Black), 1f))
            {
                graphics.DrawLine(pen, ContentBounds.Left, yPos, ContentBounds.Right, yPos);
            }
            foreach (TimelineComponent.Keyframe keyframe in keyframes)
            {
                double xpos = Remap(keyframe.Time, 0, 1, ContentBounds.Left, ContentBounds.Right);
                RenderGripDiamond(graphics, (float)xpos, yPos);
            }
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

        #region Mouse

        private PointF m_mousePosition;
        private PointF m_mouseDelta;
        private bool m_isDraggingSlider;

        public bool IsMouseOverSliderHandle()
        {
            return ProgressGrabBarBounds.Contains(m_mousePosition);
        }

        public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            m_mouseDelta = new PointF(e.CanvasX - m_mousePosition.X, e.CanvasY - m_mousePosition.Y);
            m_mousePosition = e.CanvasLocation;

            switch (e.Button)
            {
                case MouseButtons.None:
                    if (IsMouseOverSliderHandle())
                    {
                        Instances.CursorServer.AttachCursor(sender, "GH_NumericSlider");
                        return GH_ObjectResponse.Handled;
                    }
                    break;
                case MouseButtons.Left:
                    if (m_isDraggingSlider)
                    {
                        float pctChange = m_mouseDelta.X / ContentBounds.Width;
                        Owner.OnTimelineHandleDragged((double)Owner.CurrentValue + pctChange);
                        return GH_ObjectResponse.Handled;
                    }
                    break;
            }
            return base.RespondToMouseMove(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left && m_isDraggingSlider)
            {
                m_isDraggingSlider = false;
                Instances.ActiveCanvas.Invalidate();
                return GH_ObjectResponse.Release;
            }
            return base.RespondToMouseUp(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    if (IsMouseOverSliderHandle())
                    {
                        m_isDraggingSlider = true;
                        Grasshopper.Instances.ActiveCanvas.Invalidate();
                        return GH_ObjectResponse.Capture;
                    }
                    break;
            }

            return base.RespondToMouseDown(sender, e);
        }
        #endregion
    }
}