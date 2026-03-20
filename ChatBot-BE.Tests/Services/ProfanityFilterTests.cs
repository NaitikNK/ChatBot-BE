using ChatBot_BE.Services;
using FluentAssertions;

namespace ChatBot_BE.Tests.Services;

public class ProfanityFilterTests
{
    private readonly IProfanityFilter _filter;

    public ProfanityFilterTests()
    {
        _filter = new ProfanityFilter();
    }

    [Theory]
    [InlineData("Hello, how can I help you?")]
    [InlineData("I want to create a policy record")]
    [InlineData("Please show me my options")]
    [InlineData("Thank you for your assistance")]
    public void ContainsProfanity_ShouldReturnFalseForCleanText(string text)
    {
        // Act
        var result = _filter.ContainsProfanity(text);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("This is fucking amazing")]
    [InlineData("What the shit is this")]
    [InlineData("You are a bitch")]
    [InlineData("Fuck you")]
    [InlineData("This is bullshit")]
    public void ContainsProfanity_ShouldReturnTrueForProfanity(string text)
    {
        // Act
        var result = _filter.ContainsProfanity(text);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("This is f***ing amazing")]
    [InlineData("What the sh*t is this")]
    [InlineData("F*** you")]
    public void ContainsProfanity_ShouldDetectCensoredProfanity(string text)
    {
        // Act
        var result = _filter.ContainsProfanity(text);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("This is fuuucking bad")]
    [InlineData("Shiiit happens")]
    [InlineData("Wth the fck")]
    public void ContainsProfanity_ShouldDetectRepeatedCharacters(string text)
    {
        // Act
        var result = _filter.ContainsProfanity(text);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("This is f@cked up", true)]
    [InlineData("Sh1t happens", true)]
    [InlineData("B1tch please", true)]
    public void ContainsProfanity_ShouldDetectLeetSpeak(string text, bool expected)
    {
        // Act - Note: Leet speak detection is limited in current implementation
        var result = _filter.ContainsProfanity(text);

        // Assert - This test documents the current limitation
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("I have a class to attend")]
    [InlineData("Please pass the salt")]
    [InlineData("The grass is green")]
    [InlineData("Hello, how are you?")]
    [InlineData("This shell is beautiful")]
    public void ContainsProfanity_ShouldAllowLegitimateContext(string text)
    {
        // Act
        var result = _filter.ContainsProfanity(text);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CensorProfanity_ShouldReplaceProfanityWithAsterisks()
    {
        // Arrange
        var text = "This is fucking ridiculous";

        // Act
        var result = _filter.CensorProfanity(text);

        // Assert
        result.Should().Contain("****");
        result.Should().NotContain("fuck");
    }

    [Fact]
    public void GetDetectedProfanities_ShouldReturnAllProfanities()
    {
        // Arrange
        var text = "This fucking shit is bullshit";

        // Act
        var profanities = _filter.GetDetectedProfanities(text);

        // Assert
        profanities.Should().Contain(new[] { "fuck", "shit" });
    }

    [Fact]
    public void ContainsProfanity_ShouldBeCaseInsensitive()
    {
        // Arrange
        var text = "THIS IS FUCKING AMAZING";

        // Act
        var result = _filter.ContainsProfanity(text);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsProfanity_ShouldReturnFalseForEmptyString()
    {
        // Act
        var result = _filter.ContainsProfanity("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsProfanity_ShouldReturnFalseForNull()
    {
        // Act
        var result = _filter.ContainsProfanity(null!);

        // Assert
        result.Should().BeFalse();
    }
}
