using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Plugin
{
    public class TimelineHandleLayout : IGH_ResponsiveObject
    {
        public readonly TimelineComponentAttributes Owner;
        private float CurrentTime => (float)Owner.Owner.CurrentValue;
        public float CurrentTimeXPosition { get; private set; }
        public RectangleF Bounds { get; private set; }
        public RectangleF TextBounds { get; private set; }

        public TimelineHandleLayout(TimelineComponentAttributes owner)
        {
            Owner = owner;
        }

        public void Layout(RectangleF timelineBounds)
        {
            CurrentTimeXPosition = (float)MathUtils.Lerp(timelineBounds.Left, timelineBounds.Right, CurrentTime);

            Bounds = new RectangleF(CurrentTimeXPosition - 2, timelineBounds.Y, 4, timelineBounds.Height);

            TextBounds = new RectangleF(CurrentTimeXPosition - (24 / 2f), timelineBounds.Top - 22f, 24, 12);
        }

        public void Render(Graphics graphics)
        {
            int lineAlpha = GH_Canvas.ZoomFadeLow;
            if (lineAlpha < 5)
            {
                return;
            }

            // Draw Vertical line
            using (Pen pen = new Pen(Color.FromArgb(GH_Canvas.ZoomFadeLow, Color.Black), 1f))
            {
                GH_GraphicsUtil.ShadowVertical(graphics, CurrentTimeXPosition, Owner.ContentBounds.Bottom, Owner.ContentBounds.Top, 8f, true, 15);
                GH_GraphicsUtil.ShadowVertical(graphics, CurrentTimeXPosition, Owner.ContentBounds.Bottom, Owner.ContentBounds.Top, 8f, false, 15);
                graphics.DrawLine(pen, CurrentTimeXPosition, Owner.ContentBounds.Bottom, CurrentTimeXPosition, Owner.ContentBounds.Top);
            }

            if (m_isDraggingSlider || GH_Canvas.ZoomFadeHigh > 0)
            {
                // Draw text capsule tooltip

                string content = (CurrentTime * 100).ToString("00.0");
                if (CurrentTime == 1)
                {
                    content = "100";
                }

                int textAlpha = m_isDraggingSlider ? 255 : GH_Canvas.ZoomFadeHigh;

                using (SolidBrush fill = new SolidBrush(Color.FromArgb((int)(textAlpha * (200f / 255f)), Color.Black)))
                {
                    using (GraphicsPath path = GH_CapsuleRenderEngine.CreateRoundedRectangle(TextBounds, 2f))
                    {
                        path.AddPolygon(new PointF[]
                        {
                        new PointF(CurrentTimeXPosition,TextBounds.Bottom + 3f),
                        new PointF(CurrentTimeXPosition-3f,TextBounds.Bottom),
                        new PointF(CurrentTimeXPosition+3f,TextBounds.Bottom)
                        });

                        path.FillMode = FillMode.Winding;
                        graphics.FillPath(fill, path);
                    }
                }

                using (SolidBrush brush = new SolidBrush(Color.FromArgb(textAlpha, Color.White)))
                {
                    graphics.TextRenderingHint = GH_TextRenderingConstants.GH_SmoothText;
                    graphics.DrawString(content, GH_FontServer.Small, brush, TextBounds, GH_TextRenderingConstants.CenterCenter);
                }
            }
        }

        private bool m_isDraggingSlider = false;
        private PointF m_mousePosition;
        private PointF m_mouseDelta;

        public GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            m_mouseDelta = new PointF(e.CanvasX - m_mousePosition.X, e.CanvasY - m_mousePosition.Y);
            m_mousePosition = e.CanvasLocation;


            switch (e.Button)
            {
                case MouseButtons.None:
                    if (Bounds.Contains(m_mousePosition))
                    {
                        _ = Instances.CursorServer.AttachCursor(sender, "GH_NumericSlider");
                        return GH_ObjectResponse.Handled;
                    }
                    break;
                case MouseButtons.Left:
                    if (m_isDraggingSlider)
                    {
                        if ((m_mousePosition.X < Owner.ContentGraphicsBounds.Left && CurrentTime <= 0) ||
                            (m_mousePosition.X > Owner.ContentGraphicsBounds.Right && CurrentTime >= 1))
                        {
                            return GH_ObjectResponse.Handled;
                        }

                        if (Owner.IsMouseOverKeyframe(out KeyframeLayout kf))
                        {
                            Owner.Owner.OnTimelineHandleDragged(kf.Keyframe.Time);
                        }
                        else
                        {
                            float pctChange = m_mouseDelta.X / Owner.ContentBounds.Width;
                            Owner.Owner.OnTimelineHandleDragged((double)CurrentTime + pctChange);
                        }

                        return GH_ObjectResponse.Handled;
                    }
                    break;
            }

            return GH_ObjectResponse.Ignore;
        }

        public GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            m_mousePosition = e.CanvasLocation;
            switch (e.Button)
            {
                case MouseButtons.Left:
                    if (Bounds.Contains(e.CanvasLocation))
                    {
                        m_isDraggingSlider = true;
                        Instances.ActiveCanvas.Invalidate();
                        return GH_ObjectResponse.Capture;
                    }
                    break;
            }
            return GH_ObjectResponse.Ignore;
        }

        public GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            m_mousePosition = e.CanvasLocation;

            if (e.Button == MouseButtons.Left && m_isDraggingSlider)
            {
                m_isDraggingSlider = false;
                Instances.ActiveCanvas.Invalidate();
                return GH_ObjectResponse.Release;
            }

            return GH_ObjectResponse.Ignore;
        }

        public GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            return GH_ObjectResponse.Ignore;
        }

        public GH_ObjectResponse RespondToKeyDown(GH_Canvas sender, KeyEventArgs e)
        {
            return GH_ObjectResponse.Ignore;
        }

        public GH_ObjectResponse RespondToKeyUp(GH_Canvas sender, KeyEventArgs e)
        {
            return GH_ObjectResponse.Ignore;
        }
    }
}
