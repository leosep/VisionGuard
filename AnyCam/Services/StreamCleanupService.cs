using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace AnyCam.Services
{
    public class StreamCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StreamCleanupService> _logger;

        public StreamCleanupService(IServiceProvider serviceProvider, ILogger<StreamCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var streamingService = scope.ServiceProvider.GetRequiredService<StreamingService>();
                    var now = DateTime.UtcNow;
                    var timeout = TimeSpan.FromMinutes(5);
                    var toStop = new List<int>();

                    foreach (var kvp in streamingService.GetRunningStreams())
                    {
                        if (now - kvp.Value.LastAccessed > timeout)
                        {
                            toStop.Add(kvp.Key);
                        }
                    }

                    foreach (var id in toStop)
                    {
                        streamingService.StopHlsStream(id);
                        _logger.LogInformation($"Stopped inactive stream for camera {id}");
                    }
                }
            }
        }
    }
}