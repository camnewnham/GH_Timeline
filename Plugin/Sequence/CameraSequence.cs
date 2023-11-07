using Grasshopper.Kernel;
using Newtonsoft.Json;
using Rhino.Display;
using System.Collections.Generic;
using System.Linq;

namespace GH_Timeline
{
    /// <summary>
    /// A sequence of keyframes that stores camera states.  
    /// Used to control the Rhino viewport.
    /// Typically only one exists per timeline with the guid <see cref="Timeline.MainCameraSequenceId"/>
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class CameraSequence : Sequence
    {
        /// <inheritdoc/>
        public override float Sort => -10000;


        /// <inheritdoc/>
        public override string Name => "Camera";


        /// <inheritdoc/>
        public override bool SetTime(double time, GH_Document doc, RhinoViewport viewport)
        {
            List<Keyframe> kfs = Keyframes.ToList();
            for (int i = 0; i < kfs.Count; i++)
            {
                CameraKeyframe current = kfs[i] as CameraKeyframe;
                CameraKeyframe next = i < kfs.Count - 1 ? kfs[i + 1] as CameraKeyframe : null;

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
}
