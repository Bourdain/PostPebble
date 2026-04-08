using Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration["ConnectionStrings:Postgres"];
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        connectionString = "Host=localhost;Port=5432;Database=postpebble;Username=postpebble;Password=postpebble_dev_password";
    }

    options.UseNpgsql(connectionString);
});

using var host = builder.Build();
using var scope = host.Services.CreateScope();

var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
dbContext.Database.Migrate();

Console.WriteLine("Database migrations applied successfully.");
