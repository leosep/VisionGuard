using System.ComponentModel.DataAnnotations;

namespace AnyCam.Models
{
    public class Camera
    {
        public Camera()
        {
            VideoClips = new List<VideoClip>();
        }

        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string IpAddress { get; set; }

        public int Port { get; set; } = 554; // Default RTSP port

        public string? Username { get; set; }
    
        public string? Password { get; set; }

        public string StreamType { get; set; } // RTSP, HTTP, etc.

        public string Protocol { get; set; } // ONVIF, RTSP, HTTP
    
        public string? StreamUrl { get; set; } // Full RTSP/HTTP URL
    
        public bool IsOnline { get; set; } = false;
    
        public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    
        // Navigation
        public ICollection<VideoClip> VideoClips { get; set; }
    }
}