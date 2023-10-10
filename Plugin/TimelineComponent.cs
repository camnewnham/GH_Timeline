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

        public bool Recording { get; set; }

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
            document.ObjectsDeleted += OnDocumentObjectsDeleted;
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
                Sequences.Remove(obj.InstanceGuid);
            }
        }


        /// <summary>
        /// Cache of IGH_DocumentObject that expired during the last solution.
        /// </summary>
        private readonly HashSet<IGH_DocumentObject> m_expiredObjects = new HashSet<IGH_DocumentObject>();

        public readonly Dictionary<Guid, Sequence> Sequences = new Dictionary<Guid, Sequence>();

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
                if (docObj is IGH_StateAwareObject stateAwareObj)
                {
                    AddKeyframe(stateAwareObj, (double)CurrentValue);
                }
                // TODO: Handle custom objects (i.e. sliders)
            }
        }

        private void AddKeyframe(IGH_StateAwareObject stateAwareObj, double time)
        {
            IGH_DocumentObject docObj = stateAwareObj as IGH_DocumentObject;
            StateAwareKeyframe keyframe = new StateAwareKeyframe(time);
            keyframe.SaveState(docObj);
            Guid id = docObj.InstanceGuid;
            EnsureSequence(id).AddKeyframe(keyframe);
        }

        private Sequence EnsureSequence(Guid instanceGuid)
        {
            if (!(Sequences.TryGetValue(instanceGuid, out Sequence sequence)))
            {
                Sequences[instanceGuid] = sequence = new Sequence(instanceGuid);
            }
            return sequence;
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
                m_expiredObjects.Add(obj);
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
                VolatileData.AllData(true).FirstOrDefault().CastTo<double>(out double time);

                foreach (Sequence seq in Sequences.Values)
                {
                    seq.SetTime(time, OnPingDocument());
                }
            }
        }

        public void InstanceGuidsChanged(SortedDictionary<Guid, Guid> map)
        {
            foreach (KeyValuePair<Guid, Guid> kvp in map)
            {
                if (Sequences.TryGetValue(kvp.Key, out Sequence found))
                {
                    found.InstanceGuid = kvp.Value;
                }
            }
        }

        public override bool Read(GH_IReader reader)
        {
            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            return base.Write(writer);
        }

        public class Sequence
        {
            private readonly HashSet<Keyframe> keyframes = new HashSet<Keyframe>();

            private List<Keyframe> orderedKeyframes;

            private IGH_DocumentObject m_ghObject;

            private Guid m_instanceGuid;
            public Guid InstanceGuid
            {
                get => m_instanceGuid;
                set
                {
                    if (value != m_instanceGuid)
                    {
                        m_instanceGuid = value;
                        m_ghObject = null;
                    }
                }
            }

            public IEnumerable<Keyframe> Keyframes => keyframes;

            public List<Keyframe> OrderedKeyframes
            {
                get
                {
                    if (orderedKeyframes == null)
                    {
                        orderedKeyframes = keyframes.OrderBy(x => x.Time).ToList();
                    }
                    return orderedKeyframes;
                }
            }

            public IGH_DocumentObject GetDocumentObject(GH_Document doc)
            {
                if (m_ghObject == null)
                {
                    m_ghObject = doc.FindObject(InstanceGuid, true);

                    if (m_ghObject == null)
                    {
                        throw new KeyNotFoundException($"Unable to find document object to restore state ({InstanceGuid})");
                    }
                }
                return m_ghObject;
            }

            public Sequence(Guid instanceGuid)
            {
                InstanceGuid = instanceGuid;
            }

            public bool IsEmpty => keyframes.Count == 0;

            /// <summary>
            /// Hashset of the last state. The component corresponding to this sequence will
            /// only be expired if the hashcode changes.
            /// </summary>
            private int LastStateHashCode = -1;

            public void AddKeyframe(Keyframe keyframe)
            {
                orderedKeyframes = null;
                keyframes.RemoveWhere(x => x.Time == keyframe.Time);
                keyframes.Add(keyframe);
            }

            /// <summary>
            /// Handles interpolation of keyframes in this sequence, expiring components as required
            /// </summary>
            /// <param name="time">The time to set</param>
            /// <param name="doc">The document to apply the time change to</param>
            /// <returns>True if setting the time resulted in a change and expired the component (that should prompt component and document expiry)</returns>
            public bool SetTime(double time, GH_Document doc)
            {
                int oldHash = LastStateHashCode;
                for (int i = 0; i < OrderedKeyframes.Count; i++)
                {
                    Keyframe previous = i > 0 ? OrderedKeyframes[i - 1] : null;
                    Keyframe current = OrderedKeyframes[i];
                    Keyframe next = i < OrderedKeyframes.Count - 1 ? OrderedKeyframes[i + 1] : null;

                    if (next == null)  // Past last frame
                    {
                        LastStateHashCode = current.LoadState(GetDocumentObject(doc));
                        break;
                    }
                    else if (previous == null && next == null) // Only one keyframe
                    {
                        LastStateHashCode = current.LoadState(GetDocumentObject(doc));
                        break;
                    }
                    else if (previous == null && current.Time >= time)  // On or before first frame
                    {
                        LastStateHashCode = current.LoadState(GetDocumentObject(doc));
                        break;
                    }
                    else if (time >= current.Time && time < next.Time) // Between this frame and next 
                    {
                        // TODO interpolate state here.
                        LastStateHashCode = current.LoadState(GetDocumentObject(doc));
                        break;
                    }
                }

                if (oldHash != LastStateHashCode)
                {
                    IGH_DocumentObject obj = GetDocumentObject(doc);
                    if (obj is IGH_ActiveObject activeObj && activeObj.Phase != GH_SolutionPhase.Blank)
                    {
                        obj.ExpireSolution(false);
                    }
                }

                return oldHash != LastStateHashCode;
            }
        }


        [Serializable]
        public abstract class Keyframe
        {
            public double Time { get; set; }

            public Keyframe(double time)
            {
                Time = time;
            }

            public abstract int LoadState(IGH_DocumentObject obj);
            public abstract int SaveState(IGH_DocumentObject obj);

        }

        [Serializable]
        public class StateAwareKeyframe : Keyframe
        {
            private string m_state;

            public StateAwareKeyframe(double time) : base(time)
            {
            }

            public override int SaveState(IGH_DocumentObject obj)
            {
                m_state = (obj as IGH_StateAwareObject).SaveState();
                return m_state.GetHashCode();
            }

            public override int LoadState(IGH_DocumentObject obj)
            {
                (obj as IGH_StateAwareObject).LoadState(m_state);
                return m_state.GetHashCode();
            }
        }
    }
}