using DesiCorner.AuthServer.Data;
using DesiCorner.AuthServer.Identity;
using DesiCorner.AuthServer.Infrastructure;
using DesiCorner.AuthServer.Models;
using DesiCorner.AuthServer.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// DbContexts
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(cfg.GetConnectionString("Default")));

builder.Services.AddDbContext<DataProtectionKeyContext>(opt =>
    opt.UseSqlServer(cfg.GetConnectionString("DataProtection")));

// Data Protection keys
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

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(cfg["Redis:Configuration"]!));

// Configure JWT Settings
builder.Services.Configure<JwtSettings>(cfg.GetSection("JwtSettings"));
var jwtSettings = cfg.GetSection("JwtSettings").Get<JwtSettings>();

if (jwtSettings == null)
{
    throw new InvalidOperationException("JWT Settings not configured");
}

// Services
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddSingleton<IOtpService, OtpService>();
builder.Services.AddScoped<ITokenService, TokenService>(); // ← JWT Token Service

// JWT Authentication - Use Authority for OpenIddict tokens
// This allows the profile endpoint to validate tokens issued by OpenIddict
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false; // Dev only!
    // IMPORTANT: Disable claim mapping to keep original claim types
    options.MapInboundClaims = false;

    // Use Authority for JWKS discovery (validates OpenIddict's RSA tokens)
    options.Authority = cfg["OpenIddict:Issuer"];
    options.Audience = cfg["OpenIddict:Audience"];

    // Allow both OpenIddict tokens (RSA) and validation tokens
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = cfg["OpenIddict:Issuer"],
        ValidateAudience = true,
        ValidAudiences = new[] { "desicorner-api", "desicorner-angular" },
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(60),
        ValidateIssuerSigningKey = true,
        // Note: IssuerSigningKey is NOT set here - it will be retrieved from JWKS endpoint
        RoleClaimType = "role",  
        NameClaimType = "name"      
    };
    

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<Program>>();
            logger.LogInformation("✅ JWT validated for user: {User}",
                context.Principal?.Identity?.Name);
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<Program>>();
            logger.LogError(context.Exception, "❌ JWT authentication failed");
            return Task.CompletedTask;
        }
    };
});

// Authorization - Accept both Cookie and JWT
builder.Services.AddAuthorization(options =>
{
    var defaultPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();

    options.DefaultPolicy = defaultPolicy;
});

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

        // Flows - ADD PASSWORD FLOW HERE
        opt.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();
        opt.AllowRefreshTokenFlow();
        opt.AllowPasswordFlow();

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
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.SlidingExpiration = true;

    // API-only - return 401 instead of redirecting
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("desicorner-angular", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "DesiCorner Auth API",
        Version = "v1"
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Migrate and seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    var dp = scope.ServiceProvider.GetRequiredService<DataProtectionKeyContext>();
    await dp.Database.MigrateAsync();

    await Seed.InitializeAsync(app.Services, cfg["OpenIddict:Issuer"]!);

    // Also seed with DbInitializer for roles
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    await DbInitializer.Initialize(db, userManager, roleManager);
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("desicorner-angular");
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