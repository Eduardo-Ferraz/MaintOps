using System.Text;
using System.Threading.RateLimiting;
using Carter;
using FluentValidation;
using Industriall.MaintOps.Api.Common.Behaviors;
using Industriall.MaintOps.Api.Common.Exceptions;
using Industriall.MaintOps.Api.Infrastructure.Database;
using Industriall.MaintOps.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ────────────────────────────────────────────────────────────────────────────
// Database
// ────────────────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.GetName().Name)));

// ────────────────────────────────────────────────────────────────────────────
// MediatR – behaviours are registered in order: Logging → Validation → Handler
// ────────────────────────────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// ────────────────────────────────────────────────────────────────────────────
// FluentValidation – scans for all AbstractValidator<T> in this assembly
// ────────────────────────────────────────────────────────────────────────────
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// ────────────────────────────────────────────────────────────────────────────
// Carter – Minimal API module discovery
// ────────────────────────────────────────────────────────────────────────────
builder.Services.AddCarter();

// ────────────────────────────────────────────────────────────────────────────
// Global Exception Handler (RFC 7807 Problem Details)
// ────────────────────────────────────────────────────────────────────────────
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ────────────────────────────────────────────────────────────────────────────
// JWT Bearer Authentication
// ────────────────────────────────────────────────────────────────────────────
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
        };
    });

builder.Services.AddAuthorization();

// ────────────────────────────────────────────────────────────────────────────
// Rate Limiting – Fixed Window per IP
// ────────────────────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    var cfg = builder.Configuration.GetSection("RateLimiting");

    options.AddFixedWindowLimiter("fixed", limiter =>
    {
        limiter.PermitLimit          = cfg.GetValue("PermitLimit",    100);
        limiter.Window               = TimeSpan.FromSeconds(cfg.GetValue("WindowInSeconds", 60));
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit           = cfg.GetValue("QueueLimit", 5);
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ────────────────────────────────────────────────────────────────────────────
// Infrastructure Services
// ────────────────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<JwtTokenGenerator>();
builder.Services.AddSingleton<PasswordHasher>();

// ────────────────────────────────────────────────────────────────────────────
// Health Checks
// ────────────────────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");

// ────────────────────────────────────────────────────────────────────────────
// Build & Configure Middleware Pipeline
// ────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseExceptionHandler();       // Must be first to catch all downstream exceptions.
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapCarter();                              // Discovers and registers all ICarterModule endpoints.
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

// Expose Program for WebApplicationFactory in integration tests.
public partial class Program { }
