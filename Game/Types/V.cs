using Game.Protocol;
using Newtonsoft.Json;

namespace Game.Types
{
    [JsonArray, JsonConverter(typeof(VConverter))]
    public class V
    {
        public V(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
    }
}