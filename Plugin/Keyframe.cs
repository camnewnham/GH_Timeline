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
    }

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

    public class CameraKeyframe : Keyframe
    {
        public CameraKeyframe(double time) : base(time) { }
    }

    [Serializable]
    public class StateAwareKeyframe : ComponentKeyframe
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
    public class NumberSliderKeyframe : ComponentKeyframe
    {
        private double m_state;

        public NumberSliderKeyframe(double time) : base(time) { }


        public override int InterpolateState(IGH_DocumentObject obj, ComponentKeyframe other, double interpolation)
        {
            NumberSliderKeyframe otherNs = other as NumberSliderKeyframe;
            double value = MathUtils.Remap(MathUtils.EaseInOut(interpolation, Easing.Linear, Easing.Linear), 0, 1, m_state, otherNs.m_state);
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
