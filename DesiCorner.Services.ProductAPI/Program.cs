using DesiCorner.MessageBus.Extensions;
using DesiCorner.Services.ProductAPI.Data;
using DesiCorner.Services.ProductAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// Database
builder.Services.AddDbContext<ProductDbContext>(opt =>
    opt.UseSqlServer(cfg.GetConnectionString("Default")));

// MessageBus (Redis + Service Bus) - This adds ICacheService automatically
builder.Services.AddDesiCornerMessageBus(cfg);

// Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IReviewService, ReviewService>();

// Controllers
builder.Services.AddControllers();

// JWT Authentication - Must match AuthServer settings EXACTLY
var jwtSecret = cfg["JwtSettings:Secret"];
if (string.IsNullOrEmpty(jwtSecret))
{
    throw new InvalidOperationException("JWT Secret not configured");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = false; // Dev only!
        // IMPORTANT: Disable claim mapping to keep original claim types
        options.MapInboundClaims = false;

        options.Authority = cfg["JwtSettings:Issuer"];
        options.Audience = cfg["JwtSettings:Audience"]; 

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            //IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = cfg["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = cfg["JwtSettings:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = "role",
            NameClaimType = "name"
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogInformation("JWT validated for user: {User}",
                    context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "JWT authentication failed");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// CORS - Allow Gateway
builder.Services.AddCors(options =>
{
    options.AddPolicy("Gateway", policy =>
    {
        policy.WithOrigins("https://localhost:5000", "http://localhost:5000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Swagger with JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "DesiCorner Product API",
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

// Image Storage Service
builder.Services.AddScoped<IImageStorageService, LocalImageStorageService>();

var app = builder.Build();

// Migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    await db.Database.MigrateAsync();
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Gateway");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check
app.MapGet("/health/ready", () => Results.Ok(new
{
    status = "ready",
    service = "ProductAPI",
    timestamp = DateTime.UtcNow
}));

app.MapGet("/health/redis", async (IConnectionMultiplexer redis) =>
{
    try
    {
        var db = redis.GetDatabase();
        await db.PingAsync();
        return Results.Ok(new { status = "connected", redis = "OK" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Redis connection failed: {ex.Message}", statusCode: 500);
    }
});

app.Run();