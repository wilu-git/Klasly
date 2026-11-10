using Hangfire;
using Klasly.Infrastructure;
using Klasly.Worker.Jobs;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Klasly")
    ?? throw new InvalidOperationException("Connection string 'Klasly' is missing.");
var cron = builder.Configuration["Worker:Cron"]
    ?? throw new InvalidOperationException("Setting 'Worker:Cron' is missing.");

builder.Services.AddInfrastructure(connectionString);

// Hangfire keeps its own tables in the HangFire schema of the Klasly database (created on first run).
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString));
builder.Services.AddHangfireServer();

var host = builder.Build();

// Re-registered on every start, so a changed Worker:Cron takes effect after a restart. Cron runs in UTC.
host.Services.GetRequiredService<IRecurringJobManager>()
    .AddOrUpdate<WorkerTickJob>("worker-tick", job => job.RunAsync(CancellationToken.None), cron);

host.Run();
