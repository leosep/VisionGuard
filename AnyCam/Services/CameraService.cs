using AnyCam.Models;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using Emgu.CV;

namespace AnyCam.Services
{
    public class CameraService
    {
        public async Task<string> DetectProtocolAsync(string ip, int port)
        {
            // Simple detection: try RTSP, then HTTP
            if (await IsPortOpenAsync(ip, port))
            {
                // Assume RTSP if port open
                return "RTSP";
            }
            if (await IsPortOpenAsync(ip, 80))
            {
                return "HTTP";
            }
            return "Unknown";
        }

        private async Task<bool> IsPortOpenAsync(string ip, int port)
        {
            try
            {
                using var client = new TcpClient();
                var result = client.BeginConnect(ip, port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(1000); // 1 second timeout
                client.EndConnect(result);
                return success;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CheckOnlineAsync(Camera camera)
        {
            try
            {
                if (!string.IsNullOrEmpty(camera.StreamUrl))
                {
                    // Use EmguCV to check if stream is accessible
                    Console.WriteLine($"Checking stream online for {camera.Name}: {camera.StreamUrl}");
                    using var capture = new VideoCapture(camera.StreamUrl);
                    if (!capture.IsOpened)
                    {
                        return false;
                    }

                    using var frame = new Mat();
                    // Try to read a frame within 10 seconds
                    var task = Task.Run(() => capture.Read(frame));
                    if (await Task.WhenAny(task, Task.Delay(10000)) == task)
                    {
                        return !frame.IsEmpty;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    // Fallback to TCP check
                    string host = camera.IpAddress;
                    int port = camera.Port;

                    Console.WriteLine($"Checking TCP online for {camera.Name}: {host}:{port}");
                    var result = await IsPortOpenAsync(host, port);
                    Console.WriteLine($"Result for {camera.Name}: {result}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking online for {camera.Name}: {ex.Message}");
                return false;
            }
        }

    }
}