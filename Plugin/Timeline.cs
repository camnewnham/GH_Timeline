using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino.Display;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Plugin
{
    public class Timeline
    {
        internal static Guid MainCameraSequenceId => new Guid("{4B74BBB9-1713-4206-9124-68FD901FA036}");

        public Dictionary<Guid, Sequence> Sequences { get; private set; } = new Dictionary<Guid, Sequence>();

        public int SequenceCount => Sequences.Count;
        public int KeyframeCount => Sequences.Values.Sum(x => x.KeyframeCount);

        public void SetTime(double time, GH_Document doc, RhinoViewport viewport)
        {
            foreach (Sequence sq in Sequences.Values)
            {
                _ = sq.SetTime(time, doc, viewport);
            }
        }

        public bool RemoveSequence(Guid guid)
        {
            return Sequences.Remove(guid);
        }
        public bool RemoveSequence(Sequence sequence)
        {
            Guid id = Guid.Empty;
            foreach (KeyValuePair<Guid, Sequence> kvp in Sequences)
            {
                if (kvp.Value == sequence)
                {
                    id = kvp.Key;
                    break;
                }
            }
            return id != Guid.Empty && Sequences.Remove(id);
        }

        public bool TryGetSequence<T>(Guid guid, out T sequence) where T : Sequence
        {
            if (Sequences.TryGetValue(guid, out Sequence rawSeq) && rawSeq is T typedSeq)
            {
                sequence = typedSeq;
                return true;
            };
            sequence = null;
            return false;
        }

        public bool ContainsSequence(Guid guid)
        {
            return Sequences.ContainsKey(guid);
        }

        public T EnsureSequence<T>(Guid guid, Func<T> instantiator) where T : Sequence
        {
            if (!TryGetSequence(guid, out T sequence))
            {
                Sequences[guid] = sequence = instantiator();
            }
            return sequence;
        }

        public void OnTimeChanged(double time, GH_Document doc, RhinoViewport viewport)
        {
            foreach (Sequence seq in Sequences.Values)
            {
                _ = seq.SetTime(time, doc, viewport);
            }
        }

        public bool TryAddKeyframe(IGH_DocumentObject obj, double time)
        {
            switch (obj)
            {
                case TimelineComponent _:
                    return false;
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

        public void AddKeyframe(CameraState cameraState, double time)
        {
            CameraKeyframe kf = new CameraKeyframe(time);
            kf.SaveState(cameraState);
            EnsureSequence(Timeline.MainCameraSequenceId, () => new CameraSequence()).AddKeyframe(kf);
        }

        internal void AddedToDocument(GH_Document document)
        {
            foreach (Sequence sq in Sequences.Values)
            {
                if (sq is ComponentSequence cs)
                {
                    cs.Document = document;
                }
            }
        }

        internal void AddSequence(Guid value, ComponentSequence cseq)
        {
            Sequences.Add(value, cseq);
        }
    }
}
