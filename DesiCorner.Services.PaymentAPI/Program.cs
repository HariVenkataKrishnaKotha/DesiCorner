using DesiCorner.Services.PaymentAPI.Data;
using DesiCorner.Services.PaymentAPI.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<PaymentDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Payment Service
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Stripe PaymentIntent client (injected so it can be replaced in tests)
builder.Services.AddScoped<PaymentIntentService>();

// Cart lookup, used to compute the amount to charge server-side.
// CartAPI is reached through the gateway with the caller's own credentials.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICartClient, CartClient>();
builder.Services.AddHttpClient("CartAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:Gateway"]
        ?? "https://localhost:5000");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Configure Stripe
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// HttpClient for calling other services
builder.Services.AddHttpClient("OrderAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:OrderAPI"]
        ?? "https://localhost:7401");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
});

// CORS - Allow Gateway and Angular app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowGateway", policy =>
    {
        policy.WithOrigins(
            "https://localhost:5000",  // Gateway
            "https://localhost:4200",  // Angular
            "http://localhost:4200"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

var app = builder.Build();

// Webhook events are only trusted when their Stripe signature verifies. Outside
// Development, refuse to start without the signing secret. It comes from user-secrets
// or the Stripe__WebhookSecret environment variable, never from appsettings*.json.
if (!app.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(app.Configuration["Stripe:WebhookSecret"]))
{
    throw new InvalidOperationException(
        "Stripe:WebhookSecret is not configured. Set it with user-secrets or the Stripe__WebhookSecret environment variable.");
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowGateway");

app.UseAuthorization();

app.MapControllers();

// Log startup
Log.Information("PaymentAPI starting on {Url}",
    builder.Configuration["ASPNETCORE_URLS"] ?? "https://localhost:7501");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "PaymentAPI",
    timestamp = DateTime.UtcNow
}));

try
{
    Log.Information("Starting PaymentAPI");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "PaymentAPI terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Exposed so integration tests can host the app with WebApplicationFactory<Program>.
public partial class Program { }
