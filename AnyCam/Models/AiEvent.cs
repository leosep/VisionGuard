using System.ComponentModel.DataAnnotations;

namespace AnyCam.Models
{
    public class AiEvent
    {
        public int Id { get; set; }

        public int? VideoClipId { get; set; }

        public VideoClip? VideoClip { get; set; }

        [Required]
        public int CameraId { get; set; } // Camera that triggered the event

        public Camera? Camera { get; set; }

        [Required]
        public string EventType { get; set; } // e.g., "Motion", "Person Detected", "Anomaly"

        [Required]
        public string Description { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        public string? DetectedObjects { get; set; } // JSON string of detected objects

        public bool AlertSent { get; set; } = false;

        public string? AlertType { get; set; } // Email, SMS, Telegram

        public string? Confidence { get; set; } // AI confidence level
    }
}