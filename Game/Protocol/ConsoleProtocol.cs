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

        public static ReadResult Read()
        {
            var line = Console.ReadLine();
            if (line == null)
                return null;

            var json = JToken.Parse(line);
            var type = json["type"].ToString();

            if (type == "start_game")
            {
                var config = json["params"].ToObject<Config>();
                return new ReadResult {Type = type, Config = config};
            }

            if (type == "tick")
            {
                var input = json["params"].ToObject<RequestInput>();
                return new ReadResult {Type = type, Input = input};
            }

            return new ReadResult {Type = type};
        }

        public static void Write(RequestOutput output)
        {
            if (output.Debug != null && output.Debug.Length > 200)
                output.Debug = output.Debug.Substring(0, 200);
            if (output.Error != null && output.Error.Length > 200)
                output.Error = output.Error.Substring(0, 200);
            Console.WriteLine(JsonConvert.SerializeObject(output, jsonSerializerSettings));
        }
    }
}