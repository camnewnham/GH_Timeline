using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace GH_Timeline
{
    public class TimelineComponent : GH_NumberSlider, IGH_InstanceGuidDependent
    {
        /// <inheritdoc/>
        public override Guid ComponentGuid => new Guid("84e977ef-b06d-41e3-aa6d-c6f0f646cef3");
        /// <inheritdoc/>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.logo_24;
        /// <inheritdoc/>
        public override GH_ParamKind Kind => GH_ParamKind.floating;
        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public override string Name => "Timeline";
        /// <inheritdoc/>
        public override string Description => "Displays keyframes for animating your definition.";
        /// <inheritdoc/>
        public override string Category => "Display";
        /// <inheritdoc/>
        public override string SubCategory => "Timeline";

        /// <summary>
        /// If false, this <see cref="Read(GH_IReader)"/> has not been called on this component. 
        /// This signifies that it is a "new" component rather than one loaded from file, paste, or redo.
        /// </summary>
        private bool m_hasDeserialized = false;
        /// <summary>
        /// During recording to disk, this stores the viewport that is being recorded.
        /// </summary>
        private Rhino.Display.RhinoViewport m_recordAnimationViewport;

        /// <summary>
        /// Bitrate to use with ffmpeg when video is 1920x1080 (x1000). Other dimensions are multiplied accordingly.
        /// </summary>
        public static int Bitrate1920x1080 = 16384;

        public override void CreateAttributes()
        {
            Attributes = new TimelineComponentAttributes(this);
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            _ = Menu_AppendSeparator(menu);

            if (!AnimateCamera)
            {
                _ = Menu_AppendItem(menu, "Add Camera", (obj, arg) =>
                {
                    AnimateCamera = !AnimateCamera;
                    Instances.ActiveCanvas.Invalidate();
                }, true, AnimateCamera);

                _ = Menu_AppendSeparator(menu);
            }

            _ = Menu_AppendItem(menu, Recording ? "Tracking..." : "Track Changes", (obj, arg) =>
            {
                Recording = !Recording;
                Instances.ActiveCanvas.Invalidate();
            }, true, Recording);

            _ = Menu_AppendItem(menu, "Export Animation...", (obj, arg) =>
            {
                Recording = false;
                GH_SliderAnimator gH_SliderAnimator = new GH_SliderAnimator(this);
                if (gH_SliderAnimator.SetupAnimationProperties())
                {
                    Recording = false;
                    m_recordAnimationViewport = gH_SliderAnimator.Viewport;
                    int recordedFrameCount = gH_SliderAnimator.StartAnimation();
                    m_recordAnimationViewport = null;

                    int targetFrameCount = Instances.Settings.GetValue("SlAnim:FrameCount", int.MinValue) + 1;
                    string targetFolder = Instances.Settings.GetValue("SlAnim:Folder", "");
                    string targetTemplate = Instances.Settings.GetValue("SlAnim:FileTemplate", "");

                    int width = Instances.Settings.GetValue("SlAnim:Width", 640);
                    int height = Instances.Settings.GetValue("SlAnim:Height", 480);

                    if (recordedFrameCount == targetFrameCount)
                    {
                        string videoPath = FFmpegUtil.Compile(targetFolder, targetTemplate, targetFrameCount, 30, (int)(Math.Sqrt(width * height) / Math.Sqrt(1920 * 1080) * Bitrate1920x1080));
                        if (videoPath != null)
                        {
                            _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(videoPath)
                            {
                                UseShellExecute = true
                            });
                        }
                    }
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

            Timeline.AddedToDocument(document);
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

            _ = RecordUndoEvent("Add camera keyframe");
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

        public override void MovedBetweenDocuments(GH_Document oldDocument, GH_Document newDocument)
        {
            RemovedFromDocument(oldDocument);
            AddedToDocument(newDocument);
        }

        private void OnDocumentObjectsDeleted(object sender, GH_DocObjectEventArgs e)
        {
            foreach (IGH_DocumentObject obj in e.Objects)
            {
                if (Timeline.ContainsSequence(obj.InstanceGuid))
                {
                    _ = RecordUndoEvent("Sequence removed by component deletion");
                    _ = e.Document.UndoUtil.MergeRecords(2);
                    _ = Timeline.RemoveSequence(obj.InstanceGuid);
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
                    _ = Timeline.RemoveSequence(kvp.Key);
                    cseq.InstanceGuid = kvp.Value;
                    Timeline.AddSequence(kvp.Value, cseq);
                }
            }
        }

        public bool AnimateCamera
        {
            get => Timeline.ContainsSequence(Timeline.MainCameraSequenceId);
            set
            {
                bool state = AnimateCamera;
                if (value != state)
                {
                    if (value)
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

        protected override void OnVolatileDataCollected()
        {
            base.OnVolatileDataCollected();

            foreach (Sequence seq in Timeline.Sequences.Values)
            {
                if (seq.IsValidWhyNot is string reason)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, reason);
                }
            }
        }

        #endregion // Recording

        #region IO

        public override bool Read(GH_IReader reader)
        {
            m_hasDeserialized = true;

            string serializedTimeline = null;
            if (reader.TryGetString("timeline", ref serializedTimeline))
            {
                Timeline = Serialization.Deserialize(serializedTimeline);

                // In case of re-do
                if (OnPingDocument() is GH_Document doc)
                {
                    Timeline.AddedToDocument(doc);
                }
            }
            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetString("timeline", Serialization.Serialize(Timeline));

            return base.Write(writer);

        }

        #endregion
    }
}