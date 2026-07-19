using ChatBot_BE.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace ChatBot_BE.Tests.Services;

public class GeminiConfigurationTests
{
    [Fact]
    public void Configuration_BindsApiKeyArray()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gemini:ApiKeys:0"] = "api-1",
                ["Gemini:ApiKeys:1"] = "api-2",
                ["Gemini:ModelId"] = "gemini-test",
                ["Gemini:Endpoint"] = "https://example.test/"
            })
            .Build();

        var options = configuration.GetSection("Gemini").Get<GeminiOptions>();

        options.Should().NotBeNull();
        options!.ApiKeys.Should().Equal("api-1", "api-2");
    }

    [Fact]
    public void LaterConfigurationSource_OverridesExampleKeys()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gemini:ApiKeys:0"] = "api-1",
                ["Gemini:ApiKeys:1"] = "api-2"
            })
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gemini:ApiKeys:0"] = "actual-1",
                ["Gemini:ApiKeys:1"] = "actual-2",
                ["Gemini:ApiKeys:2"] = "actual-3"
            })
            .Build();

        var options = configuration.GetSection("Gemini").Get<GeminiOptions>();

        options!.ApiKeys.Should().Equal("actual-1", "actual-2", "actual-3");
    }

    [Fact]
    public void Validation_AllowsPlaceholdersInDevelopment()
    {
        var options = CreateOptions(["api-1", "api-2"]);

        var validated = GeminiOptions.ValidateAndNormalize(options, isDevelopment: true);

        validated.ApiKeys.Should().Equal("api-1", "api-2");
    }

    [Fact]
    public void Validation_RejectsPlaceholdersOutsideDevelopment()
    {
        var options = CreateOptions(["actual-1", "api-2"]);

        var action = () => GeminiOptions.ValidateAndNormalize(options, isDevelopment: false);

        action.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*placeholder API keys*");
    }

    [Fact]
    public void ParseApiKeysJson_ReadsSingleEnvironmentVariableArray()
    {
        var keys = GeminiOptions.ParseApiKeysJson("[\"actual-1\",\"actual-2\",\"actual-3\"]");

        keys.Should().Equal("actual-1", "actual-2", "actual-3");
    }

    [Fact]
    public void ParseApiKeysJson_RejectsNonArrayValue()
    {
        var action = () => GeminiOptions.ParseApiKeysJson("actual-1,actual-2");

        action.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*JSON array*");
    }

    private static GeminiOptions CreateOptions(string[] keys) => new()
    {
        ApiKeys = keys,
        ModelId = "gemini-test",
        Endpoint = "https://example.test/",
        CooldownSeconds = 60,
        MaxAttempts = 3
    };
}
