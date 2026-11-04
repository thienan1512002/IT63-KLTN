namespace Worker.Configuration;

/// <summary>
/// Registers process-wide unhandled exception handlers that are appropriate
/// for a generic Worker host (no HTTP pipeline).
/// </summary>
public sealed class GlobalExceptionHandlerService : IHostedService
{
    private readonly ILogger<GlobalExceptionHandlerService> _logger;

    public GlobalExceptionHandlerService(ILogger<GlobalExceptionHandlerService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        return Task.CompletedTask;
    }

    // Fires when an exception is thrown on a thread with no handler — process is about to crash.
    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            _logger.LogCritical(ex, "Unhandled exception. IsTerminating: {IsTerminating}", e.IsTerminating);
        else
            _logger.LogCritical("Unhandled non-Exception object: {Obj}", e.ExceptionObject);
    }

    // Fires when a faulted Task is garbage-collected without its exception being observed.
    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "Unobserved task exception");
        e.SetObserved(); // prevents process termination in .NET
    }
}
