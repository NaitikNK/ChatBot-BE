using ChatBot_BE.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ChatBot_BE.Services
{
    public class GeminiClient
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _configuration;

        public GeminiClient(HttpClient http, IConfiguration configuration)
        {
            _http = http;
            _configuration = configuration;
        }

        /// <summary>
        /// Sends a multi-turn conversation with a system instruction to the Gemini API.
        /// </summary>
        public async Task<GeminiResult> GenerateAsync(
            string systemPrompt,
            List<ConversationMessage> history,
            object[]? tools = null,
            CancellationToken cancellationToken = default)
        {
            var apiKey = _configuration["GEMINI_API_KEY"] ?? _configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("Missing Gemini API key. Set GEMINI_API_KEY or Gemini:ApiKey.");
            }

            var model = _configuration["Gemini:Model"];
            if (string.IsNullOrWhiteSpace(model))
            {
                throw new InvalidOperationException("Missing Gemini Model. Set Gemini:Model in configuration.");
            }

            var baseUrl = _configuration["Gemini:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException("Missing Gemini Base URL. Set Gemini:BaseUrl in configuration.");
            }

            _http.BaseAddress ??= new Uri(baseUrl);
            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _http.DefaultRequestHeaders.Remove("x-goog-api-key");
            _http.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);

            // Build the contents array from conversation history
            var contents = history.Select(m => new
            {
                role = m.Role,
                parts = new[] { new { text = m.Content } }
            }).ToArray();

            var request = new Dictionary<string, object>
            {
                ["system_instruction"] = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                ["contents"] = contents
            };

            if (tools != null && tools.Length > 0)
            {
                request["tools"] = tools;
            }

            var path = $"v1beta/models/{model}:generateContent";
            using var response = await _http.PostAsJsonAsync(path, request, cancellationToken);
            var responseBodyStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                using var errorDoc = await JsonDocument.ParseAsync(responseBodyStream, cancellationToken: cancellationToken);
                throw new InvalidOperationException($"Gemini API error ({(int)response.StatusCode}): {errorDoc.RootElement}");
            }

            using var doc = await JsonDocument.ParseAsync(responseBodyStream, cancellationToken: cancellationToken);
            return ExtractResult(doc.RootElement);
        }

        private static GeminiResult ExtractResult(JsonElement root)
        {
            var result = new GeminiResult();
            if (!root.TryGetProperty("candidates", out var candidates) || candidates.ValueKind != JsonValueKind.Array)
            {
                result.Text = root.ToString();
                return result;
            }

            foreach (var candidate in candidates.EnumerateArray())
            {
                if (!candidate.TryGetProperty("content", out var content)) continue;
                if (!content.TryGetProperty("parts", out var parts) || parts.ValueKind != JsonValueKind.Array) continue;

                var texts = new List<string>();
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var textEl) && textEl.ValueKind == JsonValueKind.String)
                    {
                        texts.Add(textEl.GetString() ?? string.Empty);
                    }
                    else if (part.TryGetProperty("functionCall", out var functionCall))
                    {
                        if (functionCall.TryGetProperty("name", out var fcName) && fcName.ValueKind == JsonValueKind.String)
                        {
                            result.FunctionCallName = fcName.GetString() ?? string.Empty;
                        }
                        if (functionCall.TryGetProperty("args", out var fcArgs))
                        {
                            result.FunctionCallArgs = fcArgs;
                        }
                    }
                }

                var joined = string.Join("\n", texts).Trim();
                if (!string.IsNullOrEmpty(joined))
                {
                    result.Text = joined;
                }

                if (!string.IsNullOrEmpty(result.Text) || !string.IsNullOrEmpty(result.FunctionCallName))
                {
                    return result;
                }
            }

            return result;
        }
    }
}
