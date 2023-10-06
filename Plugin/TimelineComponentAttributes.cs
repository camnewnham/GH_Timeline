using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using System.Drawing;
using System.Drawing.Drawing2D;
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

        public RectangleF TimelineBounds
        {
            get
            {
                RectangleF box = Bounds;
                box.Inflate(-6f, -6f);
                return box;
            }
        }
        public RectangleF RecordingBounds
        {
            get
            {
                RectangleF box = Bounds;
                box.Inflate(6f, 6f);
                return box;
            }
        }

        protected override void Layout()
        {
            Bounds = (RectangleF)GH_Convert.ToRectangle(new RectangleF(Pivot, Bounds.Size));
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

                    GH_Palette palette = GH_Palette.Hidden;
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

                    using (GH_Capsule capsule = GH_Capsule.CreateCapsule(Bounds, palette))
                    {
                        //capsule.SetJaggedEdges(true, true);
                        //capsule.AddInputGrip(InputGrip.Y);
                        capsule.AddOutputGrip(OutputGrip.Y);

                        graphics.SmoothingMode = SmoothingMode.HighQuality;
                        capsule.Render(graphics, Selected, Owner.Locked, false);
                    }

                    Rectangle rectangle = GH_Convert.ToRectangle(TimelineBounds);
                    //Owner.Gradient.Render_Background(graphics, (RectangleF)rectangle);
                    //Owner.Gradient.Render_Gradient(graphics, (RectangleF)rectangle);
                    if (Owner.Recording)
                    {
                        graphics.FillRectangle(new SolidBrush(Color.FromArgb(200, 0, 0)), rectangle);
                    }
                    Owner.Timeline.RenderCurrentTime(graphics, rectangle);
                    GH_GraphicsUtil.ShadowRectangle(graphics, rectangle);
                    graphics.DrawRectangle(Pens.Black, rectangle);
                    //Owner.Gradient.Render_Grips(graphics, (RectangleF)rectangle);
                    //GH_PaletteStyle impliedStyle = GH_CapsuleRenderEngine.GetImpliedStyle(palette, Selected, Owner.Locked, false);
                    //GH_ComponentAttributes.RenderComponentParameters(canvas, graphics, Owner, impliedStyle);
                    break;
            }
        }
    }
}