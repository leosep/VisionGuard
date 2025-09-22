using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IO;

namespace AnyCam.Controllers
{
    [Authorize]
    public class StreamingController : Controller
    {
        [HttpGet("/streams/{cameraId}/playlist.m3u8")]
        public IActionResult GetPlaylist(int cameraId)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "streams", cameraId.ToString(), "playlist.m3u8");
            if (System.IO.File.Exists(path))
            {
                return PhysicalFile(path, "application/vnd.apple.mpegurl");
            }
            // Try .tmp
            var tmpPath = path + ".tmp";
            if (System.IO.File.Exists(tmpPath))
            {
                return PhysicalFile(tmpPath, "application/vnd.apple.mpegurl");
            }
            return NotFound();
        }

        [HttpGet("/streams/{cameraId}/{segment}")]
        public IActionResult GetSegment(int cameraId, string segment)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "streams", cameraId.ToString(), segment);
            if (System.IO.File.Exists(path))
            {
                return PhysicalFile(path, "video/MP2T");
            }
            return NotFound();
        }
    }
}