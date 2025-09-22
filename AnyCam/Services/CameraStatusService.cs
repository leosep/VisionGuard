using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AnyCam.Models;
using Microsoft.EntityFrameworkCore;

namespace AnyCam.Services
{
    public class CameraStatusService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly ILogger<CameraStatusService> _logger;

        public CameraStatusService(IServiceProvider serviceProvider, ILogger<CameraStatusService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _logger.LogInformation("CameraStatusService constructor called");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CameraStatusService starting");
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var cameraService = scope.ServiceProvider.GetRequiredService<CameraService>();
                    var cameras = await context.Cameras.ToListAsync();

                    var tasks = cameras.Select(async camera =>
                    {
                        camera.IsOnline = await cameraService.CheckOnlineAsync(camera);
                        camera.LastChecked = DateTime.UtcNow;
                    });

                    await Task.WhenAll(tasks);

                    await context.SaveChangesAsync();
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Check every 5 minutes
            }
        }
    }
}