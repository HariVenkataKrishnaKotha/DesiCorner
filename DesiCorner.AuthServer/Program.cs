using DesiCorner.AuthServer.Data;
using DesiCorner.AuthServer.Identity;
using DesiCorner.AuthServer.Infrastructure;
using DesiCorner.AuthServer.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using System.Net;
using Twilio.Types;
using static System.Net.WebRequestMethods;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// DbContexts
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(cfg.GetConnectionString("Default")));

builder.Services.AddDbContext<DataProtectionKeyContext>(opt =>
    opt.UseSqlServer(cfg.GetConnectionString("DataProtection")));

// Data Protection keys (for multi-instance deployments)
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<DataProtectionKeyContext>();

// Identity
builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(opt =>
    {
        opt.User.RequireUniqueEmail = true;
        opt.Password.RequiredLength = 8;
        opt.Password.RequireDigit = true;
        opt.Password.RequireLowercase = true;
        opt.Password.RequireUppercase = true;
        opt.Password.RequireNonAlphanumeric = false;
        opt.Lockout.MaxFailedAccessAttempts = 5;
        opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        opt.SignIn.RequireConfirmedEmail = false;
        opt.SignIn.RequireConfirmedPhoneNumber = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Controllers
builder.Services.AddControllers();

// Redis (local instance)
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(cfg["Redis:Configuration"]!));

// Mock service instead
//builder.Services.AddSingleton<IOtpService, MockOtpService>();

// Services
// Services
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddSingleton<IOtpService, OtpService>();

// OpenIddict
builder.Services.AddOpenIddict()
    .AddCore(opt => opt.UseEntityFrameworkCore().UseDbContext<ApplicationDbContext>())
    .AddServer(opt =>
    {
        opt.SetIssuer(new Uri(cfg["OpenIddict:Issuer"]!));
        opt.DisableAccessTokenEncryption();

        // Development only - remove in production!
        opt.UseAspNetCore().DisableTransportSecurityRequirement();

        // Endpoints
        opt.SetAuthorizationEndpointUris("/connect/authorize")
           .SetTokenEndpointUris("/connect/token")
           .SetIntrospectionEndpointUris("/connect/introspect")
           .SetRevocationEndpointUris("/connect/revocation")
           .SetUserInfoEndpointUris("/connect/userinfo");

        // Flows
        opt.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();
        opt.AllowRefreshTokenFlow();

        // Development keys (use proper keys in production!)
        opt.AddEphemeralEncryptionKey()
           .AddEphemeralSigningKey();

        // Register scopes
        opt.RegisterScopes(
            OpenIddict.Abstractions.OpenIddictConstants.Scopes.OpenId,
            OpenIddict.Abstractions.OpenIddictConstants.Scopes.Email,
            OpenIddict.Abstractions.OpenIddictConstants.Scopes.Profile,
            OpenIddict.Abstractions.OpenIddictConstants.Scopes.Phone,
            OpenIddict.Abstractions.OpenIddictConstants.Scopes.OfflineAccess,
            "desicorner.products.read",
            "desicorner.products.write",
            "desicorner.cart",
            "desicorner.orders.read",
            "desicorner.orders.write",
            "desicorner.payment",
            "desicorner.admin");

        // Token lifetimes
        opt.SetAccessTokenLifetime(TimeSpan.FromMinutes(30));
        opt.SetRefreshTokenLifetime(TimeSpan.FromDays(30));

        // ASP.NET Core integration
        opt.UseAspNetCore()
           .EnableAuthorizationEndpointPassthrough()
           .EnableTokenEndpointPassthrough()
           .EnableUserInfoEndpointPassthrough();
    })
    .AddValidation(opt =>
    {
        opt.UseLocalServer();
        opt.UseAspNetCore();
    });

// Configure Identity cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = ".DesiCorner.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
    options.Cookie.SameSite = SameSiteMode.Lax; // Important for cross-origin (Angular on different port)
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.SlidingExpiration = true;

    // API-only - return 401 instead of redirecting to login page
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
});

// CORS - Allow Angular app
builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Important for cookies!
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Migrate and seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    var dp = scope.ServiceProvider.GetRequiredService<DataProtectionKeyContext>();
    await dp.Database.MigrateAsync();

    await Seed.InitializeAsync(app.Services, cfg["OpenIddict:Issuer"]!);
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS must be before Authentication
app.UseCors("Angular");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health endpoints
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", service = "DesiCorner.AuthServer" }));

app.MapGet("/health/redis", async (IConnectionMultiplexer mux) =>
{
    try
    {
        var db = mux.GetDatabase();
        await db.PingAsync();
        return Results.Ok(new { status = "connected", server = "localhost:6379" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Redis connection failed: {ex.Message}", statusCode: 500);
    }
});

app.Run();