using Hangfire;
using Klasly.Infrastructure;
using Klasly.Worker.Email;
using Klasly.Worker.Jobs;
using Klasly.Worker.Services;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Klasly")
    ?? throw new InvalidOperationException("Connection string 'Klasly' is missing.");
var cron = builder.Configuration["Worker:Cron"]
    ?? throw new InvalidOperationException("Setting 'Worker:Cron' is missing.");

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddScoped<AttendanceFinalizer>();
builder.Services.AddScoped<RiskCalculator>();
builder.Services.AddScoped<AlertQueuer>();
builder.Services.AddScoped<EmailSender>();

// Brevo:ApiKey comes from user-secrets (Development) or the environment; never from appsettings.
builder.Services.Configure<BrevoOptions>(builder.Configuration.GetSection("Brevo"));
builder.Services.AddHttpClient<IBrevoEmailClient, BrevoEmailClient>((sp, http) =>
{
    http.BaseAddress = new Uri(sp.GetRequiredService<IOptions<BrevoOptions>>().Value.BaseUrl);
    http.Timeout = TimeSpan.FromSeconds(10); // 50 sends × 10 s stays under the job's 600 s lock
});

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
