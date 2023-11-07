using Grasshopper.Kernel;
using Newtonsoft.Json;

namespace GH_Timeline
{
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
}
