namespace ChatBot_BE.Services
{
    public sealed class GeminiOptions
    {
        public const string SectionName = "Gemini";

        public string[] ApiKeys { get; init; } = Array.Empty<string>();
        public string ModelId { get; init; } = string.Empty;
        public string Endpoint { get; init; } = string.Empty;
        public int CooldownSeconds { get; init; } = 60;
        public int MaxAttempts { get; init; } = 3;

        public TimeSpan Cooldown => TimeSpan.FromSeconds(CooldownSeconds);

        public static bool IsPlaceholderKey(string key) =>
            key.StartsWith("api-", StringComparison.OrdinalIgnoreCase);

        public static GeminiOptions ValidateAndNormalize(GeminiOptions configured, bool isDevelopment)
        {
            ArgumentNullException.ThrowIfNull(configured);

            var apiKeys = configured.ApiKeys
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(key => key.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (apiKeys.Length == 0)
            {
                throw new InvalidOperationException("At least one Gemini API key is required.");
            }

            if (!isDevelopment && apiKeys.Any(IsPlaceholderKey))
            {
                throw new InvalidOperationException(
                    "Gemini placeholder API keys are not allowed outside Development. " +
                    "Configure Gemini__ApiKeys__0 and subsequent indexed environment variables.");
            }

            if (string.IsNullOrWhiteSpace(configured.ModelId))
            {
                throw new InvalidOperationException("Missing Gemini:ModelId in configuration.");
            }

            if (!Uri.TryCreate(configured.Endpoint, UriKind.Absolute, out var endpoint)
                || endpoint.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException("Gemini:Endpoint must be an absolute HTTPS URL.");
            }

            if (configured.CooldownSeconds <= 0)
            {
                throw new InvalidOperationException("Gemini:CooldownSeconds must be greater than zero.");
            }

            if (configured.MaxAttempts <= 0)
            {
                throw new InvalidOperationException("Gemini:MaxAttempts must be greater than zero.");
            }

            return new GeminiOptions
            {
                ApiKeys = apiKeys,
                ModelId = configured.ModelId.Trim(),
                Endpoint = endpoint.AbsoluteUri,
                CooldownSeconds = configured.CooldownSeconds,
                MaxAttempts = configured.MaxAttempts
            };
        }
    }
}
