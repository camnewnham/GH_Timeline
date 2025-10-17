using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GH_Timeline
{
    /// <summary>
    /// Primary component class. Inherits some utility from <see cref="GH_NumberSlider"/>
    /// </summary>
    public class TimelineComponent : GH_NumberSlider, IGH_InstanceGuidDependent
    {
        /// <inheritdoc/>
        public override Guid ComponentGuid => new Guid("84e977ef-b06d-41e3-aa6d-c6f0f646cef3");
        /// <inheritdoc/>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.logo_24;
        /// <inheritdoc/>
        public override GH_ParamKind Kind => GH_ParamKind.floating;
        /// <inheritdoc/>
        public override string InstanceDescription => $"Timeline ({(CurrentValue * 100).ToString("00.0")})\n{Timeline.SequenceCount} Sequences\n{Timeline.KeyframeCount} Keyframes";

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

        /// <inheritdoc />
        public override void CreateAttributes()
        {
            Attributes = new TimelineComponentAttributes(this);
        }

        /// <inheritdoc />
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

            _ = FormUtils.Menu_AppendNumericUpDown(menu, "Frames Per Second", (decimal)Timeline.FrameRate, 2, (obj, arg) => Timeline.FrameRate = (float)arg.Value, 1, 300);
            _ = FormUtils.Menu_AppendNumericUpDown(menu, "Frame Count", Timeline.FrameCount, 0, (obj, arg) => Timeline.FrameCount = (int)arg.Value, 0);
            _ = Menu_AppendSeparator(menu);

            _ = Menu_AppendItem(menu, Recording ? "Tracking..." : "Track Changes", (obj, arg) =>
            {
                Recording = !Recording;
                Instances.ActiveCanvas.Invalidate();
            }, true, Recording);

            _ = Menu_AppendItem(menu, "Export Animation...", async (obj, arg) =>
            {
                Recording = false;

                GH_SliderAnimator gH_SliderAnimator = new GH_SliderAnimator(this);
                gH_SliderAnimator.FrameCount = Timeline.FrameCount;
                if (gH_SliderAnimator.SetupAnimationProperties())
                {
                    Recording = false;
                    m_recordAnimationViewport = gH_SliderAnimator.Viewport;
                    int recordedFrameCount = gH_SliderAnimator.StartAnimation();
                    m_recordAnimationViewport = null;

                    int targetFrameCount = Instances.Settings.GetValue("SlAnim:FrameCount", int.MinValue);
                    string targetFolder = Instances.Settings.GetValue("SlAnim:Folder", "");
                    string targetTemplate = Instances.Settings.GetValue("SlAnim:FileTemplate", "");

                    int width = Instances.Settings.GetValue("SlAnim:Width", 640);
                    int height = Instances.Settings.GetValue("SlAnim:Height", 480);

                    if (recordedFrameCount >= targetFrameCount)
                    {
                        try
                        {
                            CancellationTokenSource cts = new CancellationTokenSource();
                            Task<string> ffmpegTask = FFmpegUtil.Compile(targetFolder, targetTemplate, targetFrameCount, Timeline.FrameRate, (int)(Math.Sqrt(width * height) / Math.Sqrt(1920 * 1080) * Bitrate1920x1080), cts.Token);

                            Stopwatch sw = new Stopwatch();
                            sw.Start();

                            while (!ffmpegTask.IsCompleted)
                            {
                                RhinoApp.Wait();
                                Application.DoEvents();
                                RhinoApp.CommandPrompt = $"Encoding video... " + sw.Elapsed;
                                if (GH_Document.IsEscapeKeyDown())
                                {
                                    cts.Cancel();
                                    throw new OperationCanceledException("Operation cancelled by escape key press.");
                                }
                            }

                            string videoPath = await ffmpegTask;

                            RhinoApp.WriteLine("Saved video: " + videoPath);

                            _ = Process.Start(new System.Diagnostics.ProcessStartInfo(videoPath)
                            {
                                UseShellExecute = true
                            });
                        }
                        catch (OperationCanceledException)
                        {
                            Rhino.RhinoApp.WriteLine("Cancelled.");
                        }
                        catch (Exception ex)
                        {
                            Rhino.UI.Dialogs.ShowMessage("Something went wrong while compiling the animation. If issues persist, please create an issue on github with the Rhino command line output.",
                                "Animation Error");
                            Rhino.RhinoApp.WriteLine(ex.ToString());
                        }
                        finally
                        {
                            RhinoApp.CommandPrompt = string.Empty;
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Display conduit to respond to camera changes and render recording notifications
        /// </summary>
        private CameraTracker m_cameraTracker;


        /// <inheritdoc />
        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            document.SolutionEnd += OnSolutionEndRecordState;
            document.SolutionStart += OnSolutionStartRecordState;
            document.ObjectsAdded += OnDocumentObjectsAdded;
            document.ObjectsDeleted += OnDocumentObjectsDeleted;

            if (!m_hasDeserialized)
            {
                OnFirstAddToDocument();
            }

            m_cameraTracker = new CameraTracker(this);
            m_cameraTracker.OnCameraStateChanged += OnCameraStateChange;

            Timeline.AddedToDocument(document);
        }

        /// <summary>
        /// Called when objects are added to the document. Used to ensure sequences are correctly linked to components.
        /// </summary>
        private void OnDocumentObjectsAdded(object sender, GH_DocObjectEventArgs e)
        {
            // In case of Undo, an object we are missing might have been re-added.
            ClearRuntimeMessages();
            ValidateComponents();
        }

        /// <summary>
        /// Called when the camera is changed via <see cref="CameraTracker"/>.  
        /// Responsible for filtering out invalid states (i.e. caused by manual slider updates or camera animations). 
        /// </summary>
        /// <param name="state">The camera state</param>
        private void OnCameraStateChange(CameraState state)
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
            Timeline.AddKeyframe(state, (double)CurrentValue);
            Attributes.ExpireLayout();
            Instances.ActiveCanvas.Invalidate();
        }

        /// <summary>
        /// Called when the component is added to the document for the first time i.e. not copy/paste or save/load.
        /// </summary>
        public void OnFirstAddToDocument()
        {
            AnimateCamera = true;
        }

        /// <inheritdoc />
        public override void RemovedFromDocument(GH_Document document)
        {
            m_cameraTracker?.Dispose();
            Recording = false;
            document.SolutionEnd -= OnSolutionEndRecordState;
            document.SolutionStart -= OnSolutionStartRecordState;
            document.ObjectsAdded -= OnDocumentObjectsAdded;
            document.ObjectsDeleted -= OnDocumentObjectsDeleted;
            base.RemovedFromDocument(document);
        }

        /// <inheritdoc />
        public override void MovedBetweenDocuments(GH_Document oldDocument, GH_Document newDocument)
        {
            RemovedFromDocument(oldDocument);
            AddedToDocument(newDocument);
        }

        /// <summary>
        /// Called when an object is removed from the document. Responsible for removing associated sequences.
        /// </summary>
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

        /// <summary>
        /// Called when the time is updated by the user / GUI
        /// </summary>
        /// <param name="newValue"></param>
        internal void OnTimelineHandleDragged(double newValue)
        {
            SetSliderValue((decimal)newValue);
            ExpireSolution(true);
        }

        /// <summary>
        /// Called when a keyframe has been updated in a way that would cause a change in the current state i.e. i.e. time, easing.
        /// </summary>
        internal void OnKeyframeChanged()
        {
            ExpireSolution(true);
        }

        /// <inheritdoc />
        public void InstanceGuidsChanged(SortedDictionary<Guid, Guid> map)
        {
            foreach (KeyValuePair<Guid, Guid> kvp in map)
            {
                Timeline.OnSequenceIdChanged(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Gets or sets whether there is a camerae sequence in the timeline.
        /// </summary>
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
                        RecordUndoEvent("Add camera sequence");
                        _ = Timeline.EnsureSequence(Timeline.MainCameraSequenceId, () => new CameraSequence());
                        Attributes.ExpireLayout();
                    }
                    else
                    {
                        RecordUndoEvent("Remove camera sequence");
                        _ = Timeline.RemoveSequence(Timeline.MainCameraSequenceId);
                        Attributes.ExpireLayout();
                    }
                }
            }
        }

        private bool m_recording = false;

        /// <summary>
        /// Gets or sets whether we are currently recording state changes.
        /// </summary>
        public bool Recording
        {
            get => m_recording;
            set
            {
                m_recording = value;
                Rhino.RhinoDoc.ActiveDoc?.Views.Redraw();
            }
        }


        #region Recording
        /// <summary>
        /// Cache of IGH_DocumentObject that expired during the last solution.
        /// </summary>
        private readonly HashSet<IGH_DocumentObject> m_expiredObjects = new HashSet<IGH_DocumentObject>();

        /// <summary>
        /// Caches whether this component was expired at the start of the last solution, and component recording should be skipped.
        /// </summary>
        private bool m_wasSliderValueChanged = false;
        /// <summary>
        /// Caches whether this component was expired at the start of the last solution, and camera recording should be skipped.
        /// </summary>
        private bool m_cameraSliderValueChanged = false;

        /// <summary>
        /// While recording, add keyframes for any compatible objects that expired in the last solution.
        /// </summary>
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

        /// <summary>
        /// While recording, track which components have expired to record their state.  
        /// Updates caused by manually changing the slider value are skipped.
        /// </summary>>
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

        /// <summary>
        /// <inheritdoc />
        /// When this component expires, we also expire any components that exist in the timeline sequences.
        /// </summary>
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

        /// <inheritdoc />
        protected override void OnVolatileDataCollected()
        {
            base.OnVolatileDataCollected();
            ValidateComponents();
        }

        /// <summary>
        /// Shows error messages to the user if something is wrong with their sequences (i.e. tracked component doesn't exist)
        /// </summary>
        private void ValidateComponents()
        {
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override bool Write(GH_IWriter writer)
        {
            writer.SetString("timeline", Serialization.Serialize(Timeline));

            return base.Write(writer);

        }

        #endregion
    }
}