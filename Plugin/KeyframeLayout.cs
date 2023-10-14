using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System.Drawing;
using System.Windows.Forms;

namespace Plugin
{

    internal class KeyframeLayout
    {
        public Keyframe Keyframe;
        public RectangleF Bounds;
        public SequenceLayout Owner;

        private bool m_selected = false;

        public KeyframeLayout(SequenceLayout owner, Keyframe keyframe)
        {
            Owner = owner;
            Keyframe = keyframe;
        }

        public void Layout(RectangleF sequenceBounds)
        {
            double x = MathUtils.Lerp(sequenceBounds.Left, sequenceBounds.Right, Keyframe.Time);
            float y = (sequenceBounds.Top + sequenceBounds.Bottom) / 2f;
            Bounds = new RectangleF((float)(x - 3), y - 3, 6, 6);
        }

        public void Render(Graphics graphics)
        {
            int alpha = GH_Canvas.ZoomFadeLow;
            if (alpha < 5)
            {
                return;
            }

            Color fill = m_selected ? GH_Skin.palette_white_selected.Fill : Color.White;
            Color stroke = m_selected ? GH_Skin.palette_white_selected.Edge : Color.Black;
            _ = m_selected ? GH_Skin.palette_white_selected : GH_Skin.palette_white_standard;
            using (SolidBrush brush = new SolidBrush(fill))
            {
                using (Pen pen = new Pen(stroke))
                {
                    RenderGripDiamond(graphics, Bounds.X + (Bounds.Width / 2), Bounds.Y + (Bounds.Width / 2), 4, brush, pen);
                }
            }
        }

        public static void RenderGripDiamond(Graphics graphics, float x, float y, float radius, Brush fill, Pen stroke)
        {
            int alpha = GH_Canvas.ZoomFadeLow;
            if (alpha < 5)
            {
                return;
            }

            PointF[] polygon = new PointF[]
            {
                new PointF(x-(radius/2),y),
                new PointF(x, y+(radius/2)),
                new PointF(x+(radius/2),y),
                new PointF(x,y-(radius/2))
            };

            graphics.FillPolygon(fill, polygon);
            graphics.DrawPolygon(stroke, polygon);
        }

        internal void AppendMenuItems(ToolStripDropDown menu)
        {
            m_selected = true;
            Instances.ActiveCanvas.Invalidate();

            menu.Closing += (obj, arg) =>
            {
                m_selected = false;
                Instances.ActiveCanvas.Invalidate();
            };

            _ = menu.Items.Add(new ToolStripMenuItem()
            {
                Text = Owner.Sequence.Name,
                Enabled = false,
            });
            _ = GH_DocumentObject.Menu_AppendSeparator(menu);

            NumericUpDown upDown = new NumericUpDown()
            {
                Minimum = 0,
                Maximum = 1,
                DecimalPlaces = 4,
                Increment = (decimal)0.01,
                Value = (decimal)Keyframe.Time,
                TextAlign = HorizontalAlignment.Center
            };
            upDown.ValueChanged += (obj, arg) =>
            {
                Keyframe.Time = (double)upDown.Value;
                Layout(Owner.Bounds);
                Owner.Owner.Owner.OnKeyframeChanged();
                Instances.ActiveCanvas.Invalidate();
            };

            _ = menu.Items.Add(new ToolStripControlHost(upDown));

            _ = menu.Items.Add(new ToolStripMenuItem("Delete", null, (obj, arg) =>
            {
                _ = Owner.Sequence.Remove(Keyframe);
                Owner.Owner.ExpireLayout();
                Instances.ActiveCanvas.Invalidate();
                menu.Close();
            }));
        }

        internal GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            Owner.Owner.Selected = false;
            ContextMenuStrip menu = new ContextMenuStrip();
            AppendMenuItems(menu);
            menu.Show(Instances.ActiveCanvas, new Point(e.ControlLocation.X, e.ControlLocation.Y + 10));
            return GH_ObjectResponse.Handled;
        }
    }

}
