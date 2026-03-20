using ChatBot_BE.Services;
using ChatBot_BE.Model;
using ChatBot_BE.Dto;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ChatBot_BE.Tests.Services;

public class InputValidatorTests
{
    private readonly IProfanityFilter _profanityFilter;
    private readonly ISpellChecker _spellChecker;
    private readonly ILogger<InputValidator> _logger;
    private readonly IInputValidator _validator;

    public InputValidatorTests()
    {
        _profanityFilter = new ProfanityFilter();
        _spellChecker = new SpellChecker();
        _logger = Substitute.For<ILogger<InputValidator>>();
        _validator = new InputValidator(_profanityFilter, _spellChecker, _logger);
    }

    [Fact]
    public void Validate_ShouldReturnInvalidForEmptyInput()
    {
        // Act
        var result = _validator.Validate("");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("empty");
    }

    [Fact]
    public void Validate_ShouldReturnInvalidForProfanity()
    {
        // Act
        var result = _validator.Validate("This is fucking ridiculous");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ContainsProfanity.Should().BeTrue();
        result.ErrorMessage.Should().Contain("respectful language");
    }

    [Fact]
    public void Validate_ShouldReturnValidForCleanText()
    {
        // Act
        var result = _validator.Validate("Hello, how can I help you?");

        // Assert
        result.IsValid.Should().BeTrue();
        result.ContainsProfanity.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldCorrectSpellingErrors()
    {
        // Act
        var result = _validator.Validate("I wanna teh police number plz");

        // Assert
        result.IsValid.Should().BeTrue();
        result.HasSpellingErrors.Should().BeTrue();
        result.CorrectedText.Should().Contain("want to");
        result.CorrectedText.Should().Contain("the");
        result.CorrectedText.Should().Contain("policy");
        result.CorrectedText.Should().Contain("please");
    }

    [Fact]
    public void Validate_ShouldReturnNoSpellingErrorsForCorrectText()
    {
        // Act
        var result = _validator.Validate("I want to view my policy");

        // Assert
        result.IsValid.Should().BeTrue();
        result.HasSpellingErrors.Should().BeFalse();
        result.CorrectedText.Should().Be("I want to view my policy");
    }

    [Fact]
    public void Validate_ShouldDetectProfanityEvenWithMisspellings()
    {
        // Act
        var result = _validator.Validate("This is fckng bullshit");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ContainsProfanity.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldReturnSpellingErrorsList()
    {
        // Act
        var result = _validator.Validate("teh taht polcie");

        // Assert
        result.SpellingErrors.Should().Contain(new[] { "teh", "taht" });
    }

    [Fact]
    public void Validate_ShouldReturnDetectedProfanitiesList()
    {
        // Act
        var result = _validator.Validate("This shit is fucking bad");

        // Assert
        result.DetectedProfanities.Should().NotBeEmpty();
        result.DetectedProfanities.Should().Contain("shit");
    }

    [Theory]
    [InlineData("Hello")]
    [InlineData("Hi there")]
    [InlineData("Good morning")]
    [InlineData("How are you?")]
    public void Validate_ShouldAcceptShortValidInput(string text)
    {
        // Act
        var result = _validator.Validate(text);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
