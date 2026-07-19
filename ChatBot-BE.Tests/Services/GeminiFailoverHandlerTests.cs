using System.Net;
using ChatBot_BE.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChatBot_BE.Tests.Services;

public class GeminiFailoverHandlerTests
{
    [Fact]
    public async Task SendAsync_On429_RetriesWithDifferentKey()
    {
        var inner = new SequenceHandler(
            _ => new HttpResponseMessage(HttpStatusCode.TooManyRequests),
            _ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = CreateClient(["secret-1", "secret-2"], inner);

        using var response = await client.PostAsync("chat/completions", new StringContent("{}"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        inner.AuthorizationValues.Should().Equal("secret-1", "secret-2");
    }

    [Fact]
    public async Task SendAsync_OnUnauthorized_DisablesKeyAndUsesNextKey()
    {
        var inner = new SequenceHandler(
            _ => new HttpResponseMessage(HttpStatusCode.Unauthorized),
            _ => new HttpResponseMessage(HttpStatusCode.OK),
            _ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = CreateClient(["secret-1", "secret-2"], inner);

        using var firstResponse = await client.GetAsync("chat/completions");
        using var secondResponse = await client.GetAsync("chat/completions");

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        inner.AuthorizationValues.Should().Equal("secret-1", "secret-2", "secret-2");
    }

    [Fact]
    public async Task SendAsync_OnBadRequest_DoesNotRotate()
    {
        var inner = new SequenceHandler(
            _ => new HttpResponseMessage(HttpStatusCode.BadRequest));
        using var client = CreateClient(["secret-1", "secret-2"], inner);

        using var response = await client.GetAsync("chat/completions");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        inner.AuthorizationValues.Should().Equal("secret-1");
    }

    [Fact]
    public async Task SendAsync_StopsAtConfiguredMaximumAttempts()
    {
        var inner = new SequenceHandler(
            _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        using var client = CreateClient(["secret-1", "secret-2", "secret-3", "secret-4"], inner, maxAttempts: 3);

        var action = () => client.GetAsync("chat/completions");

        var exception = await action.Should().ThrowAsync<GeminiServiceUnavailableException>();
        exception.Which.RetryAfter.Should().BeGreaterThan(TimeSpan.Zero);
        inner.AuthorizationValues.Should().Equal("secret-1", "secret-2", "secret-3");
    }

    [Fact]
    public async Task SendAsync_PreservesRequestBodyAcrossRetry()
    {
        var inner = new SequenceHandler(
            _ => new HttpResponseMessage(HttpStatusCode.TooManyRequests),
            _ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = CreateClient(["secret-1", "secret-2"], inner);

        using var response = await client.PostAsync(
            "chat/completions",
            new StringContent("{\"message\":\"hello\"}"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        inner.RequestBodies.Should().Equal(
            "{\"message\":\"hello\"}",
            "{\"message\":\"hello\"}");
    }

    private static HttpClient CreateClient(
        string[] keys,
        HttpMessageHandler inner,
        int maxAttempts = 3)
    {
        var options = new GeminiOptions
        {
            ApiKeys = keys,
            ModelId = "gemini-test",
            Endpoint = "https://example.test/",
            CooldownSeconds = 60,
            MaxAttempts = maxAttempts
        };
        var pool = new GeminiKeyPool(options, TimeProvider.System);
        var handler = new GeminiFailoverHandler(
            pool,
            options,
            NullLogger<GeminiFailoverHandler>.Instance)
        {
            InnerHandler = inner
        };

        return new HttpClient(handler)
        {
            BaseAddress = new Uri(options.Endpoint)
        };
    }

    private sealed class SequenceHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses;

        public SequenceHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responses)
        {
            _responses = new Queue<Func<HttpRequestMessage, HttpResponseMessage>>(responses);
        }

        public List<string?> AuthorizationValues { get; } = [];
        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            AuthorizationValues.Add(request.Headers.Authorization?.Parameter);
            RequestBodies.Add(request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken));

            return _responses.Dequeue()(request);
        }
    }
}
