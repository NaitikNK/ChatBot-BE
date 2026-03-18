using ChatBot_BE.Services;
using FluentAssertions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ChatBot_BE.Tests.Services;

public class ChatSessionStateTests
{
    [Fact]
    public void TrimHistory_ShouldKeepSystemMessage()
    {
        // Arrange
        var session = new ChatSessionState();
        session.History.AddSystemMessage("You are a helpful assistant.");
        session.History.AddUserMessage("Hello");
        session.History.AddAssistantMessage("Hi there!");

        // Act
        session.TrimHistory();

        // Assert
        session.History.Count.Should().Be(3);
        session.History[0].Role.Should().Be(AuthorRole.System);
    }

    [Fact]
    public void TrimHistory_ShouldNotTrimWhenUnderLimit()
    {
        // Arrange
        var session = new ChatSessionState();
        session.History.AddSystemMessage("You are a helpful assistant.");
        session.History.AddUserMessage("Hello");
        session.History.AddAssistantMessage("Hi there!");

        // Act
        session.TrimHistory();

        // Assert
        session.History.Count.Should().Be(3);
    }

    [Fact]
    public void TrimHistory_ShouldTrimWhenOverLimit()
    {
        // Arrange
        var session = new ChatSessionState();
        session.History.AddSystemMessage("You are a helpful assistant.");

        // Add 25 user/assistant pairs (over the 20 message limit)
        for (int i = 0; i < 25; i++)
        {
            session.History.AddUserMessage($"Message {i}");
            session.History.AddAssistantMessage($"Response {i}");
        }

        // Act
        session.TrimHistory();

        // Assert
        // Should have: 1 system message + 20 other messages = 21 total
        session.History.Count.Should().Be(21);
        session.History[0].Role.Should().Be(AuthorRole.System);
        
        // The last messages should be preserved
        session.History[^1].Content.Should().Contain("Response 24");
        session.History[^2].Content.Should().Contain("Message 24");
    }

    [Fact]
    public void TrimHistory_ShouldKeepMostRecentMessages()
    {
        // Arrange
        var session = new ChatSessionState();
        session.History.AddSystemMessage("You are a helpful assistant.");

        for (int i = 0; i < 30; i++)
        {
            session.History.AddUserMessage($"Message {i}");
            session.History.AddAssistantMessage($"Response {i}");
        }

        // Act
        session.TrimHistory();

        // Assert
        // Verify the oldest messages were removed
        session.History.Should().NotContain(m => m.Content == "Message 0");
        session.History.Should().NotContain(m => m.Content == "Response 0");
        
        // Verify the newest messages were kept
        session.History.Should().Contain(m => m.Content == "Message 29");
        session.History.Should().Contain(m => m.Content == "Response 29");
    }
}
