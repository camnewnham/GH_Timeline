using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Plugin
{
    public class TimelineComponentAttributes : GH_ResizableAttributes<TimelineComponent>
    {
        private const int Pad = 6;
        private const int SequenceHeight = 16;
        protected override Size MinimumSize => new Size(192, 64);
        protected override Padding SizingBorders => new Padding(6);
        public override bool HasOutputGrip => true;

        public TimelineComponentAttributes(TimelineComponent gradient)
          : base(gradient)
        {
            Bounds = (RectangleF)new Rectangle(0, 0, 250, 64);
        }

        /// <summary>
        /// The area where progress text is drawn (i.e. 25.2%)
        /// </summary>
        public RectangleF ProgressTextBounds { get; private set; }
        /// <summary>
        /// The main drawing area for the timeline
        /// </summary>
        public RectangleF ContentBounds { get; private set; }
        /// <summary>
        /// <see cref="ContentBounds"/> but with additional padding
        /// </summary>
        public RectangleF ContentGraphicsBounds { get; private set; }
        /// <summary>
        /// The rectangle that represents the grab bar for the progress slider
        /// </summary>
        public RectangleF ProgressGrabBarBounds { get; private set; }
        /// <summary>
        /// The X position in canvas space of the current time slider.
        /// </summary>
        public float CurrentTimeXPosition { get; private set; }

        private List<SequenceLayout> sequenceLayouts = new List<SequenceLayout>();

        protected override void Layout()
        {
            GH_Document doc = Owner.OnPingDocument();
            Bounds = (RectangleF)(GH_Convert.ToRectangle(new RectangleF(Pivot.X, Pivot.Y, Bounds.Width, Math.Max(MinimumSize.Height, 12 + Owner.Timeline.SequenceCount * SequenceHeight))));

            sequenceLayouts.Clear();
            sequenceLayouts.AddRange(Owner.Timeline.Sequences.Values.Select(x => new SequenceLayout(x)));
            sequenceLayouts.Sort((a, b) =>
            {
                // Sort by position in GH document
                if (a.Owner is ComponentSequence csa && b.Owner is ComponentSequence csb)
                {
                    return csa.GetDocumentObject(doc).Attributes.Pivot.Y.CompareTo(csb.GetDocumentObject(doc).Attributes.Pivot.Y);
                }
                return a.Owner.Name.CompareTo(b.Owner.Name);
            });

            int nameAreaWidth = 0;
            foreach (SequenceLayout sequence in sequenceLayouts)
            {
                nameAreaWidth = Math.Min(64, Math.Max(nameAreaWidth, sequence.NickNameWidth));
            }

            RectangleF timelineBounds = new RectangleF(Bounds.X + nameAreaWidth + Pad * 2, Bounds.Y + Pad, Bounds.Width - nameAreaWidth - Pad * 3, Bounds.Height - Pad * 2);
            ContentGraphicsBounds = timelineBounds;
            timelineBounds.Inflate(-6f, 0f);
            ContentBounds = timelineBounds;

            CurrentTimeXPosition = (float)MathUtils.Remap((double)Owner.CurrentValue, 0, 1, ContentBounds.X, ContentBounds.X + ContentBounds.Width);
            ProgressGrabBarBounds = new RectangleF(CurrentTimeXPosition - 2, ContentBounds.Y, 4, ContentBounds.Height);

            ProgressTextBounds = new RectangleF(CurrentTimeXPosition - 24 / 2f, ContentBounds.Top - 22f, 24, 12);

            RectangleF nameRegion = new RectangleF(Bounds.X + Pad, Bounds.Y + Pad, nameAreaWidth, ContentBounds.Height);
            LayoutSequences(nameRegion, ContentBounds);
        }

        private void LayoutSequences(RectangleF nameRegion, RectangleF contentRegion)
        {
            if (sequenceLayouts.Count == 0) return;

            float spacing = ContentBounds.Height / (Owner.Timeline.Sequences.Count + 1);

            float offset = spacing + contentRegion.Top;
            foreach (SequenceLayout sequence in sequenceLayouts)
            {
                RectangleF sequenceBounds = new RectangleF(ContentBounds.X, offset - SequenceHeight / 2, ContentBounds.Width, SequenceHeight);
                RectangleF nameBounds = new RectangleF(nameRegion.X, sequenceBounds.Y, nameRegion.Width, sequenceBounds.Height);
                sequence.Layout(sequenceBounds, nameBounds);
                offset += spacing;
            }
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

                    Rectangle rectangle = GH_Convert.ToRectangle(ContentGraphicsBounds);

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
            foreach (SequenceLayout sq in this.sequenceLayouts)
            {
                float centerY = (sq.Bounds.Bottom + sq.Bounds.Top) / 2;

                using (Pen pen = new Pen(Color.FromArgb(40 * (int)(GH_Canvas.ZoomFadeLow / 255f), Color.Black), 1f))
                {
                    graphics.DrawLine(pen, ContentGraphicsBounds.Left, centerY, ContentGraphicsBounds.Right, centerY);
                }
                sq.Render(graphics);
            }
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
                for (int i = 0; i < segmentCount + 1; i++)
                {
                    float x = (float)MathUtils.Remap(i, 0, segmentCount, ContentBounds.Left, ContentBounds.Right);
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

                    float x = (float)MathUtils.Remap(i, 0, segmentCount * 5, ContentBounds.Left, ContentBounds.Right);
                    graphics.DrawLine(pen, x, ContentBounds.Bottom, x, ContentBounds.Top);
                }
            }
        }

        #region Mouse

        private PointF m_mousePosition;
        private PointF m_mouseDelta;
        private bool m_isDraggingSlider;

        public bool IsMouseOverSliderHandle()
        {
            return ProgressGrabBarBounds.Contains(m_mousePosition);
        }

        public bool IsMouseOverTimelineBounds()
        {
            return ContentGraphicsBounds.Contains(m_mousePosition);
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
                        if ((m_mousePosition.X < ContentGraphicsBounds.Left && Owner.CurrentValue <= 0) ||
                            (m_mousePosition.X > ContentGraphicsBounds.Right && Owner.CurrentValue >= 1))
                        {
                            return GH_ObjectResponse.Handled;
                        }

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
                        Instances.ActiveCanvas.Invalidate();
                        return GH_ObjectResponse.Capture;
                    }
                    break;
            }

            return base.RespondToMouseDown(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    if (IsMouseOverTimelineBounds())
                    {
                        Owner.Recording = !Owner.Recording;
                        Instances.ActiveCanvas.Invalidate();
                        return GH_ObjectResponse.Handled;
                    }
                    break;

            }

            return base.RespondToMouseDoubleClick(sender, e);
        }
        #endregion
    }
}