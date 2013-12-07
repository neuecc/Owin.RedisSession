Owin.RedisSession
=================

Redis Session Provider for Owin built by [CloudStructures](https://github.com/neuecc/CloudStructures) which is Redis Client based on BookSleeve.

Install
---
using with NuGet, [Owin.RedisSession](https://nuget.org/packages/Owin.RedisSession/)
```
PM> Install-Package Owin.RedisSession
```

Usage
---
```csharp
// enable RedisSession Middleware.
app.UseRedisSession(new RedisSessionOptions(new RedisSettings("127.0.0.1")));
app.Run(async context => // request begin, middelware get all values from Redis server.
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
}); // request end, middleware queued set(or delete) values to Redis server.
```

Full code is avaliable on this Repositry, [Owin.RedisSession.Sample.SelfHost](https://github.com/neuecc/Owin.RedisSession/tree/master/Owin.RedisSession.Sample.SelfHost).

with OwinRequestScopeContext
---
[OwinRequestScopeContext](https://github.com/neuecc/OwinRequestScopeContext) enables take RedisSession everywhere.

```csharp
app.UseRequestScopeContext();
app.UseRedisSession(new RedisSessionOptions(new RedisSettings("127.0.0.1")));
app.Run(async context =>
{
    Store();

    int v = -1;
    context.Environment.AsRedisSession().TryGet<int>("test", out v);
    context.Response.ContentType = "text/plain";
    await context.Response.WriteAsync(v.ToString());
});

void Store()
{
    // GetSession everywhere.
    var session = OwinRequestScopeContext.Current.Session();
    session.Set<int>("test", new Random().Next());
}
```

History
---
2013-12-08 ver 1.0.0
* first release.

License
---
under [MIT License](http://opensource.org/licenses/MIT)
