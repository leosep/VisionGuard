using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AnyCam.Models;
using Microsoft.AspNetCore.Authorization;
using AnyCam.Services;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;

namespace AnyCam.Controllers
{
    [Authorize]
    public class CamerasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CameraService _cameraService;
        private readonly StreamingService _streamingService;
        private readonly ILogger<CamerasController> _logger;

        public CamerasController(ApplicationDbContext context, CameraService cameraService, StreamingService streamingService, ILogger<CamerasController> logger)
        {
            _context = context;
            _cameraService = cameraService;
            _streamingService = streamingService;
            _logger = logger;
        }

        // GET: Cameras
        public async Task<IActionResult> Index()
        {
            var cameras = await _context.Cameras.ToListAsync();
            return View(cameras);
        }

        // GET: Cameras/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var camera = await _context.Cameras
                .FirstOrDefaultAsync(m => m.Id == id);
            if (camera == null)
            {
                return NotFound();
            }

            return View(camera);
        }

        // GET: Cameras/View/5
        public async Task<IActionResult> View(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var camera = await _context.Cameras.FindAsync(id);
            if (camera == null)
            {
                return NotFound();
            }

            return View(camera);
        }

        // GET: Cameras/Wall
        public async Task<IActionResult> Wall()
        {
            var cameras = await _context.Cameras.ToListAsync();
            var recentAiEvents = await _context.AiEvents
                .Include(e => e.Camera)
                .OrderByDescending(e => e.Timestamp)
                .Take(10)
                .ToListAsync();
            var model = new WallViewModel { Cameras = cameras, RecentAiEvents = recentAiEvents };
            return View(model);
        }

        // POST: Cameras/StopStreaming/5
        [HttpPost]
        public IActionResult StopStreaming(int id)
        {
            _streamingService.StopStream(id);
            return Ok();
        }

        // GET: Cameras/Stream/5
        public async Task<IActionResult> Stream(int id)
        {
            var camera = await _context.Cameras.FindAsync(id);
            if (camera == null || string.IsNullOrEmpty(camera.StreamUrl))
            {
                return NotFound();
            }

            var response = Response;
            response.ContentType = "multipart/x-mixed-replace; boundary=frame";

            try
            {
                using var capture = new VideoCapture(camera.StreamUrl);
                if (!capture.IsOpened)
                {
                    return BadRequest("Cannot open stream");
                }

                await using var writer = new StreamWriter(response.Body);

                while (!HttpContext.RequestAborted.IsCancellationRequested)
                {
                    using var frame = new Mat();
                    if (capture.Read(frame) && !frame.IsEmpty)
                    {
                        var jpgFrame = frame.ToImage<Bgr, byte>().ToJpegData();
                        await writer.WriteLineAsync("--frame");
                        await writer.WriteLineAsync("Content-Type: image/jpeg");
                        await writer.WriteLineAsync($"Content-Length: {jpgFrame.Length}");
                        await writer.WriteLineAsync();
                        await writer.FlushAsync();
                        await response.Body.WriteAsync(jpgFrame);
                        await writer.WriteLineAsync();
                        await writer.FlushAsync();
                    }
                    else
                    {
                        await Task.Delay(100); // wait a bit if no frame
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in stream");
                return StatusCode(500, "Stream error");
            }

            return new EmptyResult();
        }

        // POST: Cameras/StopAllStreams
        [HttpPost]
        public IActionResult StopAllStreams()
        {
            _streamingService.StopAllStreams();
            return Ok();
        }

        // API endpoints for wall
        [HttpPost("api/v1/VideoStreaming/start/{cameraId}")]
        public IActionResult StartStream(int cameraId)
        {
            // Assuming streaming service handles starting
            // For now, just return ok
            return Ok();
        }

        [HttpPost("api/v1/VideoStreaming/stop/{cameraId}")]
        public IActionResult StopStream(int cameraId)
        {
            _streamingService.StopStream(cameraId);
            return Ok();
        }

        [HttpGet("api/v1/VideoStreaming/frame/{cameraId}")]
        public async Task<IActionResult> GetFrame(int cameraId)
        {
            var camera = await _context.Cameras.FindAsync(cameraId);
            if (camera == null || string.IsNullOrEmpty(camera.StreamUrl))
            {
                return NotFound();
            }

            try
            {
                using var capture = new VideoCapture(camera.StreamUrl);
                if (!capture.IsOpened)
                {
                    return BadRequest("Cannot open stream");
                }

                using var frame = new Mat();
                if (capture.Read(frame) && !frame.IsEmpty)
                {
                    var jpgFrame = frame.ToImage<Bgr, byte>().ToJpegData();
                    return File(jpgFrame, "image/jpeg");
                }
                else
                {
                    return StatusCode(500, "No frame available");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting frame");
                return StatusCode(500, "Frame error");
            }
        }

        [HttpGet("api/v1/Cameras/{cameraId}/stats")]
        public async Task<IActionResult> GetStats(int cameraId)
        {
            var camera = await _context.Cameras.FindAsync(cameraId);
            if (camera == null)
            {
                return NotFound();
            }

            // Count AI events for this camera
            var detectionCount = await _context.AiEvents
                .Where(e => e.CameraId == cameraId)
                .CountAsync();

            var alertCount = await _context.AiEvents
                .Where(e => e.CameraId == cameraId && e.AlertSent)
                .CountAsync();

            return Json(new { detectionCount, alertCount });
        }

        // GET: Cameras/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Cameras/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Location,StreamUrl")] Camera camera)
        {
            if (ModelState.IsValid)
            {
                _context.Add(camera);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(camera);
        }

        // POST: Cameras/StartRecording/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartRecording(int id, int durationMinutes = 5)
        {
            var camera = await _context.Cameras.FindAsync(id);
            if (camera == null)
            {
                return NotFound();
            }

            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "videos");
            Directory.CreateDirectory(outputPath);
            var fileName = $"{camera.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.mp4";
            var filePath = Path.Combine(outputPath, fileName);

            // Check if file already exists or recording in progress
            if (System.IO.File.Exists(filePath))
            {
                TempData["Error"] = "Recording already exists.";
                return RedirectToAction(nameof(View), new { id });
            }

            var startTime = DateTime.UtcNow;

            // Start EmguCV recording with timeout
            using var capture = new VideoCapture(camera.StreamUrl);
            if (!capture.IsOpened)
            {
                TempData["Error"] = "Failed to open camera stream.";
                return RedirectToAction(nameof(View), new { id });
            }

            double fps = capture.Get(Emgu.CV.CvEnum.CapProp.Fps);
            if (fps <= 0) fps = 30;

            int width = (int)capture.Get(Emgu.CV.CvEnum.CapProp.FrameWidth);
            int height = (int)capture.Get(Emgu.CV.CvEnum.CapProp.FrameHeight);

            using var writer = new VideoWriter(filePath, VideoWriter.Fourcc('H', '2', '6', '4'), fps, new Size(width, height), true);
            if (!writer.IsOpened)
            {
                TempData["Error"] = "Failed to create video writer.";
                return RedirectToAction(nameof(View), new { id });
            }

            int maxDuration = Math.Min(durationMinutes, 10) * 60; // seconds

            while ((DateTime.UtcNow - startTime).TotalSeconds < maxDuration)
            {
                using var frame = new Mat();
                if (capture.Read(frame) && !frame.IsEmpty)
                {
                    writer.Write(frame);
                }
                else
                {
                    TempData["Error"] = "Failed to read frames.";
                    return RedirectToAction(nameof(View), new { id });
                }
            }

            writer.Dispose();

            // Create VideoClip
            var videoClip = new VideoClip
            {
                CameraId = camera.Id,
                StartTime = startTime,
                EndTime = startTime.AddMinutes(durationMinutes),
                FilePath = $"/videos/{fileName}",
                StorageType = "Local",
                FileSize = new FileInfo(filePath).Length
            };

            _context.VideoClips.Add(videoClip);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Recording completed. Clip saved: {fileName}";

            return RedirectToAction(nameof(View), new { id });
        }

        // GET: Cameras/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var camera = await _context.Cameras.FindAsync(id);
            if (camera == null)
            {
                return NotFound();
            }
            return View(camera);
        }

        // POST: Cameras/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Location,StreamUrl")] Camera camera)
        {
            if (id != camera.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(camera);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CameraExists(camera.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(camera);
        }

        // GET: Cameras/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var camera = await _context.Cameras
                .FirstOrDefaultAsync(m => m.Id == id);
            if (camera == null)
            {
                return NotFound();
            }

            return View(camera);
        }

        // POST: Cameras/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var camera = await _context.Cameras.FindAsync(id);
            if (camera != null)
            {
                _context.Cameras.Remove(camera);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CameraExists(int id)
        {
            return _context.Cameras.Any(e => e.Id == id);
        }
    }
}