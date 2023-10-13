using GH_IO.Serialization;
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
                Grasshopper.Instances.ActiveCanvas.Invalidate();
            }, true, Recording);


            _ = Menu_AppendItem(menu, "Animate Camera", (obj, arg) =>
            {
                AnimateCamera = !AnimateCamera;
                Grasshopper.Instances.ActiveCanvas.Invalidate();
            }, true, AnimateCamera);

            _ = Menu_AppendItem(menu, "Animate...", (obj, arg) =>
            {
                GH_SliderAnimator gH_SliderAnimator = new GH_SliderAnimator(this);
                if (gH_SliderAnimator.SetupAnimationProperties())
                {
                    Recording = false;
                    _ = gH_SliderAnimator.StartAnimation();
                }
            });
        }

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
        }

        public void OnFirstAddToDocument()
        {
            // Default to enabling camera tracking
            AnimateCamera = true;
        }

        public override void RemovedFromDocument(GH_Document document)
        {
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

        #region Camera

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

        #endregion

        #region Recording
        /// <summary>
        /// Cache of IGH_DocumentObject that expired during the last solution.
        /// </summary>
        private readonly HashSet<IGH_DocumentObject> m_expiredObjects = new HashSet<IGH_DocumentObject>();
        public bool Recording { get; set; }

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
                if (Timeline.TryAddKeyframe(docObj, (double)CurrentValue))
                {
                    Attributes.ExpireLayout();
                }
            }
        }

        private void OnSolutionStartRecordState(object sender, GH_SolutionEventArgs e)
        {
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

            m_wasExpirationInitiatedBySliderValue = true;
            if (Phase == GH_SolutionPhase.Blank)
            {
                CollectData();
                _ = VolatileData.AllData(true).FirstOrDefault().CastTo(out double time);
                Timeline.OnTimeChanged(time, OnPingDocument());
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