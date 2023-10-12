using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plugin
{
    public class Timeline
    {
        public Dictionary<Guid, Sequence> Sequences { get; private set; } = new Dictionary<Guid, Sequence>();

        public int SequenceCount => Sequences.Count;
        public int KeyframeCount => Sequences.Values.Sum(x => x.KeyframeCount);

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
                Sequences[instanceGuid] = sequence = new ComponentSequence(instanceGuid);
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
            switch (obj)
            {
                case GH_NumberSlider slider:
                    AddKeyframe(slider, time);
                    return true;
                case IGH_StateAwareObject stateAwareObject:
                    AddKeyframe(stateAwareObject, time);
                    return true;
                default:
                    return false;
            }
        }

        public void AddKeyframe(IGH_StateAwareObject stateAwareObj, double time)
        {
            IGH_DocumentObject docObj = stateAwareObj as IGH_DocumentObject;
            StateAwareKeyframe keyframe = new StateAwareKeyframe(time);
            keyframe.SaveState(docObj);
            EnsureSequence(docObj.InstanceGuid).AddKeyframe(keyframe);
        }

        public void AddKeyframe(GH_NumberSlider numberSlider, double time)
        {
            NumberSliderKeyframe keyframe = new NumberSliderKeyframe(time);
            keyframe.SaveState(numberSlider);
            EnsureSequence(numberSlider.InstanceGuid).AddKeyframe(keyframe);

        }
    }
}
