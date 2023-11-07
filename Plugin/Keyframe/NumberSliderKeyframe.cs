using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Newtonsoft.Json;

namespace GH_Timeline
{
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
