using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;

namespace GH_Timeline
{
    /// <summary>
    /// Utility class to serialize and deserialize timelines.  
    /// Includes "$type"="ClassName" to allow deserialization of abstract classes.
    /// </summary>
    public static class Serialization
    {
        /// <summary>
        /// Serialization configuration to use when serializing and deserializing timelines.
        /// </summary>
        private static JsonSerializerSettings SerializationSettings => new JsonSerializerSettings()
        {
            SerializationBinder = new SimpleBinder(),
            TypeNameHandling = TypeNameHandling.Auto,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new[] { new StringEnumConverter() }
        };

        /// <summary>
        /// Serializes a timeline to Json
        /// </summary>
        /// <param name="timeline">The timeline to serialize</param>
        /// <returns>A JSON representation of the timeline</returns>
        public static string Serialize(this Timeline timeline)
        {
            return JsonConvert.SerializeObject(timeline, SerializationSettings);
        }

        /// <summary>
        /// Deserializes a timeline from Json.
        /// </summary>
        /// <param name="timelineStr">The JSON representation</param>
        /// <returns>The deserialized timeline</returns>
        public static Timeline Deserialize(string timelineStr)
        {
            return JsonConvert.DeserializeObject<Timeline>(timelineStr, SerializationSettings);
        }


        /// <summary>
        /// Simple binder to serialize with short type names (classes) instead of full assembly and namespace paths
        /// </summary>
        private class SimpleBinder : ISerializationBinder
        {
            public Type BindToType(string assemblyName, string typeName)
            {
                return Type.GetType(nameof(GH_Timeline) + "." + typeName);
            }

            public void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                assemblyName = null;
                typeName = serializedType.Name;
            }
        }
    }
}
