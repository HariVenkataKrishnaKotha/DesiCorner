using DesiCorner.Gateway.Auth;
using DesiCorner.Gateway.Infrastructure;
using DesiCorner.Gateway.Policies;
using DesiCorner.Gateway.Transforms;
using Microsoft.AspNetCore.Authorization;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using OpenTelemetry.Instrumentation.Runtime;
using System.Net;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// Redis: multiplexer + distributed cache
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(cfg["Redis:Configuration"]!));

builder.Services.AddStackExchangeRedisCache(opt =>
{
    opt.Configuration = cfg["Redis:Configuration"];
});

builder.Services.AddSingleton<IRedisRateLimiter, RedisRateLimiter>();

// Auth Services
builder.Services.AddScoped<ITokenAuthenticator, TokenAuthenticator>();

builder.Services.AddHttpClient<IJwksProvider, JwksProvider>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
    });

builder.Services.AddHttpClient<IIntrospectionClient, IntrospectionClient>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
    });

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("DesiCorner.Gateway", serviceVersion: "1.0.0"))
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation())
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation(o => { o.RecordException = true; })
        .AddHttpClientInstrumentation());

// Authorization policies
builder.Services.AddSingleton<IAuthorizationHandler, ScopeAuthorizationHandler>();
builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("ProductRead", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new ScopeRequirement("desicorner.products.read"));
    });
    opt.AddPolicy("ProductWrite", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.Requirements.Add(new ScopeRequirement("desicorner.products.write"));
        policy.RequireRole("Admin");
    });
    opt.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin");
        policy.Requirements.Add(new ScopeRequirement("desicorner.admin"));
    });
});

// CORS - Allow Angular app
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("Angular", p => p
        .WithOrigins("https://localhost:4200", "http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// Health checks
builder.Services.AddHealthChecks();

// YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(cfg.GetSection("ReverseProxy"))
    .ConfigureHttpClient((context, handler) =>
    {
        handler.AllowAutoRedirect = false;
    });

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Gateway");

// MIDDLEWARE PIPELINE

app.UseHttpsRedirection();

// Security headers
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

// CORS
app.UseCors("Angular");

// Request logging
app.Use(async (ctx, next) =>
{
    logger.LogInformation("Gateway: {Method} {Path}{QueryString}",
        ctx.Request.Method,
        ctx.Request.Path,
        ctx.Request.QueryString);

    await next();

    logger.LogInformation("Gateway: Response {Status} for {Path}",
        ctx.Response.StatusCode,
        ctx.Request.Path);
});

// Test endpoint - bypass all auth
app.MapGet("/test-token", async (HttpContext ctx, IConfiguration config) =>
{
    var token = ctx.Request.Headers.Authorization.ToString().Replace("Bearer ", "");

    var authenticator = ctx.RequestServices.GetRequiredService<ITokenAuthenticator>();
    var result = await authenticator.AuthenticateAsync(token, ctx.RequestAborted);

    return Results.Ok(new
    {
        Success = result.ok,
        Source = result.source,
        Error = result.error,
        HasPrincipal = result.principal != null,
        Claims = result.principal?.Claims.Select(c => new { c.Type, c.Value }).ToList()
    });
});

// Auth middleware: validate Authorization Bearer for API routes
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path.Value ?? "";
    var method = ctx.Request.Method;

    // PUBLIC PATHS (no Bearer token required):
    var isPublicPath = path.StartsWith("/connect", StringComparison.OrdinalIgnoreCase) ||
                       path.StartsWith("/api/account", StringComparison.OrdinalIgnoreCase) ||
                       path.StartsWith("/.well-known", StringComparison.OrdinalIgnoreCase) ||
                       path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
                       path.StartsWith("/test-token", StringComparison.OrdinalIgnoreCase);

    // Allow public READ access to products and categories (e-commerce browsing)
    var isPublicProductBrowsing = method.Equals("GET", StringComparison.OrdinalIgnoreCase) &&
                                   (path.StartsWith("/api/products", StringComparison.OrdinalIgnoreCase) ||
                                    path.StartsWith("/api/categories", StringComparison.OrdinalIgnoreCase));

    if (isPublicPath || isPublicProductBrowsing)
    {
        await next();
        return;
    }

    // For /api/** routes, require Bearer token
    var authz = ctx.Request.Headers.Authorization.ToString();
    var token = authz.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        ? authz.Substring("Bearer ".Length).Trim()
        : null;

    if (string.IsNullOrEmpty(token))
    {
        ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        await ctx.Response.WriteAsJsonAsync(new { error = "missing_bearer" });
        return;
    }

    var authenticator = ctx.RequestServices.GetRequiredService<ITokenAuthenticator>();
    var (ok, principal, source, err) = await authenticator.AuthenticateAsync(token, ctx.RequestAborted);

    if (!ok || principal is null)
    {
        ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        await ctx.Response.WriteAsJsonAsync(new { error = "invalid_token", reason = err });
        return;
    }

    // Set user and mark source
    ctx.User = principal;
    ctx.Request.Headers["X-Auth-Source"] = source;
    ForwardingTransforms.AddForwardedIdentityHeaders(ctx);

    await next();
});

// Rate limiting middleware
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path.Value ?? "";
    var limiter = ctx.RequestServices.GetRequiredService<IRedisRateLimiter>();
    var cfgRL = cfg.GetSection("RateLimiting");

    // Rate-limit /connect/* (OAuth endpoints)
    if (path.StartsWith("/connect/", StringComparison.OrdinalIgnoreCase))
    {
        var win = TimeSpan.FromSeconds(cfgRL.GetValue("AuthWindowSeconds", 60));
        var max = cfgRL.GetValue("AuthMax", 60);
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"rl:auth:{ip}";

        if (await limiter.ShouldLimitAsync(key, max, win, ctx.RequestAborted))
        {
            ctx.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            ctx.Response.Headers["Retry-After"] = "60";
            await ctx.Response.WriteAsJsonAsync(new { error = "rate_limited_auth" });
            return;
        }
    }

    // Rate-limit /api/* (API endpoints)
    if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
    {
        var win = TimeSpan.FromSeconds(cfgRL.GetValue("ApiWindowSeconds", 60));
        var max = cfgRL.GetValue("ApiMax", 100);
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"rl:api:{ip}";

        if (await limiter.ShouldLimitAsync(key, max, win, ctx.RequestAborted))
        {
            ctx.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            ctx.Response.Headers["Retry-After"] = "60";
            await ctx.Response.WriteAsJsonAsync(new { error = "rate_limited_api" });
            return;
        }
    }

    await next();
});

// Request logging scope
app.Use(async (ctx, next) =>
{
    using (logger.BeginScope(new Dictionary<string, object?>
    {
        ["request_id"] = ctx.TraceIdentifier,
        ["path"] = ctx.Request.Path.Value,
        ["method"] = ctx.Request.Method,
        ["client_ip"] = ctx.Connection.RemoteIpAddress?.ToString()
    }))
    {
        await next();
    }
});

// Map YARP reverse proxy
app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.Use((ctx, nxt) =>
    {
        if (!ctx.Request.Headers.ContainsKey("X-Request-Id"))
            ctx.Request.Headers["X-Request-Id"] = ctx.TraceIdentifier;

        // Remove hop-by-hop headers
        ctx.Request.Headers.Remove("Connection");
        ctx.Request.Headers.Remove("Keep-Alive");
        ctx.Request.Headers.Remove("Proxy-Authenticate");
        ctx.Request.Headers.Remove("Proxy-Authorization");
        ctx.Request.Headers.Remove("TE");
        ctx.Request.Headers.Remove("Trailers");
        ctx.Request.Headers.Remove("Transfer-Encoding");
        ctx.Request.Headers.Remove("Upgrade");

        return nxt();
    });
});

// Health endpoints
app.MapGet("/health/live", () => Results.Ok(new { status = "live", service = "DesiCorner.Gateway" }));

app.MapGet("/health/ready", async (IConnectionMultiplexer mux) =>
{
    try
    {
        var db = mux.GetDatabase();
        await db.PingAsync();
        return Results.Ok(new { status = "ready", redis = "connected", service = "DesiCorner.Gateway" });
    }
    catch (Exception ex)
    {
        return Results.Problem(title: "redis_unavailable", detail: ex.Message, statusCode: 503);
    }
});

app.Run();