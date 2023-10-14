using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Plugin
{
    public class TimelineComponent : GH_NumberSlider, IGH_InstanceGuidDependent
    {
        public override Guid ComponentGuid => new Guid("84e977ef-b06d-41e3-aa6d-c6f0f646cef3");
        protected override System.Drawing.Bitmap Icon => null;
        public override GH_ParamKind Kind => GH_ParamKind.floating;

        private bool m_hasDeserialized = false;

        public override string InstanceDescription => $"Timeline\n{Timeline.SequenceCount} Sequences\n{Timeline.KeyframeCount} Keyframes";

        public Timeline Timeline;
        public TimelineComponent() : base()
        {
            Slider.DecimalPlaces = 8;
            Slider.Minimum = 0;
            Slider.Maximum = 1;
            NickName = "Time";
            Timeline = new Timeline();
        }

        public override string Name => "Timeline";
        public override string Description => "Displays keyframes for animating your definition.";
        public override string Category => "Display";
        public override string SubCategory => "Timeline";

        /// <summary>
        /// During recording to disk, this stores the viewport that is being recorded.
        /// </summary>
        private Rhino.Display.RhinoViewport m_recordAnimationViewport;

        public override void CreateAttributes()
        {
            Attributes = new TimelineComponentAttributes(this);
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            _ = Menu_AppendSeparator(menu);

            _ = Menu_AppendItem(menu, Recording ? "Recording..." : "Record", (obj, arg) =>
            {
                Recording = !Recording;
                Instances.ActiveCanvas.Invalidate();
            }, true, Recording);


            _ = Menu_AppendItem(menu, "Animate Camera", (obj, arg) =>
            {
                AnimateCamera = !AnimateCamera;
                Instances.ActiveCanvas.Invalidate();
            }, true, AnimateCamera);

            _ = Menu_AppendItem(menu, "Export Animation...", (obj, arg) =>
            {
                GH_SliderAnimator gH_SliderAnimator = new GH_SliderAnimator(this);
                if (gH_SliderAnimator.SetupAnimationProperties())
                {
                    Recording = false;
                    m_recordAnimationViewport = gH_SliderAnimator.Viewport;
                    _ = gH_SliderAnimator.StartAnimation();
                    m_recordAnimationViewport = null;
                }
            });
        }

        private CameraTracker m_cameraTracker;

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            document.SolutionEnd += OnSolutionEndRecordState;
            document.SolutionStart += OnSolutionStartRecordState;
            document.ObjectsDeleted += OnDocumentObjectsDeleted;

            if (!m_hasDeserialized)
            {
                OnFirstAddToDocument();
            }

            m_cameraTracker = new CameraTracker(this);
            m_cameraTracker.OnCameraStateChanged += OnCameraStateChange;
        }

        private void OnCameraStateChange(CameraState obj)
        {
            _ = OnPingDocument();

            if (!Recording || !AnimateCamera)
            {
                return;
            }

            if (m_cameraSliderValueChanged)
            {
                m_cameraSliderValueChanged = false;
                return;
            }

            Timeline.AddKeyframe(obj, (double)CurrentValue);
            Attributes.ExpireLayout();
            Instances.ActiveCanvas.Invalidate();
        }

        public void OnFirstAddToDocument()
        {
            // Default to enabling camera tracking
            AnimateCamera = true;
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            m_cameraTracker?.Dispose();
            Recording = false;
            document.SolutionEnd -= OnSolutionEndRecordState;
            document.SolutionStart -= OnSolutionStartRecordState;
            document.ObjectsDeleted -= OnDocumentObjectsDeleted;
            base.RemovedFromDocument(document);
        }

        private void OnDocumentObjectsDeleted(object sender, GH_DocObjectEventArgs e)
        {
            foreach (IGH_DocumentObject obj in e.Objects)
            {
                if (Timeline.RemoveSequence(obj.InstanceGuid))
                {
                    Attributes.ExpireLayout();
                }
            }
        }

        internal void OnTimelineHandleDragged(double newValue)
        {
            SetSliderValue((decimal)newValue);
            ExpireSolution(true);
        }

        internal void OnKeyframeChanged()
        {
            ExpireSolution(true);
        }

        public void InstanceGuidsChanged(SortedDictionary<Guid, Guid> map)
        {
            foreach (KeyValuePair<Guid, Guid> kvp in map)
            {
                if (Timeline.TryGetSequence(kvp.Key, out Sequence found) && found is ComponentSequence cseq)
                {
                    cseq.InstanceGuid = kvp.Value;
                }
            }
        }

        private bool m_animateCamera = false;
        public bool AnimateCamera
        {
            get => m_animateCamera;
            set
            {
                if (value != m_animateCamera)
                {
                    m_animateCamera = value;
                    if (m_animateCamera)
                    {
                        _ = Timeline.EnsureSequence(Timeline.MainCameraSequenceId, () => new CameraSequence());
                        Attributes.ExpireLayout();
                    }
                    else
                    {
                        _ = Timeline.RemoveSequence(Timeline.MainCameraSequenceId);
                        Attributes.ExpireLayout();
                    }
                }
            }
        }


        private bool m_recording = false;
        public bool Recording
        {
            get => m_recording;
            set
            {
                m_recording = value;
                Rhino.RhinoDoc.ActiveDoc.Views.Redraw();
            }
        }


        #region Recording
        /// <summary>
        /// Cache of IGH_DocumentObject that expired during the last solution.
        /// </summary>
        private readonly HashSet<IGH_DocumentObject> m_expiredObjects = new HashSet<IGH_DocumentObject>();

        private bool m_wasSliderValueChanged = false;
        private bool m_cameraSliderValueChanged = false;

        private void OnSolutionEndRecordState(object sender, GH_SolutionEventArgs e)
        {
            m_cameraSliderValueChanged = true;
            if (m_wasSliderValueChanged)
            {
                m_wasSliderValueChanged = false;
                return;
            }

            if (!Recording)
            {
                return;
            }

            foreach (IGH_DocumentObject docObj in m_expiredObjects)
            {
                if (Timeline.TryAddKeyframe(docObj, (double)CurrentValue))
                {
                    Attributes.ExpireLayout();
                }
            }
        }

        private void OnSolutionStartRecordState(object sender, GH_SolutionEventArgs e)
        {
            if (!Recording || m_wasSliderValueChanged)
            {
                return;
            }

            m_expiredObjects.Clear();

            foreach (IGH_DocumentObject obj in e.Document.Objects.Where(
                x => x is IGH_ActiveObject activeObj &&
                activeObj.Phase == GH_SolutionPhase.Blank
            ))
            {
                _ = m_expiredObjects.Add(obj);
            }
        }

        protected override void ExpireDownStreamObjects()
        {
            base.ExpireDownStreamObjects();
            if (OnPingDocument() == null)
            {
                return;
            }

            m_wasSliderValueChanged = true;
            if (Phase == GH_SolutionPhase.Blank)
            {
                CollectData();
                _ = VolatileData.AllData(true).FirstOrDefault().CastTo(out double time);
                Timeline.OnTimeChanged(time, OnPingDocument(), m_recordAnimationViewport ?? Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport);
            }
        }

        #endregion // Recording

        #region IO

        public override bool Read(GH_IReader reader)
        {
            m_hasDeserialized = true;
            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            return base.Write(writer);
        }

        #endregion
    }
}