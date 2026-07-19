using System.Net;
using System.Net.Http.Headers;

namespace ChatBot_BE.Services
{
    public sealed class GeminiServiceUnavailableException : Exception
    {
        public GeminiServiceUnavailableException(TimeSpan retryAfter, Exception? innerException = null)
            : base("All Gemini API keys are temporarily unavailable.", innerException)
        {
            RetryAfter = retryAfter;
        }

        public TimeSpan RetryAfter { get; }

        public static GeminiServiceUnavailableException? FindIn(Exception exception)
        {
            for (Exception? current = exception; current != null; current = current.InnerException)
            {
                if (current is GeminiServiceUnavailableException unavailable)
                {
                    return unavailable;
                }
            }

            return null;
        }
    }

    public sealed class GeminiFailoverHandler : DelegatingHandler
    {
        private static readonly HashSet<HttpStatusCode> TransientStatusCodes =
        [
            HttpStatusCode.RequestTimeout,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout
        ];

        private readonly GeminiKeyPool _keyPool;
        private readonly GeminiOptions _options;
        private readonly ILogger<GeminiFailoverHandler> _logger;

        public GeminiFailoverHandler(
            GeminiKeyPool keyPool,
            GeminiOptions options,
            ILogger<GeminiFailoverHandler> logger)
        {
            _keyPool = keyPool;
            _options = options;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var content = request.Content is null
                ? null
                : await request.Content.ReadAsByteArrayAsync(cancellationToken);
            var attemptedKeyIds = new HashSet<int>();
            var maximumAttempts = Math.Min(_options.MaxAttempts, _keyPool.Count);
            Exception? lastException = null;

            for (var attempt = 1; attempt <= maximumAttempts; attempt++)
            {
                var lease = _keyPool.TryAcquire(attemptedKeyIds);
                if (lease is null)
                {
                    break;
                }

                attemptedKeyIds.Add(lease.Id);
                using var retryRequest = CloneRequest(request, content);
                retryRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", lease.ApiKey);

                try
                {
                    var response = await base.SendAsync(retryRequest, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogDebug(
                            "Gemini request succeeded with key {KeyId} on attempt {Attempt}.",
                            lease.Id,
                            attempt);
                        return response;
                    }

                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        var cooldown = GetRetryAfter(response) ?? _keyPool.DefaultCooldown;
                        _keyPool.PutOnCooldown(lease.Id, cooldown);
                        LogCooldown(lease.Id, response.StatusCode, cooldown, attempt);
                        response.Dispose();
                        continue;
                    }

                    if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                    {
                        _keyPool.Disable(lease.Id);
                        _logger.LogWarning(
                            "Disabled Gemini key {KeyId} after HTTP {StatusCode} on attempt {Attempt}.",
                            lease.Id,
                            (int)response.StatusCode,
                            attempt);
                        response.Dispose();
                        continue;
                    }

                    if (TransientStatusCodes.Contains(response.StatusCode))
                    {
                        _keyPool.PutOnCooldown(lease.Id, _keyPool.DefaultCooldown);
                        LogCooldown(lease.Id, response.StatusCode, _keyPool.DefaultCooldown, attempt);
                        response.Dispose();
                        continue;
                    }

                    return response;
                }
                catch (HttpRequestException ex)
                {
                    lastException = ex;
                    _keyPool.PutOnCooldown(lease.Id, _keyPool.DefaultCooldown);
                    _logger.LogWarning(
                        "Gemini network failure with key {KeyId} on attempt {Attempt}; cooling key for {CooldownSeconds} seconds.",
                        lease.Id,
                        attempt,
                        _keyPool.DefaultCooldown.TotalSeconds);
                }
                catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    lastException = ex;
                    _keyPool.PutOnCooldown(lease.Id, _keyPool.DefaultCooldown);
                    _logger.LogWarning(
                        "Gemini timeout with key {KeyId} on attempt {Attempt}; cooling key for {CooldownSeconds} seconds.",
                        lease.Id,
                        attempt,
                        _keyPool.DefaultCooldown.TotalSeconds);
                }
            }

            throw new GeminiServiceUnavailableException(_keyPool.GetRetryAfter(), lastException);
        }

        private void LogCooldown(int keyId, HttpStatusCode statusCode, TimeSpan cooldown, int attempt)
        {
            _logger.LogWarning(
                "Gemini key {KeyId} returned HTTP {StatusCode} on attempt {Attempt}; cooling key for {CooldownSeconds} seconds.",
                keyId,
                (int)statusCode,
                attempt,
                cooldown.TotalSeconds);
        }

        private static TimeSpan? GetRetryAfter(HttpResponseMessage response)
        {
            var retryAfter = response.Headers.RetryAfter;
            if (retryAfter?.Delta is { } delta)
            {
                return delta;
            }

            if (retryAfter?.Date is { } date)
            {
                var duration = date - DateTimeOffset.UtcNow;
                return duration > TimeSpan.Zero ? duration : TimeSpan.Zero;
            }

            return null;
        }

        private static HttpRequestMessage CloneRequest(HttpRequestMessage source, byte[]? content)
        {
            var clone = new HttpRequestMessage(source.Method, source.RequestUri)
            {
                Version = source.Version,
                VersionPolicy = source.VersionPolicy
            };

            foreach (var header in source.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            foreach (var option in source.Options)
            {
                clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
            }

            if (content is not null)
            {
                clone.Content = new ByteArrayContent(content);
                foreach (var header in source.Content!.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return clone;
        }
    }
}
