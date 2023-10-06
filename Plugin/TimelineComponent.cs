using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Plugin
{
    public class TimelineComponent : GH_NumberSlider
    {
        public override Guid ComponentGuid => new Guid("84e977ef-b06d-41e3-aa6d-c6f0f646cef3");
        protected override System.Drawing.Bitmap Icon => null;
        public override GH_ParamKind Kind => GH_ParamKind.floating;

        public TimelineComponent() : base()
        {
            Slider.DecimalPlaces = 8;
            Slider.Minimum = 0;
            Slider.Maximum = 1;
            NickName = "Time";

        }

        public override string Name => "Timeline";
        public override string Description => "Displays keyframes for animating your definition.";

        public override string Category => "Display";
        public override string SubCategory => "Timeline";

        private bool m_recording = false;
        private Action stopRecordingAction;

        public bool Recording
        {
            get => m_recording;
            set
            {
                if (value != m_recording)
                {
                    m_recording = value;
                    if (m_recording)
                    {
                        GH_Document doc = OnPingDocument();
                        Debug.Assert(doc != null, "Can not start recording; document was not found.");
                        Debug.Assert(stopRecordingAction == null, "Can not start recording; stop recording action must be null.");
                        doc.SolutionEnd += OnSolutionEndRecordState;
                        stopRecordingAction = () =>
                        {
                            doc.SolutionEnd -= OnSolutionEndRecordState;
                        };
                    }
                    else
                    {
                        Debug.Assert(stopRecordingAction != null, "Can not stop recording; stop recording action was null.");
                        stopRecordingAction();
                        stopRecordingAction = null;
                    }
                }
            }
        }

        public override void CreateAttributes()
        {
            Attributes = new TimelineComponentAttributes(this);
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendWireDisplay(menu);
            Menu_AppendDisconnectWires(menu);
            Menu_AppendSeparator(menu);

            Menu_AppendItem(menu, Recording ? "Recording" : "Not Recording", (obj, arg) =>
            {
                Recording = !Recording;
                Grasshopper.Instances.ActiveCanvas.Invalidate();
            }, true, Recording);

            Menu_AppendItem(menu, "Animate…", (obj, arg) =>
            {
                GH_SliderAnimator gH_SliderAnimator = new GH_SliderAnimator(this);
                if (gH_SliderAnimator.SetupAnimationProperties())
                {
                    Recording = false;
                    gH_SliderAnimator.StartAnimation();
                }
            });
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            Recording = false;
            base.RemovedFromDocument(document);
        }

        private void OnSolutionEndRecordState(object sender, GH_SolutionEventArgs e)
        {
            // TODO create an entry.
        }
    }
}