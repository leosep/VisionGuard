using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace AnyCam.Models
{
    public class LogEntry
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // From Identity

        public IdentityUser User { get; set; }

        [Required]
        public string Action { get; set; } // e.g., "Login", "Added Camera"

        public string Details { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string IpAddress { get; set; }
    }
}