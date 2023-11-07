using Grasshopper.Kernel;
using Newtonsoft.Json;
using Rhino.Display;
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

        /// <summary>
        /// The name of this sequence
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// A number determining how this sequences should be sorted in the GUI.
        /// </summary>
        public abstract float Sort { get; }

        /// <summary>
        /// If the sequence is invalid, this returns a string that describes why. 
        /// </summary>
        public virtual string IsValidWhyNot => null;

        /// <summary>
        /// The number of keyframes in this sequence
        /// </summary>
        public int KeyframeCount => keyframes.Count;

        /// <summary>
        /// Retrieves the keyframes for this sequence, ordered by time.
        /// </summary>
        public IEnumerable<Keyframe> Keyframes => keyframes.OrderBy(x => x.Time);

        /// <summary>
        /// Adds a keyframe. If another keyframe exists with the same time, it will be deleted.
        /// </summary>
        /// <param name="keyframe">The keyframe to add</param>
        public void AddKeyframe(Keyframe keyframe)
        {
            _ = keyframes.RemoveWhere(x => x.Time == keyframe.Time);
            _ = keyframes.Add(keyframe);
        }

        /// <summary>
        /// Removes a keyframe
        /// </summary>
        /// <param name="keyframe">The keyframe to remove</param>
        /// <returns>True if the keyframe was removed</returns>
        public bool Remove(Keyframe keyframe)
        {
            return keyframes.Remove(keyframe);
        }

        /// <summary>
        /// Instructs the sequence to apply a given time and perform state updates.
        /// </summary>
        /// <param name="time">The time to apply</param>
        /// <param name="doc">The current Rhino document</param>
        /// <param name="viewport">The current Rhino viewport</param>
        /// <returns>True if a change was made</returns>
        public abstract bool SetTime(double time, GH_Document doc, RhinoViewport viewport);
    }
}
