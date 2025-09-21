using AnyCam.Models;
using System.Net.Sockets;
using System.Net;

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
                if (!string.IsNullOrEmpty(camera.StreamUrl) && camera.StreamUrl.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase))
                {
                    // For RTSP, send OPTIONS request
                    var uri = new Uri(camera.StreamUrl);
                    var host = uri.Host;
                    var port = uri.Port;

                    Console.WriteLine($"Checking RTSP online for {camera.Name}: {host}:{port}");
                    return await CheckRtspOnlineAsync(host, port, camera.StreamUrl);
                }
                else
                {
                    // Fallback to TCP check
                    string host = camera.IpAddress;
                    int port = camera.Port;

                    if (!string.IsNullOrEmpty(camera.StreamUrl))
                    {
                        var uri = new Uri(camera.StreamUrl);
                        host = uri.Host;
                        port = uri.Port;
                    }

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

        private async Task<bool> CheckRtspOnlineAsync(string host, int port, string url)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    // Try connect with a short timeout
                    var result = client.BeginConnect(host, port, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3));

                    if (!success) return false;
                    client.EndConnect(result);

                    // Send an OPTIONS request to validate RTSP
                    using (var stream = client.GetStream())
                    {
                        string request = $"OPTIONS {url} RTSP/1.0\r\nCSeq: 1\r\n\r\n";
                        byte[] data = System.Text.Encoding.ASCII.GetBytes(request);
                        stream.Write(data, 0, data.Length);

                        byte[] buffer = new byte[1024];
                        int read = stream.Read(buffer, 0, buffer.Length);
                        string response = System.Text.Encoding.ASCII.GetString(buffer, 0, read);

                        return response.Contains("RTSP");
                    }
                }
            }
            catch
            {
                return false;
            }
        }
    }
}