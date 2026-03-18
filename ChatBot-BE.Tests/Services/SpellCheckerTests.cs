using ChatBot_BE.Services;
using FluentAssertions;

namespace ChatBot_BE.Tests.Services;

public class SpellCheckerTests
{
    private readonly ISpellChecker _spellChecker;

    public SpellCheckerTests()
    {
        _spellChecker = new SpellChecker();
    }

    [Theory]
    [InlineData("teh", "the")]
    [InlineData("taht", "that")]
    [InlineData("police", "policy")]
    [InlineData("insurence", "insurance")]
    [InlineData("becuase", "because")]
    [InlineData("wanna", "want to")]
    [InlineData("gonna", "going to")]
    [InlineData("plz", "please")]
    [InlineData("thx", "thanks")]
    public void CorrectSpelling_ShouldFixCommonMisspellings(string misspelled, string expected)
    {
        // Act
        var result = _spellChecker.CorrectSpelling(misspelled);

        // Assert
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void CorrectSpelling_ShouldFixMultipleErrorsInSentence()
    {
        // Arrange
        var text = "I wanna teh police number plz";

        // Act
        var result = _spellChecker.CorrectSpelling(text);

        // Assert
        result.Should().Contain("want to");
        result.Should().Contain("the");
        result.Should().Contain("policy");
        result.Should().Contain("please");
    }

    [Fact]
    public void CorrectSpelling_ShouldPreserveCase()
    {
        // Arrange
        var text = "Teh quick brown fox";

        // Act
        var result = _spellChecker.CorrectSpelling(text);

        // Assert
        result.Should().StartWith("The");
    }

    [Fact]
    public void HasSpellingErrors_ShouldReturnTrueForMisspelledText()
    {
        // Arrange
        var text = "I tehnk this is gud";

        // Act
        var result = _spellChecker.HasSpellingErrors(text);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasSpellingErrors_ShouldReturnFalseForCorrectText()
    {
        // Arrange
        var text = "I think this is good";

        // Act
        var result = _spellChecker.HasSpellingErrors(text);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetSpellingErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var text = "teh taht polcie";

        // Act
        var errors = _spellChecker.GetSpellingErrors(text);

        // Assert
        errors.Should().Contain(new[] { "teh", "taht" });
    }

    [Fact]
    public void CorrectSpelling_ShouldHandleEmptyString()
    {
        // Act
        var result = _spellChecker.CorrectSpelling("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void CorrectSpelling_ShouldHandleNull()
    {
        // Act
        var result = _spellChecker.CorrectSpelling(null!);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("I want to view my policy")]
    [InlineData("Please create a new record")]
    [InlineData("Show me all my insurance policies")]
    public void HasSpellingErrors_ShouldReturnFalseForCorrectSentences(string text)
    {
        // Act
        var result = _spellChecker.HasSpellingErrors(text);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("I wnt to veiw my polcie")]
    [InlineData("Pleas crete a new recrod")]
    [InlineData("Shw me all my insurence policys")]
    public void HasSpellingErrors_ShouldReturnTrueForMisspelledSentences(string text)
    {
        // Act
        var result = _spellChecker.HasSpellingErrors(text);

        // Assert
        result.Should().BeTrue();
    }
}
