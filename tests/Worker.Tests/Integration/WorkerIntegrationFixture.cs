using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Worker.Data;
using Worker.Services;
using Worker.Services.Handlers;
using Worker.Settings;
using Worker.Shared.Data;
using Worker.Shared.Interfaces;
using Xunit;


namespace Worker.Tests.Integration;

/// <summary>
/// Shared fixture for Worker integration tests.
/// Builds a real DI container from appsettings.Development.json so tests run
/// against the configured SQL Server database and Hangfire storage.
/// </summary>
public class WorkerIntegrationFixture : IDisposable
{
    public IServiceProvider Services { get; }
    public IConfiguration Configuration { get; }

    public string WorkerDbConnectionString =>
        Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

    public string HangfireConnectionString =>
        Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

    public WorkerIntegrationFixture()
    {
        var connection = Environment.GetEnvironmentVariable("APPROVAL_TEST_CONNECTION") ??
            "Server=localhost;Database=AutomaticApprovalDb_IntegrationTest;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";
        if (!new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection).InitialCatalog.Contains("Test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Integration tests require an isolated Test database.");
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: false)
            .AddEnvironmentVariables()
            .AddInMemoryCollection(new Dictionary<string, string> {
                ["ConnectionStrings:DefaultConnection"] = connection,
                ["Smtp:PickupDirectory"] = Path.Combine(Path.GetTempPath(), "approval-mail-test-" + Guid.NewGuid().ToString("N")),
                ["Smtp:FromAddress"] = "noreply@example.test",
                ["Smtp:EnableSsl"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));

        // IHostEnvironment — ContentRootPath points to the test output directory
        // which has Templates/** copied from src/Worker via the .csproj Content items.
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment
        {
            ContentRootPath = AppContext.BaseDirectory
        });

        // DB
        services.AddDbContext<WorkerDbContext>(options =>
            options
                .UseSqlServer(WorkerDbConnectionString,
                    o => o.MigrationsHistoryTable("__EFMigrationsHistory", "worker"))
                .ConfigureWarnings(w =>
                    w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        // SMTP
        services.Configure<SmtpSettings>(Configuration.GetSection(SmtpSettings.SectionName));

        // Email services
        services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<INotificationEmailHandler, WorkflowEmailHandler>();
        services.AddScoped<INotificationEmailHandler, ResetPasswordEmailHandler>();
        services.AddScoped<NotificationEmailDispatcher>();
        services.AddScoped<IEmailSender, EmailSender>();

        // Hangfire (SQL Server)
        services.AddHangfire(config =>
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(HangfireConnectionString));

        // Enqueue only: tests must never consume jobs or contact a real mail server.

        Services = services.BuildServiceProvider();

        // Apply any pending EF migrations so tests can run against a fresh DB schema
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<WorkerDbContext>().Database.Migrate();

        // Initialize Hangfire's global JobStorage.Current so BackgroundJob.Enqueue (static)
        // works inside EmailNotificationJob.ProcessEmailAsync.
        GlobalConfiguration.Configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(HangfireConnectionString);
    }

    public T Resolve<T>() where T : notnull => Services.GetRequiredService<T>();

    public IServiceScope CreateScope() => Services.CreateScope();

    public void Dispose()
    {
        (Services as IDisposable)?.Dispose();
    }
}

/// <summary>
/// xUnit collection fixture so all integration tests share one DI container.
/// </summary>
[CollectionDefinition("WorkerIntegration")]
public class WorkerIntegrationCollection : ICollectionFixture<WorkerIntegrationFixture> { }

/// <summary>
/// Minimal IHostEnvironment for test wiring — ContentRootPath is all that matters.
/// </summary>
internal sealed class TestHostEnvironment : IHostEnvironment
{
    public string ApplicationName { get; set; } = "Worker.Tests";
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public string EnvironmentName { get; set; } = "Development";
}
