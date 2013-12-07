using CloudStructures.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Owin
{
    public class RedisSession
    {
        enum Operation
        {
            Set,
            Remove,
        }

        readonly string sessionKey;
        readonly RedisSessionOptions options;

        Dictionary<string, byte[]> cacheData;
        readonly Dictionary<string, Tuple<Operation, byte[]>> operations = new Dictionary<string, Tuple<Operation, byte[]>>();

        RedisSession(string sessionKey, RedisSessionOptions options)
        {
            this.sessionKey = sessionKey;
            this.options = options;
        }

        /// <summary>
        /// Take or generate redisSession key from cookie and get all session values from redis server.
        /// </summary>
        public static async Task<RedisSession> Start(IDictionary<string, object> owinEnvironment, RedisSessionOptions options)
        {
            var headers = owinEnvironment.AsRequestHeaders();

            var cookieName = options.CookieName;
            string sessionKey = null;

            var cookie = headers.GetValueOrDefault("Cookie");
            if (cookie != null)
            {
                var cookieValue = cookie
                    .SelectMany(x => HttpHelper.ParseCookie(x))
                    .FirstOrDefault(x => x.Item1 == cookieName);
                if (cookieValue != null)
                {
                    sessionKey = cookieValue.Item2;
                }
            }
            if (string.IsNullOrEmpty(sessionKey))
            {
                sessionKey = options.SessionKeyGenerator();

                var header = owinEnvironment.AsResponseHeaders();
                var setCookieContainer = header.GetValueOrDefault("Set-Cookie", new string[0]);

                var dest = new string[setCookieContainer.Length + 1];
                Array.Copy(setCookieContainer, dest, setCookieContainer.Length);
                dest[dest.Length - 1] = options.SetCookieOptions.ToSetCookieValue(cookieName, sessionKey);

                header["Set-Cookie"] = dest;
            }

            var session = new RedisSession(sessionKey, options);

            var redis = session.GetRedisSettings();
            ICommandTracer commandTracer = null;
            if (redis.CommandTracerFactory != null)
            {
                commandTracer = redis.CommandTracerFactory();
                commandTracer.CommandStart(redis, "RedisSession.Create", sessionKey);
            }

            var isError = false;
            try
            {
                var connection = redis.GetConnection();
                var cacheData = connection.Hashes.GetAll(redis.Db, sessionKey);
                var expire = connection.Keys.Expire(redis.Db, sessionKey, (int)options.SessionExpire.TotalSeconds);
                await Task.WhenAll(cacheData, expire).ConfigureAwait(false);
                session.cacheData = cacheData.Result;
            }
            catch
            {
                isError = true;
                throw;
            }
            finally
            {
                if (commandTracer != null)
                {
                    commandTracer.CommandFinish(null, null, isError);
                }
            }

            return session;
        }

        private RedisSettings GetRedisSettings()
        {
            return options.GetRedisSettings(sessionKey);
        }


        private byte[] this[string key]
        {
            get
            {
                byte[] value;
                return cacheData.TryGetValue(key, out value)
                    ? value
                    : null;
            }
            set
            {
                cacheData[key] = value;
                operations[key] = Tuple.Create(Operation.Set, value);
            }
        }

        /// <summary>
        /// Get value from local storage. If key doesn't exists return null. If T is struct, please use TryGet.
        /// </summary>
        public T Get<T>(string key) where T : class
        {
            T value;
            TryGet<T>(key, out value);
            return value;
        }

        /// <summary>
        /// Get value from local storage. If key doesn't exists return false.
        /// </summary>
        public bool TryGet<T>(string key, out T value)
        {
            var buff = this[key];
            if (buff == null)
            {
                value = default(T);
                return false;
            }

            var redis = GetRedisSettings();
            try
            {
                value = redis.ValueConverter.Deserialize<T>(buff);
                return true;
            }
            catch
            {
                value = default(T);
                Remove(key); // deserialize failed key remove

                return false;
            }
        }

        /// <summary>
        /// Set value to local storage and enqueue value. When execute FlushAll, value store to Redis server.
        /// </summary>
        public void Set<T>(string key, T val)
        {
            this[key] = GetRedisSettings().ValueConverter.Serialize(val);
        }

        /// <summary>
        /// Remove value to local storage and enqueue value. When execute FlushAll, value remove from Redis server.
        /// </summary>
        public bool Remove(string key)
        {
            if (cacheData.Remove(key))
            {
                operations[key] = Tuple.Create(Operation.Remove, (byte[])null);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Write store operation(set or remove) to Redis server.
        /// </summary>
        /// <returns></returns>
        public async Task FlushAll()
        {
            if (operations.Count == 0) return;
            if (sessionKey == null) return;

            var set = new Dictionary<string, byte[]>();
            var remove = new List<string>();
            foreach (var item in operations)
            {
                if (item.Value.Item1 == Operation.Set) set.Add(item.Key, item.Value.Item2);
                else if (item.Value.Item1 == Operation.Remove) remove.Add(item.Key);
            }

            var redis = GetRedisSettings();

            ICommandTracer commandTracer = null;
            if (redis.CommandTracerFactory != null)
            {
                commandTracer = redis.CommandTracerFactory();
                commandTracer.CommandStart(redis, "RedisSession.FlushAll", sessionKey);
            }

            var isError = false;
            try
            {
                using (var tx = redis.GetConnection().CreateTransaction())
                {
                    if (set.Any())
                    {
                        var _ = tx.Hashes.Set(redis.Db, sessionKey, set);
                    }
                    if (remove.Any())
                    {
                        var _ = tx.Hashes.Remove(redis.Db, sessionKey, remove.ToArray());
                    }

                    await tx.Execute().ConfigureAwait(false);
                }
            }
            catch
            {
                isError = true;
                throw;
            }
            finally
            {
                if (commandTracer != null)
                {
                    commandTracer.CommandFinish(null, null, isError);
                }
            }

            operations.Clear();
        }
    }
}