using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plugin
{
    public class Timeline
    {
        internal static Guid MainCameraSequenceId => Guid.Empty;

        public Dictionary<Guid, Sequence> Sequences { get; private set; } = new Dictionary<Guid, Sequence>();

        public int SequenceCount => Sequences.Count;
        public int KeyframeCount => Sequences.Values.Sum(x => x.KeyframeCount);

        public void SetTime(double time, GH_Document doc)
        {
            foreach (Sequence sq in Sequences.Values)
            {
                _ = sq.SetTime(time, doc);
            }
        }

        public bool RemoveSequence(Guid guid)
        {
            return Sequences.Remove(guid);
        }

        public bool TryGetSequence(Guid guid, out Sequence sequence)
        {
            return Sequences.TryGetValue(guid, out sequence);
        }

        public Sequence EnsureSequence(Guid guid, Func<Sequence> instantiator)
        {
            if (!TryGetSequence(guid, out Sequence sequence))
            {
                Sequences[guid] = sequence = instantiator();
            }
            return sequence;
        }

        public void OnTimeChanged(double time, GH_Document doc)
        {
            foreach (Sequence seq in Sequences.Values)
            {
                _ = seq.SetTime(time, doc);
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
            _ = keyframe.SaveState(docObj);
            EnsureSequence(docObj.InstanceGuid, () => new ComponentSequence(docObj.InstanceGuid, docObj.OnPingDocument())).AddKeyframe(keyframe);
        }

        public void AddKeyframe(GH_NumberSlider numberSlider, double time)
        {
            NumberSliderKeyframe keyframe = new NumberSliderKeyframe(time);
            _ = keyframe.SaveState(numberSlider);
            EnsureSequence(numberSlider.InstanceGuid, () => new ComponentSequence(numberSlider.InstanceGuid, numberSlider.OnPingDocument())).AddKeyframe(keyframe);

        }
    }
}
