// TODO add RabbitMQ for async processing of invoice creation and updates

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InvoiceTrackerAPI2.Data;
using InvoiceTrackerAPI2.Mappings;
using InvoiceTrackerAPI2.Repositories;
using InvoiceTrackerAPI2.Repositories.Interfaces;
using InvoiceTrackerAPI2.Services;
using InvoiceTrackerAPI2.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ───────────────────────────────────────────────────────────────────
// Reads configuration from the "Serilog" section in appsettings.json.
// Enriches every log event with a unique RequestId for correlation across services.
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "InvoiceTrackerAPI"));

// ── JWT key validation — fail fast if key is missing or too short ─────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 bytes (256 bits).");

// ── Request size limit — 1 MB cap to prevent large-payload DoS ───────────────
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 1_048_576);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 1_048_576);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// ── Redis ─────────────────────────────────────────────────────────────────────
// IConnectionMultiplexer is thread-safe and expensive to create — singleton is correct
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));
builder.Services.AddSingleton<ITokenRevocationService, TokenRevocationService>();

// ── Auth ──────────────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // After signature/expiry checks pass, reject tokens that have been revoked
        opt.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var revocation = ctx.HttpContext.RequestServices
                    .GetRequiredService<ITokenRevocationService>();

                var jti = ctx.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
                if (jti is not null && await revocation.IsRevokedAsync(jti))
                    ctx.Fail("Token has been revoked.");
            }
        };
    });
builder.Services.AddAuthorization();

// ── Output caching ────────────────────────────────────────────────────────────
builder.Services.AddOutputCache(o =>
{
    // Summary is per-user — vary by Authorization header so users don't see each other's data
    o.AddPolicy("SummaryCache", p => p
        .Expire(TimeSpan.FromSeconds(30))
        .SetVaryByQuery("*")
        .Tag("summary"));
});

// ── Services ──────────────────────────────────────────────────────────────────

builder.Services.AddHostedService<OverdueInvoiceJob>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IInvoiceNumberAllocator, SequentialInvoiceNumberAllocator>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<InvoiceMappingProfile>());

// ── Health checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database")
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!, name: "redis");

// CORS is not needed — nginx sits in front of both the frontend and API on the
// same origin, so the browser never makes a cross-origin request to the API directly.
// var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "")
//     .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
//
// builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
//     p.WithOrigins(allowedOrigins)
//      .AllowAnyHeader()
//      .AllowAnyMethod()));

builder.Services.AddControllers()
    .AddJsonOptions(opt =>
        opt.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddOpenApi();

var app = builder.Build();

// ── Ensure database schema exists ────────────────────────────────────────────
// EnsureCreated creates all tables from the model if they don't exist.
// It does nothing if the database already has tables — safe to call on every startup.
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
app.UseExceptionHandler(err => err.Run(async ctx =>
{
    ctx.Response.StatusCode  = StatusCodes.Status500InternalServerError;
    ctx.Response.ContentType = "application/json";
    await ctx.Response.WriteAsJsonAsync(new { message = "An unexpected error occurred." });
}));

// ── Security headers ──────────────────────────────────────────────────────────
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"]        = "DENY";
    ctx.Response.Headers["Referrer-Policy"]        = "strict-origin-when-cross-origin";
    ctx.Response.Headers["Permissions-Policy"]     = "geolocation=(), microphone=(), camera=()";
    await next();
});

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseSerilogRequestLogging();
app.UseOutputCache();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Health / liveness / readiness endpoints ───────────────────────────────────
// GET /healthz          → liveness  (is the process alive?)
// GET /healthz/ready    → readiness (is the DB reachable?)
// GET /healthz/live     → liveness  (alias, for k8s probes)
app.MapHealthChecks("/healthz/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready") || check.Name == "database",
    ResponseWriter = WriteHealthResponse
});

app.MapHealthChecks("/healthz/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false, // liveness = process is up, no checks needed
    ResponseWriter = WriteHealthResponse
});

app.MapHealthChecks("/healthz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = WriteHealthResponse
});

app.Run();

static Task WriteHealthResponse(HttpContext ctx, Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
{
    ctx.Response.ContentType = "application/json";
    var result = System.Text.Json.JsonSerializer.Serialize(new
    {
        status  = report.Status.ToString(),
        checks  = report.Entries.Select(e => new
        {
            name     = e.Key,
            status   = e.Value.Status.ToString(),
            duration = e.Value.Duration.TotalMilliseconds + "ms",
            error    = e.Value.Exception?.Message
        })
    });
    return ctx.Response.WriteAsync(result);
}
