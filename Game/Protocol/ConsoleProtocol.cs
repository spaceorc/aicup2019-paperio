using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Game.Protocol
{
    public static class ConsoleProtocol
    {
        public static readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = {new StringEnumConverter(true)}
        };

        public static Config ReadConfig()
        {
            var line = Console.ReadLine();
            var json = JToken.Parse(line);
            if (json["type"].ToString() != "start_game")
                return null;
            return json["params"].ToObject<Config>();
        }

        public static TurnInput ReadTurnInput()
        {
            var line = Console.ReadLine();
            if (line == null)
                return null;
            var json = JToken.Parse(line);
            if (json["type"].ToString() != "tick")
                return null;
            return json["params"].ToObject<TurnInput>();
        }

        public static void WriteTurnInput(TurnOutput output)
        {
            if (output.Debug != null && output.Debug.Length > 200)
                output.Debug = output.Debug.Substring(0, 200);
            if (output.Error != null && output.Error.Length > 200)
                output.Error = output.Error.Substring(0, 200);
            var line = JsonConvert.SerializeObject(output, jsonSerializerSettings);
            Console.WriteLine(line);
        }
    }
}