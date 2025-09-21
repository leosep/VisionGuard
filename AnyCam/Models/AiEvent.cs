using System.ComponentModel.DataAnnotations;

namespace AnyCam.Models
{
    public class AiEvent
    {
        public int Id { get; set; }

        [Required]
        public int VideoClipId { get; set; }

        public VideoClip VideoClip { get; set; }

        [Required]
        public string EventType { get; set; } // e.g., "Motion", "Person Detected", "Anomaly"

        public string Description { get; set; }

        public DateTime Timestamp { get; set; }

        public string DetectedObjects { get; set; } // JSON string of detected objects

        public bool AlertSent { get; set; } = false;

        public string AlertType { get; set; } // Email, SMS, Telegram
    }
}