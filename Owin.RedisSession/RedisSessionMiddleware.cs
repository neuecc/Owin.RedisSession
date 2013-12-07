using CloudStructures.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Owin
{
    using AppFunc = Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

    public class RedisSessionMiddleware
    {
        public const string StoreEnvironmentKey = "owin.RedisSession";

        readonly AppFunc next;
        readonly RedisSessionOptions options;

        public RedisSessionMiddleware(AppFunc next, RedisGroup redis)
            : this(next, new RedisSessionOptions(redis))
        {
        }

        public RedisSessionMiddleware(AppFunc next, RedisSettings redis)
            : this(next, new RedisSessionOptions(redis))
        {
        }

        public RedisSessionMiddleware(AppFunc next, RedisSessionOptions options)
        {
            this.next = next;
            this.options = options;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            var session = await RedisSession.Start(environment, options);
            environment[StoreEnvironmentKey] = session;

            await next(environment).ContinueWith(_ => session.FlushAll()).Unwrap();
        }
    }
}

namespace Owin
{
    public static class OwinRedisSessionExtensions
    {
        /// <summary>
        /// Use RedisSession. After enabled, you can take RedisSession from environment.AsRedisSession().
        /// </summary>
        public static IAppBuilder UseRedisSession(this IAppBuilder app, RedisGroup redis)
        {
            return app.Use(typeof(RedisSessionMiddleware), redis);
        }

        /// <summary>
        /// Use RedisSession. After enabled, you can take RedisSession from environment.AsRedisSession().
        /// </summary>
        public static IAppBuilder UseRedisSession(this IAppBuilder app, RedisSettings redis)
        {
            return app.Use(typeof(RedisSessionMiddleware), redis);
        }

        /// <summary>
        /// Use RedisSession. After enabled, you can take RedisSession from environment.AsRedisSession().
        /// </summary>
        public static IAppBuilder UseRedisSession(this IAppBuilder app, RedisSessionOptions options)
        {
            return app.Use(typeof(RedisSessionMiddleware), options);
        }

        /// <summary>
        /// Take RedisSession from owinEnvironment.
        /// </summary>
        public static RedisSession AsRedisSession(this IDictionary<string, object> owinEnvironment)
        {
            return owinEnvironment[RedisSessionMiddleware.StoreEnvironmentKey] as RedisSession;
        }
    }
}