using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace Plugin
{
    public class TimelineComponent : GH_NumberSlider
    {
        public override Guid ComponentGuid => new Guid("84e977ef-b06d-41e3-aa6d-c6f0f646cef3");
        protected override System.Drawing.Bitmap Icon => null;

        public readonly Timeline Timeline;
        public override GH_ParamKind Kind => GH_ParamKind.floating;

        public TimelineComponent() : base()
        {
            Timeline = new Timeline();
            Slider.DecimalPlaces = 8;
            Slider.Minimum = 0;
            Slider.Maximum = 1;
            Slider.ValueChanged += OnSliderValueChanged;
        }

        private void OnSliderValueChanged(object sender, Grasshopper.GUI.Base.GH_SliderEventArgs e)
        {
            Timeline.Time = (double)e.Slider.Value;
        }

        public override string Name => "Timeline";
        public override string NickName => "Timeline";
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
                        doc.SolutionStart += OnSolutionStartRecordState;
                        stopRecordingAction = () =>
                        {
                            doc.SolutionEnd -= OnSolutionEndRecordState;
                            doc.SolutionStart -= OnSolutionStartRecordState;
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

        protected override void OnVolatileDataCollected()
        {
            double time = 0;
            if (VolatileDataCount > 0)
            {
                IGH_Goo goo = VolatileData.AllData(true).First();
                if (!goo.IsValid)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, goo.IsValidWhyNot);
                    return;
                };
                if (goo.CastTo(out double result))
                {
                    if (result < 0 || result > 1)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Time must be between 0 and 1.");
                        result = Math.Min(1, Math.Max(0, result));
                    }
                    time = result;
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Unable to convert to number.");
                }
            }
            if (VolatileDataCount > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Only one input value between 0 and 1 is supported.");
            }
            Timeline.Time = time;
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

        private bool recordNextState = false;
        private void OnSolutionEndRecordState(object sender, GH_SolutionEventArgs e)
        {
            // Ignore components that are upstream of this component
            if (recordNextState)
            {
                Rhino.RhinoApp.WriteLine("Record state!");
            }
            else
            {
                Rhino.RhinoApp.WriteLine("Do not record state!");
            }
        }

        private void OnSolutionStartRecordState(object sender, GH_SolutionEventArgs e)
        {
            // Skip updates where only this component and/or the input slider have changed
            foreach (IGH_DocumentObject obj in e.Document.Objects)
            {
                if (obj != this && obj is IGH_ActiveObject activeObj && activeObj.Phase == GH_SolutionPhase.Blank && !DependsOn(activeObj))
                {
                    recordNextState = true;
                    return;
                }
            }
            recordNextState = false;
            return;
        }
    }
}