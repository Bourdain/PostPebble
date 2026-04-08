using Api.Billing;
using Api.Infrastructure;
using Api.LinkedIn;
using Api.Scheduler;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<LinkedInOptions>(builder.Configuration.GetSection(LinkedInOptions.SectionName));
builder.Services.Configure<SchedulerWorkerOptions>(builder.Configuration.GetSection(SchedulerWorkerOptions.SectionName));

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("Postgres");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        connectionString = "Host=localhost;Port=5432;Database=postpebble;Username=postpebble;Password=postpebble_dev_password";
    }

    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<CreditLedgerService>();
builder.Services.AddHttpClient<LinkedInPublisher>();
builder.Services.AddHostedService<SchedulerPublishWorker>();

var app = builder.Build();
app.Run();
