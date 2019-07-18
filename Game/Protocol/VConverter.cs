using System;
using Game.Types;
using Newtonsoft.Json;

namespace Game.Protocol
{
    public class VConverter : JsonConverter<V>
    {
        public override void WriteJson(JsonWriter writer, V value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, new[] {value.X, value.Y});
        }

        public override V ReadJson(JsonReader reader, Type objectType, V existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var ints = serializer.Deserialize<int[]>(reader);
            return new V(ints[0], ints[1]);
        }
    }
}