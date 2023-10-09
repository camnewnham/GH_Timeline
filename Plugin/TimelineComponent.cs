using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;
using System.Collections.Generic;
using System.Linq;
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
            set => m_recording = value;
        }

        public override void CreateAttributes()
        {
            Attributes = new TimelineComponentAttributes(this);
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendSeparator(menu);

            Menu_AppendItem(menu, Recording ? "Stop Recording" : "Record", (obj, arg) =>
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
            document.SolutionEnd += OnSolutionEndRecordState;
            document.SolutionStart += OnSolutionStartRecordState;
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            Recording = false;
            document.SolutionEnd -= OnSolutionEndRecordState;
            document.SolutionStart -= OnSolutionStartRecordState;
            base.RemovedFromDocument(document);
        }

        /// <summary>
        /// Cache of IGH_DocumentObject that expired during the last solution.
        /// </summary>
        private HashSet<IGH_DocumentObject> m_expiredObjects = new HashSet<IGH_DocumentObject>();

        public readonly Dictionary<Guid, HashSet<Keyframe>> Keyframes = new Dictionary<Guid, HashSet<Keyframe>>();

        internal void OnTimelineHandleDragged(double newValue)
        {
            SetSliderValue((decimal)newValue);
            ExpireSolution(true);
        }

        private bool m_wasExpirationInitiatedBySliderValue = false;

        private void OnSolutionEndRecordState(object sender, GH_SolutionEventArgs e)
        {

            if (m_wasExpirationInitiatedBySliderValue)
            {
                m_wasExpirationInitiatedBySliderValue = false;
                return;
            }

            if (!Recording)
            {
                return;
            }

            foreach (IGH_DocumentObject docObj in m_expiredObjects)
            {
                // TODO: Handle custom objects (i.e. sliders)
                if (docObj is IGH_StateAwareObject stateAwareObj)
                {
                    AddKeyframe(new StateAwareKeyframe(stateAwareObj, (double)CurrentValue));
                }
            }
        }

        private void AddKeyframe(Keyframe keyFrame)
        {
            if (!(Keyframes.TryGetValue(keyFrame.InstanceGuid, out HashSet<Keyframe> frames)))
            {
                Keyframes[keyFrame.InstanceGuid] = frames = new HashSet<Keyframe>();
            }
            frames.RemoveWhere(kf => kf.Time == keyFrame.Time);
            frames.Add(keyFrame);
        }

        private void OnSolutionStartRecordState(object sender, GH_SolutionEventArgs e)
        {
            m_wasExpirationInitiatedBySliderValue = Phase == GH_SolutionPhase.Blank;
            if (!Recording || m_wasExpirationInitiatedBySliderValue)
            {
                return;
            }

            m_expiredObjects.Clear();

            foreach (IGH_DocumentObject obj in e.Document.Objects.Where(
                x => x is IGH_ActiveObject activeObj &&
                activeObj.Phase == GH_SolutionPhase.Blank
            ))
            {
                m_expiredObjects.Add(obj);
            }
        }

        public abstract class Keyframe
        {
            public Guid InstanceGuid { get; set; }
            public double Time { get; set; }

            public Keyframe(IGH_DocumentObject obj, double time)
            {
                InstanceGuid = obj.InstanceGuid;
                Time = time;
            }

            public IGH_DocumentObject LoadState(GH_Document doc)
            {
                IGH_DocumentObject obj = doc.FindObject(InstanceGuid, true);
                if (obj == null)
                {
                    throw new KeyNotFoundException($"Unable to find document object to restore state ({InstanceGuid})");
                }

                Load(obj);
                return obj;
            }

            protected abstract void Load(IGH_DocumentObject obj);

        }

        [Serializable]
        public class StateAwareKeyframe : Keyframe
        {
            private string m_state;

            public StateAwareKeyframe(IGH_StateAwareObject stateAwareObj, double time) : base(stateAwareObj as IGH_DocumentObject, time)
            {
                m_state = stateAwareObj.SaveState();
            }

            protected override void Load(IGH_DocumentObject obj)
            {
                (obj as IGH_StateAwareObject).LoadState(m_state);
            }
        }
    }
}