using OpenAI.Chat;
using System.Text.Json;

namespace AnyCam.Services
{
    public class AiService
    {
        private readonly string _apiKey;

        public AiService(IConfiguration configuration)
        {
            _apiKey = configuration["OpenAI:ApiKey"] ?? "";
        }

        public async Task<string> AnalyzeImageAsync(byte[] imageBytes)
        {
            var client = new ChatClient("gpt-4o", _apiKey);

            var messages = new List<ChatMessage>
            {
                new UserChatMessage(
                    ChatMessageContentPart.CreateTextPart("Analyze this image and detect any objects, people, or anomalies. Provide a JSON response with 'objects': array of detected objects, 'people': count, 'anomalies': description if any."),
                    ChatMessageContentPart.CreateImagePart(new BinaryData(imageBytes), "image/jpeg")
                )
            };

            var response = await client.CompleteChatAsync(messages);

            return response.Value.Content[0].Text;
        }

        public async Task<bool> IsAnomalyAsync(byte[] imageBytes)
        {
            var analysis = await AnalyzeImageAsync(imageBytes);
            // Simple check for "anomaly" in response
            return analysis.Contains("anomaly", StringComparison.OrdinalIgnoreCase);
        }

        // For facial recognition, assume a simple match
        public async Task<string> RecognizeFaceAsync(byte[] imageBytes, List<string> knownFaces)
        {
            var client = new ChatClient("gpt-4o", _apiKey);

            var messages = new List<ChatMessage>
            {
                new UserChatMessage(
                    ChatMessageContentPart.CreateTextPart($"Compare this face to known faces: {string.Join(", ", knownFaces)}. Identify if it matches any."),
                    ChatMessageContentPart.CreateImagePart(new BinaryData(imageBytes), "image/jpeg")
                )
            };

            var response = await client.CompleteChatAsync(messages);

            return response.Value.Content[0].Text;
        }

        // Natural language search
        public async Task<List<int>> SearchVideosAsync(string query, List<string> videoDescriptions)
        {
            var client = new ChatClient("gpt-4o", _apiKey);

            var messages = new List<ChatMessage>
            {
                new UserChatMessage($"Given the query: '{query}', find matching video indices from descriptions: {string.Join("; ", videoDescriptions)}")
            };

            var response = await client.CompleteChatAsync(messages);

            // Parse response to get indices
            // For simplicity, return all for now
            return Enumerable.Range(0, videoDescriptions.Count).ToList();
        }
    }
}