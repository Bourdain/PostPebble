using System.Text;
using System.Text.Json.Serialization;
using Api.Auth;
using Api.Billing;
using Api.Domain;
using Api.Infrastructure;
using Api.LinkedIn;
using Api.Media;
using Api.Scheduler;
using Api.Tenants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection(StripeOptions.SectionName));
builder.Services.Configure<MediaStorageOptions>(builder.Configuration.GetSection(MediaStorageOptions.SectionName));
builder.Services.Configure<LinkedInOptions>(builder.Configuration.GetSection(LinkedInOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var frontendBaseUrl = builder.Configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Postgres");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        connectionString = "Host=localhost;Port=5432;Database=postpebble;Username=postpebble;Password=postpebble_dev_password";
    }

    options.UseNpgsql(connectionString);
});
builder.Services.AddDbContext<BillingDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Postgres");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        connectionString = "Host=localhost;Port=5432;Database=postpebble;Username=postpebble;Password=postpebble_dev_password";
    }

    options.UseNpgsql(connectionString);
});
builder.Services.AddDbContext<MediaDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Postgres");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        connectionString = "Host=localhost;Port=5432;Database=postpebble;Username=postpebble;Password=postpebble_dev_password";
    }

    options.UseNpgsql(connectionString);
});
builder.Services.AddDbContext<SchedulerDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Postgres");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        connectionString = "Host=localhost;Port=5432;Database=postpebble;Username=postpebble;Password=postpebble_dev_password";
    }

    options.UseNpgsql(connectionString);
});
builder.Services.AddDbContext<IntegrationsDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Postgres");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        connectionString = "Host=localhost;Port=5432;Database=postpebble;Username=postpebble;Password=postpebble_dev_password";
    }

    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<CreditLedgerService>();
builder.Services.AddScoped<ITenantAccessService, TenantAccessService>();
builder.Services.AddScoped<IReservationLedgerService>(sp => sp.GetRequiredService<CreditLedgerService>());
builder.Services.AddScoped<StripeWebhookService>();
builder.Services.AddScoped<IMediaStorage, LocalMediaStorage>();
builder.Services.AddHttpClient<LinkedInOAuthService>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        policy.WithOrigins(frontendBaseUrl, "http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            NameClaimType = "sub"
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseCors("FrontendCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "postpebble-api" }));

app.MapGet("/api/v1/system", () =>
    Results.Ok(new
    {
        name = "PostPebble API",
        version = "0.1.0",
        utcNow = DateTime.UtcNow
    }));

app.MapAuthEndpoints();
app.MapTenantEndpoints();
app.MapBillingEndpoints();
app.MapSchedulerEndpoints();
app.MapMediaEndpoints();
app.MapLinkedInEndpoints();

app.Run();
