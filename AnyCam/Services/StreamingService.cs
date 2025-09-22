using System.Collections.Concurrent;
using System.Diagnostics;
using Emgu.CV;
using System.Drawing;
using Emgu.CV.Structure;

namespace AnyCam.Services
{
    public class StreamingService
    {
        private readonly ILogger<StreamingService> _logger;
        private readonly ConcurrentDictionary<int, VideoCapture> _runningStreams = new();

        public StreamingService(ILogger<StreamingService> logger)
        {
            _logger = logger;
        }

        public void StartStream(string rtspUrl, int cameraId)
        {
            _logger.LogInformation($"Starting stream for {rtspUrl}");

            try
            {
                var capture = new VideoCapture(rtspUrl);
                if (capture.IsOpened)
                {
                    _runningStreams[cameraId] = capture;
                    _logger.LogInformation($"Started stream for camera {cameraId}");
                }
                else
                {
                    _logger.LogError($"Failed to open stream {rtspUrl}");
                }
            }
            catch (AccessViolationException ex)
            {
                _logger.LogError(ex, $"Access violation starting stream for {rtspUrl}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception starting stream for {rtspUrl}");
            }
        }

        public Mat? GetFrame(int cameraId)
        {
            if (_runningStreams.TryGetValue(cameraId, out var capture))
            {
                var frame = new Mat();
                if (capture.Read(frame) && !frame.IsEmpty)
                {
                    return frame;
                }
            }
            return null;
        }

        public void StopStream(int cameraId)
        {
            if (_runningStreams.TryRemove(cameraId, out var capture))
            {
                try
                {
                    capture.Dispose();
                    _logger.LogInformation($"Stopped stream for camera {cameraId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error stopping stream for camera {cameraId}");
                }
            }
        }

        public IEnumerable<int> GetRunningStreamIds()
        {
            return _runningStreams.Keys;
        }

        public IEnumerable<KeyValuePair<int, VideoCapture>> GetRunningStreams()
        {
            return _runningStreams;
        }

        public void StopAllStreams()
        {
            var ids = _runningStreams.Keys.ToList();
            foreach (var cameraId in ids)
            {
                StopStream(cameraId);
            }
        }
    }
}