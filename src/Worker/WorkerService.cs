namespace Worker
{

    public class WorkerService(ILogger<WorkerService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Email Worker Service started at: {time}", DateTimeOffset.Now);

            // Hangfire server runs in the background via AddHangfireServer().
            // This hosted service keeps the host alive and can be used for
            // additional startup/shutdown logic if needed.
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Email Worker Service stopping at: {time}", DateTimeOffset.Now);
            await base.StopAsync(cancellationToken);
        }
    }
}
