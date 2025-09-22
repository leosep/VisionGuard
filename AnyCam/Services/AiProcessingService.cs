using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using AnyCam.Models;
using Microsoft.EntityFrameworkCore;
using Emgu.CV;
using Emgu.CV.Structure;

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
                                        VideoClipId = null, // For live events, no associated clip
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
            _logger.LogInformation($"Capturing frame from {rtspUrl}");
            try
            {
                using var capture = new VideoCapture(rtspUrl);
                if (!capture.IsOpened)
                {
                    _logger.LogWarning($"Failed to open stream {rtspUrl}");
                    return Array.Empty<byte>();
                }

                using var frame = new Mat();
                if (capture.Read(frame) && !frame.IsEmpty)
                {
                    var jpgData = frame.ToImage<Bgr, byte>().ToJpegData();
                    return jpgData;
                }
                else
                {
                    _logger.LogWarning($"Failed to read frame from {rtspUrl}");
                    return Array.Empty<byte>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error capturing frame from {rtspUrl}");
                return Array.Empty<byte>();
            }
        }
    }
}