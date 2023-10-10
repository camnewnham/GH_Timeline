using Grasshopper.Kernel;
using System;
using System.Collections.Generic;

namespace Plugin
{
    public class Timeline
    {
        public Dictionary<Guid, Sequence> Sequences { get; private set; } = new Dictionary<Guid, Sequence>();

        public void SetTime(double time, GH_Document doc)
        {
            foreach (Sequence sq in Sequences.Values)
            {
                sq.SetTime(time, doc);
            }
        }

        public bool RemoveSequence(Guid instanceGuid)
        {
            return Sequences.Remove(instanceGuid);
        }

        public bool TryGetSequence(Guid instanceGuid, out Sequence sequence)
        {
            return Sequences.TryGetValue(instanceGuid, out sequence);
        }

        public Sequence EnsureSequence(Guid instanceGuid)
        {
            if (!TryGetSequence(instanceGuid, out Sequence sequence))
            {
                Sequences[instanceGuid] = sequence = new Sequence(instanceGuid);
            }
            return sequence;
        }

        public void OnTimeChanged(double time, GH_Document doc)
        {
            foreach (Sequence seq in Sequences.Values)
            {
                seq.SetTime(time, doc);
            }
        }

        public bool TryAddKeyframe(IGH_DocumentObject obj, double time)
        {
            if (obj is IGH_StateAwareObject stateAwareObj)
            {
                AddKeyframe(stateAwareObj, time);
                return true;
            }
            return false;
        }

        public void AddKeyframe(IGH_StateAwareObject stateAwareObj, double time)
        {
            IGH_DocumentObject docObj = stateAwareObj as IGH_DocumentObject;
            StateAwareKeyframe keyframe = new StateAwareKeyframe(time);
            keyframe.SaveState(docObj);
            Guid id = docObj.InstanceGuid;
            EnsureSequence(id).AddKeyframe(keyframe);
        }
    }
}
