using Newtonsoft.Json;
using Rhino.Display;

namespace GH_Timeline
{
    /// <summary>
    /// A keyframe that stores a camera state
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class CameraKeyframe : Keyframe
    {
        [JsonProperty("state")]
        private CameraState m_state;
        public CameraKeyframe(double time) : base(time) { }

        public void SaveState(CameraState state)
        {
            m_state = state;
        }

        public bool LoadState(RhinoViewport viewport)
        {
            if (!m_state.Equals(new CameraState(viewport)))
            {
                m_state.ApplyToViewport(viewport);
                return true;
            }
            return false;
        }

        public bool InterpolateState(RhinoViewport viewport, CameraKeyframe other, double t)
        {
            CameraState tween = MathUtils.EaseInOut(t, m_state, other.m_state, EaseOut, other.EaseIn);

            if (!tween.Equals(new CameraState(viewport)))
            {
                tween.ApplyToViewport(viewport);
                return true;
            }
            return false;
        }
    }
}
