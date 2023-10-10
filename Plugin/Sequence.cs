using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plugin
{

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
}
