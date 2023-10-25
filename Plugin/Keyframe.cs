using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Newtonsoft.Json;
using Rhino.Display;

namespace Plugin
{

    [JsonObject(MemberSerialization.OptIn)]
    public abstract class Keyframe
    {
        [JsonProperty]
        public Easing EaseIn { get; set; } = Easing.Linear;
        [JsonProperty]
        public Easing EaseOut { get; set; } = Easing.Linear;
        [JsonProperty]
        public double Time { get; set; }

        public Keyframe(double time)
        {
            Time = time;
        }
    }

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

    [JsonObject(MemberSerialization.OptIn)]
    public class CameraKeyframe : Keyframe
    {
        [JsonProperty]
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

    [JsonObject(MemberSerialization.OptIn)]
    public class StateAwareKeyframe : ComponentKeyframe
    {
        [JsonProperty]
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

    [JsonObject(MemberSerialization.OptIn)]
    public class NumberSliderKeyframe : ComponentKeyframe
    {
        [JsonProperty]
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
