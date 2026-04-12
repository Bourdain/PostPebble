using Api.Billing;
using Api.Infrastructure;
using Api.LinkedIn;
using Api.Media;
using Api.Scheduler;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<LinkedInOptions>(builder.Configuration.GetSection(LinkedInOptions.SectionName));
builder.Services.Configure<SchedulerWorkerOptions>(builder.Configuration.GetSection(SchedulerWorkerOptions.SectionName));
builder.Services.Configure<MediaStorageOptions>(builder.Configuration.GetSection(MediaStorageOptions.SectionName));

builder.Services.AddDbContext<SchedulerDbContext>(options =>
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
builder.Services.AddDbContext<IntegrationsDbContext>(options =>
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

builder.Services.AddScoped<CreditLedgerService>();
builder.Services.AddScoped<IReservationLedgerService>(sp => sp.GetRequiredService<CreditLedgerService>());
builder.Services.AddScoped<IMediaStorage, LocalMediaStorage>();
builder.Services.AddHttpClient<LinkedInPublisher>();
builder.Services.AddHostedService<SchedulerPublishWorker>();

var app = builder.Build();
app.Run();

