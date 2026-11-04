using Hangfire;
using Microsoft.EntityFrameworkCore;
using Worker;
using Worker.Configuration;
using Worker.Data;
using Worker.Jobs;
using Worker.Services;
using Worker.Services.Handlers;
using Worker.Settings;
using Worker.Shared.Interfaces;
using Serilog;
using Microsoft.Extensions.Options;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

// Bootstrap logger — captures errors before the host is fully built (e.g. bad connection string).
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/worker-bootstrap.txt")
    .CreateBootstrapLogger();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "IKSN Worker Service";
});

// Wire Serilog into ILogger<T> using appsettings.json config.
builder.Services.AddSerilog((services, lc) => lc
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// --- Configuration ---
builder.Services.AddSingleton<IValidateOptions<SmtpSettings>, SmtpSettingsValidator>();
builder.Services.AddOptions<SmtpSettings>()
    .Bind(builder.Configuration.GetSection(SmtpSettings.SectionName))
    .ValidateOnStart();

// --- Database ---
builder.Services.AddDbContext<WorkerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.MigrationsHistoryTable("__EFMigrationsHistory", "worker")));


// --- Database ---
builder.Services.AddDbContext<WebDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// --- Hangfire ---
builder.Services.AddHangfire(config =>
    config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();

// --- Infrastructure services ---
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddSingleton<IEmailTemplateService, EmailTemplateService>();

// --- Notification handlers (add new handlers here) ---
builder.Services.AddScoped<INotificationEmailHandler, WorkflowEmailHandler>();
builder.Services.AddScoped<INotificationEmailHandler, ResetPasswordEmailHandler>();
builder.Services.AddScoped<INotificationEmailHandler, UserStatusEmailHandler>();
builder.Services.AddScoped<NotificationEmailDispatcher>();

builder.Services.AddScoped<IEmailJob, EmailNotificationJob>();
builder.Services.AddScoped<EmailReminderJob>();
builder.Services.AddScoped<IUserStatusJob, UserStatusJob>();

// --- Background worker ---
builder.Services.AddHostedService<GlobalExceptionHandlerService>(); // must be first
builder.Services.AddHostedService<WorkerService>();

var host = builder.Build();

try
{
    host.Run();
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    Log.Fatal(ex, "Worker host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
