using DesiCorner.MessageBus.Extensions;
using DesiCorner.Services.ProductAPI.Data;
using DesiCorner.Services.ProductAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// Database
builder.Services.AddDbContext<ProductDbContext>(opt =>
    opt.UseSqlServer(cfg.GetConnectionString("Default")));

// Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// MessageBus (Redis + Service Bus)
builder.Services.AddDesiCornerMessageBus(cfg);

// Controllers
builder.Services.AddControllers();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = cfg["JwtOptions:Issuer"];
        options.Audience = cfg["JwtOptions:Audience"];
        options.RequireHttpsMetadata = false; // Dev only!

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = cfg["JwtOptions:Issuer"],
            ValidateAudience = true,
            ValidAudience = cfg["JwtOptions:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(60)
        };
    });

builder.Services.AddAuthorization();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", service = "ProductAPI" }));

app.Run();