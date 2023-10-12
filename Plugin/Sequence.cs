using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plugin
{

    public abstract class Sequence
    {
        public abstract string Name { get; }
        public int KeyframeCount => keyframes.Count;
        private readonly HashSet<Keyframe> keyframes = new HashSet<Keyframe>();

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
            keyframes.RemoveWhere(x => x.Time == keyframe.Time);
            keyframes.Add(keyframe);
        }

        public abstract bool SetTime(double time, GH_Document doc);
    }

    public class ComponentSequence : Sequence
    {
        public override string Name => m_ghObject?.GetName() ?? "Component";
        /// <summary>
        /// Hashset of the last state. The component corresponding to this sequence will
        /// only be expired if the hashcode changes.
        /// </summary>
        private int LastStateHashCode = -1;

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

        public ComponentSequence(Guid instanceGuid, string name)
        {
            InstanceGuid = instanceGuid;
            m_name = name;
        }

        /// <summary>
        /// Handles interpolation of keyframes in this sequence, expiring components as required
        /// </summary>
        /// <param name="time">The time to set</param>
        /// <param name="doc">The document to apply the time change to</param>
        /// <returns>True if setting the time resulted in a change and expired the component (that should prompt component and document expiry)</returns>
        public override bool SetTime(double time, GH_Document doc)
        {
            int oldHash = LastStateHashCode;
            for (int i = 0; i < OrderedKeyframes.Count; i++)
            {
                ComponentKeyframe current = OrderedKeyframes[i] as ComponentKeyframe;
                ComponentKeyframe next = i < OrderedKeyframes.Count - 1 ? OrderedKeyframes[i + 1] as ComponentKeyframe : null;

                if (next == null // Past last frame or only one keyframe
                    || (i == 0 && current.Time >= time))   // On or before first frame
                {
                    LastStateHashCode = current.LoadState(GetDocumentObject(doc));
                }
                else if (time >= current.Time && time < next.Time) // Between this frame and next 
                {
                    double fraction = MathUtils.Remap(time, current.Time, next.Time, 0, 1);
                    current.InterpolateState(GetDocumentObject(doc), next, fraction);
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
