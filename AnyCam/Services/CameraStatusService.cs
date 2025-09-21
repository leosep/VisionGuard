using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AnyCam.Models;
using Microsoft.EntityFrameworkCore;

namespace AnyCam.Services
{
    public class CameraStatusService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public CameraStatusService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            Console.WriteLine("CameraStatusService constructor called");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("CameraStatusService starting");
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var cameraService = scope.ServiceProvider.GetRequiredService<CameraService>();
                    var cameras = await context.Cameras.ToListAsync();

                    foreach (var camera in cameras)
                    {
                        camera.IsOnline = await cameraService.CheckOnlineAsync(camera);
                        camera.LastChecked = DateTime.UtcNow;
                    }

                    await context.SaveChangesAsync();
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Check every 1 minute
            }
        }
    }
}