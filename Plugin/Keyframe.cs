using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;

namespace Plugin
{
    [Serializable]
    public abstract class Keyframe
    {
        public double Time { get; set; }

        public Keyframe(double time)
        {
            Time = time;
        }

        public abstract int LoadState(IGH_DocumentObject obj);
        public abstract int SaveState(IGH_DocumentObject obj);

    }

    [Serializable]
    public class StateAwareKeyframe : Keyframe
    {
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

    [Serializable]
    public abstract class InterpolatableKeyframe : Keyframe
    {
        protected InterpolatableKeyframe(double time) : base(time) { }

        public abstract int InterpolateState(IGH_DocumentObject obj, InterpolatableKeyframe other, double amount);
    }

    [Serializable]
    public class NumberSliderKeyframe : InterpolatableKeyframe
    {
        private double m_state;

        public NumberSliderKeyframe(double time) : base(time) { }


        public override int InterpolateState(IGH_DocumentObject obj, InterpolatableKeyframe other, double interpolation)
        {
            double value = m_state * (1 - interpolation) + ((NumberSliderKeyframe)other).m_state * interpolation;
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
