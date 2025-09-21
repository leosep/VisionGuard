t camusing System.Diagnostics;

namespace AnyCam.Services
{
    public class StreamingService
    {
        private readonly ILogger<StreamingService> _logger;

        public StreamingService(ILogger<StreamingService> logger)
        {
            _logger = logger;
        }

        public void StartHlsStream(string rtspUrl, string outputPath)
        {
            _logger.LogInformation($"Starting HLS stream for {rtspUrl} to {outputPath}");
            _logger.LogInformation($"Current directory: {Directory.GetCurrentDirectory()}");
            _logger.LogInformation($"Output path exists: {Directory.Exists(outputPath)}");

            // FFmpeg command to convert RTSP to HLS
            var segmentPattern = Path.Combine(outputPath, "segment_%03d.ts");
            var playlistPath = Path.Combine(outputPath, "playlist.m3u8");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"{rtspUrl}\" -c:v libx264 -c:a aac -f hls -hls_time 10 -hls_list_size 0 -hls_segment_filename \"{segmentPattern}\" \"{playlistPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            // Log the command
            _logger.LogInformation($"FFmpeg command: ffmpeg {process.StartInfo.Arguments}");

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _logger.LogInformation("FFmpeg stderr: {Data}", e.Data);
                }
            };

            process.Start();
            process.BeginErrorReadLine();
        }
    }
}