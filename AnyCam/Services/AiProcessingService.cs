using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using AnyCam.Models;
using AnyCam.Services;
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
            _logger.LogInformation("AI Processing Service started");

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

                    _logger.LogInformation($"Found {rtspCameras.Count} RTSP cameras to process");

                    if (rtspCameras.Count == 0)
                    {
                        _logger.LogWarning("No RTSP cameras found. AI processing will not run until cameras with RTSP streams are configured.");
                        await Task.Delay(30000, stoppingToken);
                        continue;
                    }

                    foreach (var camera in rtspCameras)
                    {
                        try
                        {
                            // Capture a frame (simplified - in real app, use FFmpeg or library)
                            var frameBytes = await CaptureFrameAsync(camera.StreamUrl);
                            if (frameBytes != null && frameBytes.Length > 0)
                            {
                                // Analyze with AI
                                var analysis = await aiService.AnalyzeImageAsync(frameBytes);

                                // Check if we should create an event
                                if (await aiService.ShouldCreateEventAsync(frameBytes))
                                {
                                    // Check for recent similar events to avoid duplicates (within last 5 minutes)
                                    var recentEvent = await context.AiEvents
                                        .Where(e => e.CameraId == camera.Id &&
                                                   e.EventType == analysis.EventType &&
                                                   e.Timestamp > DateTime.UtcNow.AddMinutes(-5))
                                        .OrderByDescending(e => e.Timestamp)
                                        .FirstOrDefaultAsync(stoppingToken);

                                    if (recentEvent == null || !IsSimilarEvent(recentEvent, analysis))
                                    {
                                        // Create AI event
                                        var aiEvent = new AiEvent
                                        {
                                            CameraId = camera.Id,
                                            VideoClipId = null, // For live events, no associated clip
                                            EventType = analysis.EventType ?? "Detection",
                                            Description = analysis.Summary,
                                            Timestamp = DateTime.UtcNow,
                                            DetectedObjects = analysis.Objects.Any() ?
                                                JsonSerializer.Serialize(analysis.Objects) : null,
                                            AlertSent = false,
                                            AlertType = DetermineAlertType(analysis),
                                            Confidence = analysis.Confidence.ToString("F2")
                                        };

                                        context.AiEvents.Add(aiEvent);
                                        await context.SaveChangesAsync(stoppingToken);

                                        _logger.LogInformation($"AI event created for camera {camera.Name}: {analysis.EventType} - {analysis.Summary}");
                                    }
                                    else
                                    {
                                        _logger.LogDebug($"Skipping duplicate event for camera {camera.Name}: {analysis.EventType}");
                                    }
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

        private bool IsSimilarEvent(AiEvent recentEvent, AiAnalysisResult newAnalysis)
        {
            // Consider events similar if they have the same type and similar descriptions
            return recentEvent.EventType == newAnalysis.EventType &&
                   recentEvent.Description.Contains(newAnalysis.Summary.Split(' ')[0]); // First word match
        }

        private string? DetermineAlertType(AiAnalysisResult analysis)
        {
            // Determine alert type based on severity
            if (analysis.EventType == "Anomaly" || analysis.PeopleCount > 5)
            {
                return "Email"; // High priority
            }
            else if (analysis.EventType == "Person Detected" || analysis.PeopleCount > 0)
            {
                return "Telegram"; // Medium priority
            }
            else if (analysis.EventType == "Motion")
            {
                return null; // Low priority, no alert
            }

            return "Email"; // Default
        }
    }
}