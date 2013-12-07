using CloudStructures.Redis;
using System;
using System.Globalization;

namespace Owin
{
    public class RedisSessionOptions
    {
        public TimeSpan SessionExpire { get; set; }
        public string CookieName { get; set; }
        public CookieOptions SetCookieOptions { get; set; }
        public Func<string> SessionKeyGenerator { get; set; }

        readonly RedisGroup redisGroup;
        readonly RedisSettings redisSettings;

        public RedisSessionOptions(RedisGroup redisGroup)
        {
            if (redisGroup == null) throw new ArgumentNullException("redisGroup");

            this.redisGroup = redisGroup;
            SetDefault();
        }

        public RedisSessionOptions(RedisSettings redisSettings)
        {
            if (redisSettings == null) throw new ArgumentNullException("redisSettings");

            this.redisSettings = redisSettings;
            SetDefault();
        }

        void SetDefault()
        {
            SessionExpire = TimeSpan.FromDays(30);
            CookieName = "session";
            SessionKeyGenerator = () => Guid.NewGuid().ToString();
            SetCookieOptions = new CookieOptions();
        }

        internal RedisSettings GetRedisSettings(string sessionKey)
        {
            if (redisSettings != null) return redisSettings;
            if (redisGroup != null) return redisGroup.GetSettings(sessionKey);

            throw new InvalidOperationException("RedisSettings or RedisGroup is null");
        }

        public class CookieOptions
        {
            public string Path { get; set; }
            public string Domain { get; set; }
            public DateTime? Expires { get; set; }
            public bool Secure { get; set; }
            public bool HttpOnly { get; set; }

            public string ToSetCookieValue(string key, string value)
            {
                // borrow from Microsoft.Owin.ResponseCookieCollection

                bool domainHasValue = !string.IsNullOrEmpty(this.Domain);
                bool pathHasValue = !string.IsNullOrEmpty(this.Path);
                bool expiresHasValue = this.Expires.HasValue;

                string setCookieValue = string.Concat(
                     Uri.EscapeDataString(key),
                     "=",
                     Uri.EscapeDataString(value ?? string.Empty),
                     !domainHasValue ? null : "; domain=",
                     !domainHasValue ? null : this.Domain,
                     !pathHasValue ? null : "; path=",
                     !pathHasValue ? null : this.Path,
                     !expiresHasValue ? null : "; expires=",
                     !expiresHasValue ? null : this.Expires.Value.ToString("ddd, dd-MMM-yyyy HH:mm:ss ", CultureInfo.InvariantCulture) + "GMT",
                     !this.Secure ? null : "; secure",
                     !this.HttpOnly ? null : "; HttpOnly");

                return setCookieValue;
            }
        }
    }
}