using Grasshopper.Kernel;
using Newtonsoft.Json;
using Rhino.Display;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GH_Timeline
{
    /// <summary>
    /// Base class for a sequence of keyframes.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class Sequence
    {
        [JsonProperty("keyframes")]
        private readonly HashSet<Keyframe> keyframes = new HashSet<Keyframe>();
        public abstract string Name { get; }
        public abstract float Sort { get; }
        public virtual string IsValidWhyNot => null;


        public int KeyframeCount => keyframes.Count;

        private List<Keyframe> orderedKeyframes;

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

        public bool IsEmpty => keyframes.Count == 0;

        public void AddKeyframe(Keyframe keyframe)
        {
            orderedKeyframes = null;
            _ = keyframes.RemoveWhere(x => x.Time == keyframe.Time);
            _ = keyframes.Add(keyframe);
        }

        public bool Remove(Keyframe keyframe)
        {
            orderedKeyframes = null;
            return keyframes.Remove(keyframe);
        }

        public bool Remove(double keyframeTime)
        {
            orderedKeyframes = null;
            return keyframes.RemoveWhere(x => x.Time == keyframeTime) > 0;
        }

        /// <summary>
        /// Called when a keyframe changes that could influence how this component applies itself. 
        /// Typically called when a keyframe changes but is not added or removed.
        /// </summary>
        public void Invalidate()
        {
            orderedKeyframes = null;
        }

        public abstract bool SetTime(double time, GH_Document doc, RhinoViewport viewport);
    }

    /// <summary>
    /// A sequence of keyframes that stores camera states.  
    /// Used to control the Rhino viewport.
    /// Typically only one exists per timeline with the guid <see cref="Timeline.MainCameraSequenceId"/>
    /// </summary>
    public class CameraSequence : Sequence
    {
        public override float Sort => -10000;
        public override string Name => "Camera";

        public override bool SetTime(double time, GH_Document doc, RhinoViewport viewport)
        {
            for (int i = 0; i < OrderedKeyframes.Count; i++)
            {
                CameraKeyframe current = OrderedKeyframes[i] as CameraKeyframe;
                CameraKeyframe next = i < OrderedKeyframes.Count - 1 ? OrderedKeyframes[i + 1] as CameraKeyframe : null;

                if (next == null // Past last frame or only one keyframe
                    || (i == 0 && current.Time >= time))   // On or before first frame
                {
                    return current.LoadState(viewport);
                }
                else if (time >= current.Time && time < next.Time) // Between this frame and next 
                {
                    double fraction = MathUtils.Remap(time, current.Time, next.Time, 0, 1);
                    return current.InterpolateState(viewport, next, fraction);
                }
            }
            return false;
        }
    }

    /// <summary>
    /// A sequence of keyframes that stores component keyframes
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ComponentSequence : Sequence
    {
        [JsonProperty("id")]
        private Guid m_instanceGuid;

        public override string IsValidWhyNot => TryGetDocumentObject(out _) ? null : $"Component not found with InstanceGuid {m_instanceGuid}";

        public override float Sort => (m_ghObject?.Attributes.Pivot.Y ?? 0) + 10000;

        private GH_Document m_document;
        public GH_Document Document
        {
            get => m_document;
            internal set
            {
                if (value != m_document)
                {
                    m_document = value;
                    m_ghObject = value?.FindObject(InstanceGuid, true);
                }
            }
        }

        public IGH_DocumentObject DocumentObject => TryGetDocumentObject(out IGH_DocumentObject found)
                    ? found : throw new KeyNotFoundException($"Unable to find document object ({InstanceGuid})");

        public bool TryGetDocumentObject(out IGH_DocumentObject obj)
        {
            if (m_ghObject != null)
            {
                obj = m_ghObject;
                return true;
            }
            m_ghObject = obj = Document?.FindObject(InstanceGuid, true);
            return obj != null;
        }

        public override string Name => m_ghObject?.GetName() ?? "Missing";
        /// <summary>
        /// Hashset of the last state. The component corresponding to this sequence will
        /// only be expired if the hashcode changes.
        /// </summary>
        private int m_lastStateHashCode = -1;

        private IGH_DocumentObject m_ghObject;

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

        public ComponentSequence(Guid instanceGuid, GH_Document doc)
        {
            InstanceGuid = instanceGuid;
            Document = doc;
        }

        /// <summary>
        /// Handles interpolation of keyframes in this sequence, expiring components as required
        /// </summary>
        /// <param name="time">The time to set</param>
        /// <param name="doc">The document to apply the time change to</param>
        /// <returns>True if setting the time resulted in a change and expired the component (that should prompt component and document expiry)</returns>
        public override bool SetTime(double time, GH_Document doc, RhinoViewport viewport)
        {
            Document = doc;

            if (IsValidWhyNot != null)
            {
                return false;
            }

            int oldHash = m_lastStateHashCode;
            for (int i = 0; i < OrderedKeyframes.Count; i++)
            {
                ComponentKeyframe current = OrderedKeyframes[i] as ComponentKeyframe;
                ComponentKeyframe next = i < OrderedKeyframes.Count - 1 ? OrderedKeyframes[i + 1] as ComponentKeyframe : null;

                if (next == null // Past last frame or only one keyframe
                    || (i == 0 && current.Time >= time))   // On or before first frame
                {
                    m_lastStateHashCode = current.LoadState(DocumentObject);
                    break;
                }
                else if (time >= current.Time && time < next.Time) // Between this frame and next 
                {
                    double fraction = MathUtils.Remap(time, current.Time, next.Time, 0, 1);
                    m_lastStateHashCode = current.InterpolateState(DocumentObject, next, fraction);
                    break;
                }
            }

            if (oldHash != m_lastStateHashCode)
            {
                if (DocumentObject is IGH_ActiveObject activeObj && activeObj.Phase != GH_SolutionPhase.Blank)
                {
                    DocumentObject.ExpireSolution(false);
                }
            }

            return oldHash != m_lastStateHashCode;
        }
    }
}
