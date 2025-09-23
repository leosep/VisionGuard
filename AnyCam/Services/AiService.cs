using OpenAI.Chat;
using System.Text.Json;

namespace AnyCam.Services
{
    public class AiAnalysisResult
    {
        public List<string> Objects { get; set; } = new List<string>();
        public int PeopleCount { get; set; }
        public string? Anomalies { get; set; }
        public string? EventType { get; set; }
        public double Confidence { get; set; }
        public string Summary { get; set; } = "";
    }

    public class AiService
    {
        private readonly string _apiKey;

        public AiService(IConfiguration configuration)
        {
            _apiKey = configuration["OpenAI:ApiKey"] ?? "";
        }

        public async Task<AiAnalysisResult> AnalyzeImageAsync(byte[] imageBytes)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return new AiAnalysisResult
                {
                    Summary = "AI service not configured",
                    EventType = "Configuration Error",
                    Confidence = 0
                };
            }

            try
            {
                var client = new ChatClient("gpt-4o", _apiKey);

                var messages = new List<ChatMessage>
                {
                    new UserChatMessage(
                        ChatMessageContentPart.CreateTextPart(@"Analyze this security camera image and provide a JSON response with:
{
  'objects': ['list', 'of', 'detected', 'objects'],
  'peopleCount': number_of_people_detected,
  'anomalies': 'description of any unusual activity or null',
  'eventType': 'Motion/Person Detected/Anomaly/None',
  'confidence': confidence_score_0_to_1,
  'summary': 'brief description of what was detected'
}
Only return valid JSON, no additional text."),
                        ChatMessageContentPart.CreateImagePart(new BinaryData(imageBytes), "image/jpeg")
                    )
                };

                var response = await client.CompleteChatAsync(messages);
                var jsonText = response.Value.Content[0].Text.Trim();

                // Clean up the response (remove markdown code blocks if present)
                if (jsonText.StartsWith("```json"))
                {
                    jsonText = jsonText.Replace("```json", "").Replace("```", "").Trim();
                }

                var result = JsonSerializer.Deserialize<AiAnalysisResult>(jsonText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new AiAnalysisResult
                {
                    Summary = "Failed to parse AI response",
                    EventType = "Parse Error",
                    Confidence = 0
                };
            }
            catch (Exception ex)
            {
                return new AiAnalysisResult
                {
                    Summary = $"AI analysis failed: {ex.Message}",
                    EventType = "Analysis Error",
                    Confidence = 0
                };
            }
        }

        public async Task<bool> ShouldCreateEventAsync(byte[] imageBytes)
        {
            var analysis = await AnalyzeImageAsync(imageBytes);

            // Create events for significant detections
            return analysis.EventType != "None" &&
                   analysis.Confidence > 0.3 && // Minimum confidence threshold
                   (analysis.PeopleCount > 0 || !string.IsNullOrEmpty(analysis.Anomalies) || analysis.Objects.Any());
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
                new UserChatMessage($"Given the query: '{query}', find matching video indices from descriptions: {string.Join("; ", videoDescriptions.Select((desc, i) => $"{i}: {desc}"))}. Return a JSON array of matching indices.")
            };

            var response = await client.CompleteChatAsync(messages);

            var text = response.Value.Content[0].Text;
            try
            {
                var indices = JsonSerializer.Deserialize<List<int>>(text);
                return indices ?? new List<int>();
            }
            catch
            {
                // Fallback: return all
                return Enumerable.Range(0, videoDescriptions.Count).ToList();
            }
        }
    }
}