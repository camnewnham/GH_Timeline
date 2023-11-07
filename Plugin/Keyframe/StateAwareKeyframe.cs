using Grasshopper.Kernel;
using Newtonsoft.Json;

namespace GH_Timeline
{

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
}
