using Newtonsoft.Json;

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
}
