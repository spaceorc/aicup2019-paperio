using System;
using System.Collections.Generic;
using System.Linq;
using Game.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Game.Helpers
{
    public static class GameHelpers
    {
        public static void Shuffle<T>(this Random random, IList<T> arr, int count = -1, int iterations = -1)
        {
            if (count < 0 || count > arr.Count)
                count = arr.Count;
            if (iterations < 0 || iterations > count - 1)
                iterations = count - 1;
            for (var i = 0; i < iterations; i++)
            {
                var t = random.Next(i, count);
                var tmp = arr[i];
                arr[i] = arr[t];
                arr[t] = tmp;
            }
        }

        public static string ToJson(this object o)
        {
            return JsonConvert.SerializeObject(o, ConsoleProtocol.jsonSerializerSettings);
        }

        public static List<Type> GetImplementors<T>()
        {
            return typeof(T).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(T).IsAssignableFrom(t)).ToList();
        }

        public static T GetOrAdd<TKey, T>(this Dictionary<TKey, T> dct, TKey key)
            where T : new()
        {
            return dct.GetOrAdd(key, _ => new T());
        }

        public static T GetOrAdd<TKey, T>(this Dictionary<TKey, T> dct, TKey key, Func<TKey, T> factory)
        {
            if (dct.TryGetValue(key, out var value))
                return value;
            dct.Add(key, value = factory(key));
            return value;
        }

        public static void Remove<TKey, T>(this Dictionary<TKey, T> dct, KeyValuePair<TKey, T> kvp)
        {
            ((ICollection<KeyValuePair<TKey, T>>)dct).Remove(kvp);
        }
    }
}