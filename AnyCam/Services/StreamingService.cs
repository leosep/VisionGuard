using System.Collections.Concurrent;
using System.Diagnostics;

namespace AnyCam.Services
{
    public class StreamingService
    {
        private readonly ILogger<StreamingService> _logger;
        private readonly ConcurrentDictionary<int, (Process Process, DateTime LastAccessed)> _runningStreams = new();

        public StreamingService(ILogger<StreamingService> logger)
        {
            _logger = logger;
        }

        public Process StartHlsStream(string rtspUrl, string outputPath, int cameraId)
        {
            _logger.LogInformation($"Starting HLS stream for {rtspUrl} to {outputPath}");
            _logger.LogInformation($"Current directory: {Directory.GetCurrentDirectory()}");
            _logger.LogInformation($"Output path exists: {Directory.Exists(outputPath)}");

            // FFmpeg command to convert RTSP to HLS with limited segments
            var segmentPattern = Path.Combine(outputPath, "segment_%03d.ts");
            var playlistPath = Path.Combine(outputPath, "playlist.m3u8");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-rtsp_transport tcp -i \"{rtspUrl}\" -c:v libx264 -c:a aac -f hls -hls_time 5 -hls_list_size 50 -hls_flags delete_segments -hls_segment_filename \"{segmentPattern}\" \"{playlistPath}\"",
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

            // Store the process
            _runningStreams[cameraId] = (process, DateTime.UtcNow);
            _logger.LogInformation($"Started HLS stream for camera {cameraId}");

            return process;
        }

        public void StopHlsStream(int cameraId)
        {
            if (_runningStreams.TryRemove(cameraId, out var streamInfo))
            {
                try
                {
                    if (!streamInfo.Process.HasExited)
                    {
                        streamInfo.Process.Kill();
                        _logger.LogInformation($"Stopped HLS stream for camera {cameraId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error stopping HLS stream for camera {cameraId}");
                }
            }
        }

        public void UpdateLastAccessed(int cameraId)
        {
            if (_runningStreams.TryGetValue(cameraId, out var streamInfo))
            {
                _runningStreams[cameraId] = (streamInfo.Process, DateTime.UtcNow);
            }
        }

        public IEnumerable<int> GetRunningStreamIds()
        {
            return _runningStreams.Keys;
        }

        public IEnumerable<KeyValuePair<int, (Process Process, DateTime LastAccessed)>> GetRunningStreams()
        {
            return _runningStreams;
        }

        public void StopAllStreams()
        {
            foreach (var cameraId in _runningStreams.Keys)
            {
                StopHlsStream(cameraId);
            }
        }
    }
}