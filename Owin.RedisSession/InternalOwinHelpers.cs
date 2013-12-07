using System;
using System.Collections.Generic;
using System.IO;

namespace Owin
{
    internal static class OwinExtensions
    {
        public static Stream AsResponseBody(this IDictionary<string, object> environment)
        {
            return environment["owin.RequestBody"] as Stream;
        }

        public static IDictionary<string, string[]> AsRequestHeaders(this IDictionary<string, object> environment)
        {
            return environment["owin.RequestHeaders"] as IDictionary<string, string[]>;
        }

        public static IDictionary<string, string[]> AsResponseHeaders(this IDictionary<string, object> environment)
        {
            return environment["owin.ResponseHeaders"] as IDictionary<string, string[]>;
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default(TValue))
        {
            TValue value;
            if (dict.TryGetValue(key, out value))
            {
                return value;
            }
            else
            {
                return defaultValue;
            }
        }
    }

    internal static class HttpHelper
    {
        public static IEnumerable<Tuple<string, string>> ParseCookie(string cookie)
        {
            var split = cookie.Split(';');
            foreach (var item in split)
            {
                var split2 = item.Split('=');
                if (split2.Length != 2) continue;
                yield return new Tuple<string, string>(split2[0].Trim(), split2[1].Trim());
            }
        }
    }
}