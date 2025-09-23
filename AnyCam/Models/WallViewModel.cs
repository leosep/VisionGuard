using System.Collections.Generic;

namespace AnyCam.Models
{
    public class WallViewModel
    {
        public List<Camera> Cameras { get; set; } = new List<Camera>();
        public List<AiEvent> RecentAiEvents { get; set; } = new List<AiEvent>();
    }
}