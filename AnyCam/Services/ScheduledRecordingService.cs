using System.Diagnostics;
using AnyCam.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace AnyCam.Services
{
    public class ScheduledRecordingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ScheduledRecordingService> _logger;

        public ScheduledRecordingService(IServiceProvider serviceProvider, ILogger<ScheduledRecordingService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var now = DateTime.UtcNow;

                    // Find scheduled clips that should be recording now
                    var scheduledClips = await context.VideoClips
                        .Where(v => v.StartTime <= now && v.EndTime > now && string.IsNullOrEmpty(v.FilePath))
                        .Include(v => v.Camera)
                        .ToListAsync(stoppingToken);

                    _logger.LogInformation($"Found {scheduledClips.Count} scheduled clips to record: {string.Join(", ", scheduledClips.Select(c => c.Id))}");

                    foreach (var clip in scheduledClips)
                    {
                        if (clip.Camera?.StreamUrl?.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "videos");
                            Directory.CreateDirectory(outputPath);
                            var fileName = $"{clip.Id}_{clip.StartTime:yyyyMMddHHmmss}.mp4";
                            var filePath = Path.Combine(outputPath, fileName);

                            var duration = (int)(clip.EndTime - now).TotalSeconds;
                            if (duration > 0)
                            {
                                var process = new Process
                                {
                                    StartInfo = new ProcessStartInfo
                                    {
                                        FileName = "ffmpeg",
                                        Arguments = $"-i \"{clip.Camera.StreamUrl}\" -t {duration} -c copy \"{filePath}\"",
                                        UseShellExecute = false,
                                        RedirectStandardOutput = true,
                                        RedirectStandardError = true,
                                        CreateNoWindow = true
                                    }
                                };

                                process.Start();
                                process.WaitForExit();

                                if (process.ExitCode == 0 && File.Exists(filePath))
                                {
                                    clip.FilePath = $"/videos/{fileName}";
                                    clip.FileSize = new FileInfo(filePath).Length;
                                    clip.StorageType = "Local";
                                    await context.SaveChangesAsync(stoppingToken);
                                    _logger.LogInformation($"Scheduled recording completed for clip {clip.Id}: {filePath}");
                                }
                                else
                                {
                                    _logger.LogError($"Scheduled recording failed for clip {clip.Id}: Exit code {process.ExitCode}");
                                }
                            }
                        }
                    }
                }

                // Check every minute
                await Task.Delay(60000, stoppingToken);
            }
        }
    }
}