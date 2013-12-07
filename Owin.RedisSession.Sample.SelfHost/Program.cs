using System;
using Microsoft.Owin.Hosting;
using CloudStructures.Redis;
using Microsoft.Owin;

namespace Owin.RedisSessionSample.SelfHost
{
    class Program
    {
        static void Main(string[] args)
        {
            using (WebApp.Start<Startup>("http://localhost:12345"))
            {
                Console.ReadLine();
            }
        }
    }

    public class Startup
    {
        public void Configuration(Owin.IAppBuilder app)
        {
            // enable RedisSession Middleware.
            // RedisSession
            app.UseRedisSession(new RedisSessionOptions(new RedisSettings("127.0.0.1")));
            app.Run(async context => // request begin, Get all values from Redis server.
            {
                // take session from owin environment(IDictionary<string, object>)
                var session = context.Environment.AsRedisSession();

                // TryGet(or Get) take from local storage.
                DateTime lastAccess;
                int accessCount = 1;
                if (session.TryGet<DateTime>("LastAccess", out lastAccess) && session.TryGet<int>("Counter", out accessCount))
                {
                    accessCount++;
                    await context.Response.WriteAsync("AccessCount " + accessCount);
                    await context.Response.WriteAsync(", LastAccess from RedisSession => " + lastAccess.ToString());
                }
                else
                {
                    await context.Response.WriteAsync("First Access");
                }

                // Set(or Remove) set to local storage and enqueue operation.
                session.Set("Counter", accessCount);
                session.Set("LastAccess", DateTime.Now);

                context.Response.ContentType = "text/plain";
            }); // request end, queued set(or delete) values to Redis server.

            // context save pattern, can take everywhere.
            //app.UseRequestScopeContext();
            //app.UseRedisSession(new RedisSessionOptions(new RedisSettings("127.0.0.1")));
            //app.Run(async context =>
            //{
            //    Store();

            //    int v = -1;
            //    context.Environment.AsRedisSession().TryGet<int>("test", out v);
            //    context.Response.ContentType = "text/plain";
            //    await context.Response.WriteAsync(v.ToString());
            //});
        }

        static void Store()
        {
            // GetSession everywhere.
            var session = OwinRequestScopeContext.Current.Session();
            session.Set<int>("test", new Random().Next());
        }
    }

    public static class Extensions
    {
        public static RedisSession Session(this IOwinRequestScopeContext context)
        {
            return context.Environment.AsRedisSession();
        }
    }
}
