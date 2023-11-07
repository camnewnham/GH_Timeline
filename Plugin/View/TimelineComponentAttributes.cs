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

namespace GH_Timeline
{
    public class TimelineComponentAttributes : GH_ResizableAttributes<TimelineComponent>
    {
        private const int Pad = 6;
        private const int SequenceHeight = 10;
        protected override Size MinimumSize => new Size(192, 64);
        protected override Padding SizingBorders => new Padding(6);
        public override bool HasOutputGrip => true;

        public TimelineComponentAttributes(TimelineComponent owner)
          : base(owner)
        {
            Bounds = (RectangleF)new Rectangle(0, 0, 250, 64);
            handle = new TimelineHandleLayout(this);
        }

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


        private readonly List<SequenceLayout> sequences = new List<SequenceLayout>();
        private readonly TimelineHandleLayout handle;

        protected override void Layout()
        {
            Bounds = (RectangleF)GH_Convert.ToRectangle(new RectangleF(Pivot.X, Pivot.Y, Bounds.Width, Math.Max(MinimumSize.Height, 12 + (Owner.Timeline.SequenceCount * SequenceHeight))));

            sequences.Clear();
            sequences.AddRange(Owner.Timeline.Sequences.Values.Select(x => new SequenceLayout(this, x)));
            sequences.Sort((a, b) => a.Sequence.Sort.CompareTo(b.Sequence.Sort));

            int nameAreaWidth = 0;
            foreach (SequenceLayout sequence in sequences)
            {
                nameAreaWidth = Math.Min(64, Math.Max(nameAreaWidth, sequence.NickNameWidth));
            }

            RectangleF timelineBounds = new RectangleF(Bounds.X + nameAreaWidth + (Pad * 2), Bounds.Y + Pad, Bounds.Width - nameAreaWidth - (Pad * 3), Bounds.Height - (Pad * 2));
            ContentGraphicsBounds = timelineBounds;
            timelineBounds.Inflate(-6f, 0f);
            ContentBounds = timelineBounds;

            handle.Layout(ContentBounds);

            RectangleF nameRegion = new RectangleF(Bounds.X + Pad, Bounds.Y + Pad, nameAreaWidth, ContentBounds.Height);
            LayoutSequences(nameRegion, ContentBounds);
        }

        private void LayoutSequences(RectangleF nameRegion, RectangleF contentRegion)
        {
            if (sequences.Count == 0)
            {
                return;
            }

            float spacing = ContentBounds.Height / (Owner.Timeline.Sequences.Count + 1);

            float offset = spacing + contentRegion.Top;
            foreach (SequenceLayout sequence in sequences)
            {
                RectangleF sequenceBounds = new RectangleF(ContentBounds.X, offset - ((SequenceHeight + Pad) / 2), ContentBounds.Width, SequenceHeight + Pad);
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
                    handle.Render(graphics);
                    RenderSequences(graphics);
                    graphics.DrawRectangle(Pens.Black, rectangle);
                    break;
            }
        }

        private void RenderSequences(Graphics graphics)
        {
            foreach (SequenceLayout sq in sequences)
            {
                float centerY = (sq.TimelineBounds.Bottom + sq.TimelineBounds.Top) / 2;

                using (Pen pen = new Pen(Color.FromArgb(40 * (int)(GH_Canvas.ZoomFadeLow / 255f), Color.Black), 1f))
                {
                    graphics.DrawLine(pen, ContentGraphicsBounds.Left, centerY, ContentGraphicsBounds.Right, centerY);
                }
                sq.Render(graphics);
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

        internal bool TryGetKeyframe(PointF canvasPosition, out KeyframeLayout keyframe)
        {
            foreach (SequenceLayout sq in sequences)
            {
                if (sq.Bounds.Contains(canvasPosition))
                {
                    foreach (KeyframeLayout kf in sq.KeyframeLayouts)
                    {
                        if (kf.Bounds.Contains(canvasPosition))
                        {
                            keyframe = kf;
                            return true;
                        }
                    }
                }
            }
            keyframe = null;
            return false;
        }

        private readonly InputForwarder m_inputForwarder = new InputForwarder();

        private IEnumerable<InputHandler> InputHandlers()
        {
            foreach (SequenceLayout sq in sequences)
            {
                yield return sq;
            }
            yield return handle;
        }

        public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            m_inputForwarder.InputHandlers = InputHandlers();
            return m_inputForwarder.RespondToMouseMove(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            m_inputForwarder.InputHandlers = InputHandlers();
            if (m_inputForwarder.RespondToMouseUp(sender, e) is GH_ObjectResponse res && res != GH_ObjectResponse.Ignore)
            {
                return res;
            }

            if (e.Button == MouseButtons.Right)
            {
                if (TryGetKeyframe(e.CanvasLocation, out KeyframeLayout kf))
                {
                    return kf.RespondToMouseUp(sender, e);
                }
            }
            return base.RespondToMouseUp(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            m_inputForwarder.InputHandlers = InputHandlers();
            return m_inputForwarder.RespondToMouseDown(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            m_inputForwarder.InputHandlers = InputHandlers();
            if (m_inputForwarder.RespondToMouseDoubleClick(sender, e) is GH_ObjectResponse res && res != GH_ObjectResponse.Ignore)
            {
                return res;
            }

            switch (e.Button)
            {
                case MouseButtons.Left:
                    Owner.Recording = !Owner.Recording;
                    Instances.ActiveCanvas.Invalidate();
                    return GH_ObjectResponse.Handled;
            }

            return base.RespondToMouseDoubleClick(sender, e);
        }
        #endregion
    }
}