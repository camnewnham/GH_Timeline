using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GH_Timeline
{
    /// <summary>
    /// GUI for editing a sequence and laying out keyframes.
    /// </summary>
    internal class SequenceLayout : InputHandler
    {
        public TimelineComponentAttributes ParentAttributes;
        public Sequence Sequence;
        public RectangleF Bounds { get; private set; }
        public RectangleF TimelineBounds { get; private set; }
        public RectangleF NameBounds { get; private set; }

        public List<KeyframeLayout> KeyframeLayouts = new List<KeyframeLayout>();

        public int NickNameWidth => GH_FontServer.StringWidth(Sequence.Name, GH_FontServer.Small);

        public SequenceLayout(TimelineComponentAttributes owner, Sequence sequence)
        {
            Sequence = sequence;
            ParentAttributes = owner;
        }

        public void Layout(RectangleF bounds, RectangleF nameBounds)
        {
            TimelineBounds = bounds;
            NameBounds = nameBounds;
            Bounds = RectangleF.Union(TimelineBounds, NameBounds);
            LayoutKeyframes();
        }

        private void LayoutKeyframes()
        {
            KeyframeLayouts.Clear();
            _ = (TimelineBounds.Bottom + TimelineBounds.Top) / 2;

            foreach (Keyframe keyframe in Sequence.OrderedKeyframes)
            {
                _ = (float)MathUtils.Remap(keyframe.Time, 0, 1, TimelineBounds.Left, TimelineBounds.Right);
                KeyframeLayout keyframeLayout = new KeyframeLayout(this, keyframe);
                keyframeLayout.Layout(TimelineBounds);
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

        #region Input

        public IEnumerable<InputHandler> InputHandlers()
        {
            foreach (KeyframeLayout keyframe in KeyframeLayouts)
            {
                yield return keyframe;
            }
        }

        public GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Right && NameBounds.Contains(e.CanvasLocation))
            {
                ContextMenuStrip menu = new ContextMenuStrip();
                AppendMenuItems(menu);
                menu.Show(Instances.ActiveCanvas, new Point(e.ControlLocation.X, e.ControlLocation.Y));
                return GH_ObjectResponse.Handled;
            }
            return GH_ObjectResponse.Ignore;
        }

        public GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            return GH_ObjectResponse.Ignore;
        }

        public GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            return GH_ObjectResponse.Ignore;
        }

        public GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            return GH_ObjectResponse.Ignore;
        }


        internal void AppendMenuItems(ToolStripDropDown menu)
        {
            _ = GH_DocumentObject.Menu_AppendItem(menu, Sequence.Name, null, false);
            _ = GH_DocumentObject.Menu_AppendSeparator(menu);
            _ = GH_DocumentObject.Menu_AppendItem(menu, "Delete", (obj, arg) =>
            {
                _ = ParentAttributes.Owner.RecordUndoEvent("Delete Sequence");
                _ = ParentAttributes.Owner.Timeline.RemoveSequence(Sequence);
                ParentAttributes.Owner.OnKeyframeChanged();
            });
        }

        #endregion
    }
}
