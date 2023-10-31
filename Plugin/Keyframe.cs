using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Newtonsoft.Json;
using Rhino.Display;

namespace GH_Timeline
{
    /// <summary>
    /// Base keyframe class.  Contains time and easing information.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class Keyframe
    {
        [JsonProperty("ease_in")]
        public Easing EaseIn { get; set; } = Easing.Linear;
        [JsonProperty("ease_out")]
        public Easing EaseOut { get; set; } = Easing.Linear;
        [JsonProperty("time")]
        public double Time { get; set; }

        public Keyframe(double time)
        {
            Time = time;
        }
    }

    /// <summary>
    /// A keyframe that is related to a specific IGH_DocumentObject
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class ComponentKeyframe : Keyframe
    {
        protected ComponentKeyframe(double time) : base(time) { }
        public abstract int LoadState(IGH_DocumentObject obj);
        public virtual int InterpolateState(IGH_DocumentObject obj, ComponentKeyframe other, double amount)
        {
            return LoadState(obj);
        }
        public abstract int SaveState(IGH_DocumentObject obj);
    }

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

    /// <summary>
    /// A component keyframe for objects that implement <see cref="IGH_StateAwareObject"/>
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class StateAwareKeyframe : ComponentKeyframe
    {
        [JsonProperty("state")]
        private string m_state;

        public StateAwareKeyframe(double time) : base(time)
        {
        }

        public override int SaveState(IGH_DocumentObject obj)
        {
            m_state = (obj as IGH_StateAwareObject).SaveState();
            return m_state.GetHashCode();
        }

        public override int LoadState(IGH_DocumentObject obj)
        {
            (obj as IGH_StateAwareObject).LoadState(m_state);
            return m_state.GetHashCode();
        }
    }

    /// <summary>
    /// A component keyframe for number sliders, permitting interpolation.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class NumberSliderKeyframe : ComponentKeyframe
    {
        [JsonProperty("state")]
        private double m_state;

        public NumberSliderKeyframe(double time) : base(time) { }


        public override int InterpolateState(IGH_DocumentObject obj, ComponentKeyframe other, double interpolation)
        {
            NumberSliderKeyframe otherNs = other as NumberSliderKeyframe;
            double value = MathUtils.EaseInOut(interpolation, m_state, otherNs.m_state, EaseOut, other.EaseIn);
            (obj as GH_NumberSlider).SetSliderValue((decimal)value);
            return value.GetHashCode();

        }

        public override int LoadState(IGH_DocumentObject obj)
        {
            ((GH_NumberSlider)obj).SetSliderValue((decimal)m_state);
            return m_state.GetHashCode();
        }

        public override int SaveState(IGH_DocumentObject obj)
        {
            m_state = (double)((GH_NumberSlider)obj).CurrentValue;
            return m_state.GetHashCode();
        }
    }
}
