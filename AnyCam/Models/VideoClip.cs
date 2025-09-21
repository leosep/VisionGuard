using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AnyCam.Models
{
    public class VideoClip
    {
        public VideoClip()
        {
            AiEvents = new List<AiEvent>();
        }

        public int Id { get; set; }

        [Required]
        public int CameraId { get; set; }

        [ValidateNever]
        public Camera Camera { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public string? FilePath { get; set; } // Local or cloud path

        public string? StorageType { get; set; } // Local, AWS S3

        public long FileSize { get; set; } // in bytes

        // AI events associated
        public ICollection<AiEvent> AiEvents { get; set; }
    }
}