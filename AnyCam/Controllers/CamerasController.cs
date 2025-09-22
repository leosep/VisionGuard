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
            // Update online status for all cameras
            foreach (var camera in cameras)
            {
                camera.IsOnline = await _cameraService.CheckOnlineAsync(camera);
                camera.LastChecked = DateTime.UtcNow;
            }
            _context.UpdateRange(cameras);
            await _context.SaveChangesAsync();
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

            _logger.LogInformation($"Camera StreamUrl: {camera.StreamUrl}");

            // If RTSP, start HLS streaming
            if (camera.StreamUrl?.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase) == true)
            {
                var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "streams", id.ToString());
                Directory.CreateDirectory(outputPath);
                _streamingService.StartHlsStream(camera.StreamUrl, outputPath, camera.Id);
                ViewBag.HlsUrl = $"/streams/{id}/playlist.m3u8";
                // Wait longer for FFmpeg to start and create playlist
                await Task.Delay(15000);
                var playlistPath = Path.Combine(outputPath, "playlist.m3u8");
                if (System.IO.File.Exists(playlistPath))
                {
                    Console.WriteLine("Playlist created successfully");
                }
                else
                {
                    Console.WriteLine("Playlist not found");
                    TempData["Error"] = "Failed to start stream. Camera may be offline.";
                }
            }

            return View(camera);
        }

        // GET: Cameras/Wall
        public async Task<IActionResult> Wall()
        {
            var cameras = await _context.Cameras.ToListAsync();
            // Start HLS for RTSP cameras
            foreach (var camera in cameras.Where(c => c.StreamUrl?.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase) == true))
            {
                var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "streams", camera.Id.ToString());
                Directory.CreateDirectory(outputPath);
                _streamingService.StartHlsStream(camera.StreamUrl, outputPath, camera.Id);
            }
            // Wait for streams to start
            await Task.Delay(10000);
            return View(cameras);
        }

        // POST: Cameras/StopStreaming/5
        [HttpPost]
        public IActionResult StopStreaming(int id)
        {
            _streamingService.StopHlsStream(id);
            return Ok();
        }

        // POST: Cameras/StopAllStreams
        [HttpPost]
        public IActionResult StopAllStreams()
        {
            _streamingService.StopAllStreams();
            return Ok();
        }

        // GET: Cameras/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Cameras/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,IpAddress,Port,Username,Password,StreamType,Protocol,StreamUrl,IsOnline,LastChecked")] Camera camera)
        {
            _logger.LogInformation($"Creating camera with StreamUrl: {camera.StreamUrl}");
            if (!string.IsNullOrEmpty(camera.StreamUrl))
            {
                // Parse the URL to extract IP, Port, Protocol
                try
                {
                    var uri = new Uri(camera.StreamUrl);
                    camera.IpAddress = uri.Host;
                    camera.Port = uri.Port;
                    camera.Protocol = uri.Scheme.ToUpper();
                }
                catch
                {
                    ModelState.AddModelError("StreamUrl", "Invalid URL format.");
                }
            }

            if (ModelState.IsValid)
            {
                // Auto-detect protocol if not set
                if (string.IsNullOrEmpty(camera.Protocol))
                {
                    camera.Protocol = await _cameraService.DetectProtocolAsync(camera.IpAddress, camera.Port);
                }
                // Check online status
                camera.IsOnline = await _cameraService.CheckOnlineAsync(camera);
                camera.LastChecked = DateTime.UtcNow;

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

            if (!camera.StreamUrl?.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase) == true)
            {
                TempData["Error"] = "Recording only supported for RTSP streams.";
                return RedirectToAction(nameof(View), new { id });
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

            // Start FFmpeg recording with timeout
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"{camera.StreamUrl}\" -t {Math.Min(durationMinutes, 10) * 60} -fs 100M \"{filePath}\"", // Limit to 10 min, 100MB
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            bool exited = process.WaitForExit(600000); // 10 min timeout
            if (!exited)
            {
                process.Kill();
                TempData["Error"] = "Recording timed out.";
                return RedirectToAction(nameof(View), new { id });
            }

            if (process.ExitCode == 0)
            {
                // Create VideoClip
                var videoClip = new VideoClip
                {
                    CameraId = camera.Id,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow.AddMinutes(durationMinutes),
                    FilePath = $"/videos/{fileName}",
                    StorageType = "Local",
                    FileSize = new FileInfo(filePath).Length
                };

                _context.VideoClips.Add(videoClip);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Recording completed. Clip saved: {fileName}";
            }
            else
            {
                TempData["Error"] = "Recording failed.";
            }

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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,IpAddress,Port,Username,Password,StreamType,Protocol,StreamUrl,IsOnline,LastChecked")] Camera camera)
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