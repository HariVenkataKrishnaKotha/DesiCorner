using DesiCorner.Services.OrderAPI.Data;
using DesiCorner.Services.OrderAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<IOrderService, OrderService>();

// HttpClient for inter-service communication
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ForwardAuthorizationHandler>();

// CartAPI is reached through the gateway; the caller's bearer token identifies the cart
builder.Services.AddHttpClient("CartAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:Gateway"]
        ?? "https://localhost:5000");
})
.AddHttpMessageHandler<ForwardAuthorizationHandler>();

// AuthServer's user lookup is service-only: OrderAPI authenticates with its own
// client-credentials token, fetched on a separate client so the handler never recurses
var authServerUrl = new Uri(builder.Configuration["ServiceUrls:AuthAPI"] ?? "https://localhost:7001");
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IServiceTokenProvider, ServiceTokenProvider>();
builder.Services.AddTransient<ServiceTokenHandler>();
builder.Services.AddHttpClient(ServiceTokenProvider.HttpClientName, client => client.BaseAddress = authServerUrl);

builder.Services.AddHttpClient("AuthAPI", client =>
{
    client.BaseAddress = authServerUrl;
})
.AddHttpMessageHandler<ServiceTokenHandler>();

// HttpClient for PaymentAPI
builder.Services.AddHttpClient("PaymentAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:PaymentAPI"]
        ?? "https://localhost:7501");
    client.Timeout = TimeSpan.FromSeconds(30);
})
// PaymentAPI answers /verify only for the payment's creator: forward the caller's token
.AddHttpMessageHandler<ForwardAuthorizationHandler>()
.ConfigurePrimaryHttpMessageHandler(() => builder.Environment.IsDevelopment()
    // Development only: accept the local dev certificate. Elsewhere certificates are validated.
    ? new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true }
    : new HttpClientHandler());

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.Audience = builder.Configuration["Authentication:Audience"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        // IMPORTANT: Disable claim mapping to keep original claim types
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = "role",
            NameClaimType = "name"
        };
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowGateway", policy =>
    {
        policy.WithOrigins(
                builder.Configuration["Cors:Gateway"] ?? "http://localhost:5000",
                builder.Configuration["Cors:Angular"] ?? "http://localhost:4200"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// The client-credentials secret comes only from configuration (user-secrets or environment)
if (string.IsNullOrWhiteSpace(app.Configuration[ServiceTokenProvider.ClientSecretKey]))
{
    if (!app.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            $"Missing configuration: {ServiceTokenProvider.ClientSecretKey} " +
            $"(set the {ServiceTokenProvider.ClientSecretKey.Replace(":", "__")} environment variable)");
    }

    app.Logger.LogWarning("{Key} is not configured; guest orders are not linked to registered accounts",
        ServiceTokenProvider.ClientSecretKey);
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Apply migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    // Non-relational providers (e.g. the EF in-memory provider used by tests) have no migrations
    if (context.Database.IsRelational())
    {
        context.Database.Migrate();
    }
}

app.UseCors("AllowGateway");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "OrderAPI" }));

app.Run();

// Exposed so integration tests can host the app with WebApplicationFactory<Program>.
public partial class Program { }
