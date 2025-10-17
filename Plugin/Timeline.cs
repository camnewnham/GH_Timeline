using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Newtonsoft.Json;
using Rhino.Display;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GH_Timeline
{
    /// <summary>
    /// Container for a collection of sequences. This object is serialized to store state.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Timeline
    {
        /// <summary>
        /// GUID of the main camera sequence. 
        /// </summary>
        internal static Guid MainCameraSequenceId => new Guid("{4B74BBB9-1713-4206-9124-68FD901FA036}");

        /// <summary>
        /// The sequences in the timeline.
        /// </summary>
        [JsonProperty("sequences")]
        public Dictionary<Guid, Sequence> Sequences { get; private set; } = new Dictionary<Guid, Sequence>();

        [JsonProperty("frameRate")]
        public float FrameRate = 30;

        [JsonProperty("frameCount")]
        public int FrameCount = 100;

        /// <summary>
        /// The number of sequences in this timeline.
        /// </summary>
        public int SequenceCount => Sequences.Count;
        /// <summary>
        /// The total number of keyframes in all sequences in this timeline.
        /// </summary>
        public int KeyframeCount => Sequences.Values.Sum(x => x.KeyframeCount);

        /// <summary>
        /// Removes an entire sequence from the timeline.
        /// </summary>
        /// <param name="guid">The Id of the sequence</param>
        /// <returns>True if it was successfully removed</returns>
        public bool RemoveSequence(Guid guid)
        {
            return Sequences.Remove(guid);
        }
        /// <summary>
        /// Removes a sequence from the timeline.
        /// </summary>
        /// <param name="sequence">The sequence to remove</param>
        /// <returns>True if the sequence was found and removed.</returns>
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

        /// <summary>
        /// Retrieves a sequence from the timeline by Id
        /// </summary>
        /// <typeparam name="T">The type of sequence</typeparam>
        /// <param name="guid">The Id of the sequence</param>
        /// <param name="sequence">The sequence, or null</param>
        /// <returns>True if the sequence was found</returns>
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

        /// <summary>
        /// Checks if a sequence exists in the timeline
        /// </summary>
        /// <param name="guid">The Id of the timeline</param>
        /// <returns>True if a sequence exists in the timeline with a given Id</returns>
        public bool ContainsSequence(Guid guid)
        {
            return Sequences.ContainsKey(guid);
        }

        /// <summary>
        /// Retrieves or creates a sequence in the timeline.
        /// </summary>
        /// <typeparam name="T">The type of sequence</typeparam>
        /// <param name="guid">The Id of the sequence</param>
        /// <param name="instantiator">A function that instantiates the sequence if it does not exist</param>
        /// <returns>The found or created sequence</returns>
        public T EnsureSequence<T>(Guid guid, Func<T> instantiator) where T : Sequence
        {
            if (!TryGetSequence(guid, out T sequence))
            {
                Sequences[guid] = sequence = instantiator();
            }
            return sequence;
        }

        /// <summary>
        /// Called when the time is changed.
        /// </summary>
        /// <param name="time">The new time</param>
        /// <param name="doc">The current grasshopper document</param>
        /// <param name="viewport">The current rhino viewport</param>
        public void OnTimeChanged(double time, GH_Document doc, RhinoViewport viewport)
        {
            foreach (Sequence seq in Sequences.Values)
            {
                _ = seq.SetTime(time, doc, viewport);
            }
        }

        /// <summary>
        /// Attempts to add a keyframe for a document object. 
        /// </summary>
        /// <param name="obj">The object to add</param>
        /// <param name="time">The time to add the keyframe</param>
        /// <returns>True if the component type is supported and the keyframe was added</returns>
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

        /// <summary>
        /// Adds a keyframe for a <see cref="IGH_StateAwareObject"/>
        /// </summary>
        /// <param name="stateAwareObj">The object to add a keyframe for</param>
        /// <param name="time">The time to add the keyframe</param>
        public void AddKeyframe(IGH_StateAwareObject stateAwareObj, double time)
        {
            IGH_DocumentObject docObj = stateAwareObj as IGH_DocumentObject;
            StateAwareKeyframe keyframe = new StateAwareKeyframe(time);
            _ = keyframe.SaveState(docObj);
            EnsureSequence(docObj.InstanceGuid, () => new ComponentSequence(docObj.InstanceGuid, docObj.OnPingDocument())).AddKeyframe(keyframe);
        }

        /// <summary>
        /// Adds a keyframe for a <see cref="GH_NumberSlider"/>
        /// </summary>
        /// <param name="numberSlider">The object to add a keyframe for</param>
        /// <param name="time">The time to add the keyframe</param>
        public void AddKeyframe(GH_NumberSlider numberSlider, double time)
        {
            NumberSliderKeyframe keyframe = new NumberSliderKeyframe(time);
            _ = keyframe.SaveState(numberSlider);
            EnsureSequence(numberSlider.InstanceGuid, () => new ComponentSequence(numberSlider.InstanceGuid, numberSlider.OnPingDocument())).AddKeyframe(keyframe);
        }

        /// <summary>
        /// Adds a keyframe for a a camera state
        /// </summary>
        /// <param name="cameraState">The camera state to add a keyframe for</param>
        /// <param name="time">The time to add the keyframe</param>
        public void AddKeyframe(CameraState cameraState, double time)
        {
            CameraKeyframe kf = new CameraKeyframe(time);
            kf.SaveState(cameraState);
            EnsureSequence(Timeline.MainCameraSequenceId, () => new CameraSequence()).AddKeyframe(kf);
        }

        /// <summary>
        /// Called when the timeline is assigned a document. Ensures <see cref="ComponentSequence"/> get assigned the correct document.
        /// </summary>
        /// <param name="document">The grasshopper document.</param>
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

        /// <summary>
        /// Called to respond to <see cref="IGH_InstanceGuidDependent.InstanceGuidsChanged(SortedDictionary{Guid, Guid})"/>
        /// </summary>
        /// <param name="oldGuid">The old component Id</param>
        /// <param name="newGuid">The new component Id</param>
        internal bool OnSequenceIdChanged(Guid oldGuid, Guid newGuid)
        {
            if (TryGetSequence(oldGuid, out Sequence found) && found is ComponentSequence cseq)
            {
                Sequences.Remove(oldGuid);
                cseq.InstanceGuid = newGuid;
                Sequences.Add(newGuid, cseq);
                return true;
            }
            return false;
        }
    }
}
