using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using AnyCam.Models;
using Microsoft.EntityFrameworkCore;

namespace AnyCam.Services
{
    public class AiProcessingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AiProcessingService> _logger;

        public AiProcessingService(IServiceProvider serviceProvider, ILogger<AiProcessingService> logger)
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
                    var aiService = scope.ServiceProvider.GetRequiredService<AiService>();

                    // Get cameras with RTSP streams
                    var rtspCameras = await context.Cameras
                        .Where(c => c.StreamUrl != null && c.StreamUrl.StartsWith("rtsp://"))
                        .ToListAsync(stoppingToken);

                    foreach (var camera in rtspCameras)
                    {
                        try
                        {
                            // Capture a frame (simplified - in real app, use FFmpeg or library)
                            var frameBytes = await CaptureFrameAsync(camera.StreamUrl);
                            if (frameBytes != null)
                            {
                                // Analyze with AI
                                var analysis = await aiService.AnalyzeImageAsync(frameBytes);

                                // Check for anomalies
                                if (await aiService.IsAnomalyAsync(frameBytes))
                                {
                                    // Create AI event
                                    var aiEvent = new AiEvent
                                    {
                                        VideoClipId = 0, // For live, perhaps create a temp clip or use null
                                        EventType = "Anomaly Detected",
                                        Description = analysis,
                                        Timestamp = DateTime.UtcNow,
                                        DetectedObjects = analysis,
                                        AlertSent = false,
                                        AlertType = "Email" // Or Telegram
                                    };

                                    context.AiEvents.Add(aiEvent);
                                    await context.SaveChangesAsync(stoppingToken);

                                    _logger.LogInformation($"Anomaly detected for camera {camera.Name}: {analysis}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error processing AI for camera {camera.Name}");
                        }
                    }
                }

                // Wait 30 seconds before next check
                await Task.Delay(30000, stoppingToken);
            }
        }

        private async Task<byte[]> CaptureFrameAsync(string rtspUrl)
        {
            // Simplified: In real implementation, use FFmpeg to capture a single frame
            // For demo, return dummy data or implement actual capture
            _logger.LogInformation($"Capturing frame from {rtspUrl}");
            // Placeholder: return empty byte array
            return Array.Empty<byte>();
        }
    }
}