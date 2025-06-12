using Microsoft.Extensions.DependencyInjection;
using RAppsAPI.Data;
using RAppsAPI.Models.MPM;
using Serilog;
using static RAppsAPI.Data.Constants;

namespace RAppsAPI.Services
{
    public class MPMMonitoringService: IHostedService, IDisposable
    {
        private const int MONITOR_INTERVAL_SECS = 300;        
        
        private int executionCount = 0;
        private readonly ILogger<MPMMonitoringService> _logger;
        private readonly IMPMBackgroundRequestQueue _reqQueue;
        private Timer? _timer = null;

        public MPMMonitoringService(ILogger<MPMMonitoringService> logger,
            IMPMBackgroundRequestQueue reqQueue)
        {
            _logger = logger;
            _reqQueue = reqQueue;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            Log.Information("MPMMonitoringService:StartAsync: Hosted Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(MONITOR_INTERVAL_SECS));

            return Task.CompletedTask;
        }

        private void DoWork(object? state)
        {
            var count = Interlocked.Increment(ref executionCount);

            Log.Information(
                "MPMMonitoringService:DoWork: Hosted Service is working. Count: {Count}", count);
            // Queue file writing check command into queue every time this func is called
            _reqQueue.QueueBackgroundRequest(new()
            {
                UserId = DBConstants.ADMIN_USER_ID,
                Command = BGQueueCmd.WriteFiles
            });
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MPMMonitoringService:StopAsync: Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
