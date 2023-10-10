using Grasshopper.Kernel;
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
}
