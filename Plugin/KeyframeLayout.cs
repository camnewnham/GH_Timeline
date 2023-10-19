using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Plugin
{

    internal class KeyframeLayout : InputHandler
    {
        public Keyframe Keyframe;
        public RectangleF Bounds { get; private set; }
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
            Bounds = new RectangleF((float)(x - 2), y - 2, 4, 4);
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
                    RenderKeyframe(graphics, Bounds.X + (Bounds.Width / 2), Bounds.Y + (Bounds.Width / 2), Bounds.Height / 2, Keyframe.EaseIn, Keyframe.EaseOut, brush, pen);
                }
            }
        }

        public static void RenderKeyframe(Graphics graphics, float x, float y, float radius, Easing left, Easing right, Brush fill, Pen stroke)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                switch (left)
                {
                    case Easing.Linear:
                        path.AddLines(new PointF[]
                        {
                            new PointF(x, y+radius),
                            new PointF(x-radius,y),
                            new PointF(x,y-radius),
                        });
                        break;
                    case Easing.Square:
                        path.AddArc(x - radius, y - radius, radius * 2, radius * 2, 90, 180);
                        break;
                    case Easing.Cubic:
                        path.AddLine(x, y + radius, x - radius / 2, y + radius);
                        path.AddArc(x - radius, y, radius, radius, 90, 90);
                        path.AddLine(x - radius, y + radius / 2, x - radius, y + radius / 2);
                        path.AddArc(x - radius, y - radius, radius, radius, 180, 90);
                        path.AddLine(x - radius / 2, y - radius, x, y - radius);
                        break;
                    case Easing.None:
                    default:
                        path.AddLines(new PointF[]
                        {
                            new PointF(x, y+radius),
                            new PointF(x-radius,y+radius),
                            new PointF(x-radius/2,y),
                            new PointF(x-radius,y-radius),
                            new PointF(x,y-radius),
                        });
                        break;
                }

                switch (right)
                {
                    case Easing.Linear:
                        path.AddLines(new PointF[]
                        {
                            new PointF(x+radius,y)
                        });
                        break;
                    case Easing.Square:
                        path.AddArc(x - radius, y - radius, radius * 2, radius * 2, 270, 180);
                        break;
                    case Easing.Cubic:
                        path.AddArc(x, y - radius, radius, radius, 270, 90);
                        path.AddArc(x, y, radius, radius, 0, 90);
                        break;
                    case Easing.None:
                    default:
                        path.AddLines(new PointF[]
                        {
                            new PointF(x+radius,y-radius),
                            new PointF(x+radius/2,y),
                            new PointF(x+radius,y+radius)
                        });
                        break;
                }

                path.CloseFigure();

                graphics.FillPath(fill, path);
                graphics.DrawPath(stroke, path);
            }
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

            ToolStripDropDownButton easeInDropDown = new ToolStripDropDownButton()
            {
                Text = $"Ease In: {Keyframe.EaseIn}",
            };
            AppendEnumItems(easeInDropDown.DropDown, Keyframe.EaseIn, res =>
            {
                Keyframe.EaseIn = res;
                Owner.Owner.Owner.OnKeyframeChanged();
            });
            menu.Items.Add(easeInDropDown);

            ToolStripDropDownButton easeOutDropDown = new ToolStripDropDownButton()
            {
                Text = $"Ease Out: {Keyframe.EaseOut}",
            };
            AppendEnumItems(easeOutDropDown.DropDown, Keyframe.EaseOut, res =>
            {
                Keyframe.EaseOut = res;
                Owner.Owner.Owner.OnKeyframeChanged();
            });

            menu.Items.Add(easeOutDropDown);

            _ = menu.Items.Add(new ToolStripMenuItem("Delete", null, (obj, arg) =>
            {
                _ = Owner.Sequence.Remove(Keyframe);
                Owner.Owner.Owner.OnKeyframeChanged();
                menu.Close();
            }));
        }

        private static void AppendEnumItems<T>(ToolStripDropDown dropdown, T currentValue, Action<T> onValueChanged) where T : Enum
        {
            foreach (T val in Enum.GetValues(typeof(T)).Cast<T>())
            {
                _ = dropdown.Items.Add(new ToolStripMenuItem(val.ToString(), null, (obj, arg) =>
                {
                    onValueChanged(val);
                })
                {
                    Checked = val.Equals(currentValue)
                });
            }
        }

        public GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Owner.Owner.Selected = false;
                ContextMenuStrip menu = new ContextMenuStrip();
                AppendMenuItems(menu);
                menu.Show(Instances.ActiveCanvas, new Point(e.ControlLocation.X, e.ControlLocation.Y + 10));
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

        public IEnumerable<InputHandler> InputHandlers() { yield break; }
    }
}
