using System.ComponentModel.DataAnnotations;

namespace AnyCam.Models
{
    public class Camera
    {
        public Camera()
        {
            VideoClips = new List<VideoClip>();
            AiEvents = new List<AiEvent>();
        }

        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Location { get; set; }

        public string? StreamUrl { get; set; } // Full RTSP/HTTP URL

        public bool IsOnline { get; set; } = false;

        public DateTime LastChecked { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<VideoClip> VideoClips { get; set; }
        public ICollection<AiEvent> AiEvents { get; set; }
    }
}