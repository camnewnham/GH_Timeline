using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GH_Timeline
{
    public class TimelineHandleLayout : InputHandler
    {
        public readonly TimelineComponentAttributes ParentAttributes;
        private float CurrentTime => (float)ParentAttributes.Owner.CurrentValue;
        public float CurrentTimeXPosition { get; private set; }
        public RectangleF Bounds { get; private set; }
        public RectangleF TextBounds { get; private set; }

        public TimelineHandleLayout(TimelineComponentAttributes owner)
        {
            ParentAttributes = owner;
        }

        public void Layout(RectangleF timelineBounds)
        {
            CurrentTimeXPosition = (float)MathUtils.Lerp(timelineBounds.Left, timelineBounds.Right, CurrentTime);

            Bounds = new RectangleF(CurrentTimeXPosition - 2, timelineBounds.Y, 4, timelineBounds.Height);

            TextBounds = new RectangleF(CurrentTimeXPosition - (16), timelineBounds.Top - 28f, 32, 16);
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
                GH_GraphicsUtil.ShadowVertical(graphics, CurrentTimeXPosition, ParentAttributes.ContentBounds.Bottom, ParentAttributes.ContentBounds.Top, 8f, true, 15);
                GH_GraphicsUtil.ShadowVertical(graphics, CurrentTimeXPosition, ParentAttributes.ContentBounds.Bottom, ParentAttributes.ContentBounds.Top, 8f, false, 15);
                graphics.DrawLine(pen, CurrentTimeXPosition, ParentAttributes.ContentBounds.Bottom, CurrentTimeXPosition, ParentAttributes.ContentBounds.Top);
            }

            if (m_isDragging || GH_Canvas.ZoomFadeHigh > 0)
            {
                // Draw text capsule tooltip

                string content = (CurrentTime * 100).ToString("00.0");
                if (CurrentTime == 1)
                {
                    content = "100";
                }

                int textAlpha = m_isDragging ? 255 : GH_Canvas.ZoomFadeHigh;

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

        private bool m_isDragging = false;
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
                    if (m_isDragging)
                    {
                        // Ignore drag outside of X bounds
                        if ((m_mousePosition.X < ParentAttributes.ContentGraphicsBounds.Left && CurrentTime <= 0) ||
                            (m_mousePosition.X > ParentAttributes.ContentGraphicsBounds.Right && CurrentTime >= 1))
                        {
                            return GH_ObjectResponse.Handled;
                        }

                        if (ParentAttributes.TryGetKeyframe(e.CanvasLocation, out KeyframeLayout kf))
                        {
                            ParentAttributes.Owner.OnTimelineHandleDragged(kf.Keyframe.Time);
                        }
                        else
                        {
                            float pctChange = m_mouseDelta.X / ParentAttributes.ContentBounds.Width;
                            ParentAttributes.Owner.OnTimelineHandleDragged((double)CurrentTime + pctChange);
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
                        m_isDragging = true;
                        _ = ParentAttributes.Owner.RecordUndoEvent("Drag timeline handle");
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

            if (e.Button == MouseButtons.Left && m_isDragging)
            {
                m_isDragging = false;
                Instances.ActiveCanvas.Invalidate();
                return GH_ObjectResponse.Release;
            }

            return GH_ObjectResponse.Ignore;
        }

        public GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            return GH_ObjectResponse.Ignore;
        }

        public IEnumerable<InputHandler> InputHandlers()
        {
            yield break;
        }
    }
}
