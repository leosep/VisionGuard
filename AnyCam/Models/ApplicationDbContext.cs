using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AnyCam.Models
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Camera> Cameras { get; set; }
        public DbSet<VideoClip> VideoClips { get; set; }
        public DbSet<AiEvent> AiEvents { get; set; }
        public DbSet<LogEntry> LogEntries { get; set; }
    }
}