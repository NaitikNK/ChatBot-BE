using ChatBot_BE.Data;
using ChatBot_BE.Services;
using ChatBot_BE.Model;
using ChatBot_BE.Dto;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ChatBot_BE.Tests.Services;

public class UserManagementPluginTests
{
    private readonly IUserStore _userStore;
    private readonly ILogger<UserManagementPlugin> _logger;
    private readonly IConversationContext _context;
    private readonly UserManagementPlugin _plugin;

    public UserManagementPluginTests()
    {
        _userStore = Substitute.For<IUserStore>();
        _logger = Substitute.For<ILogger<UserManagementPlugin>>();
        _context = new ConversationContext { ConversationId = "test-conversation-123" };
        _plugin = new UserManagementPlugin(_userStore, _logger, _context);
    }

    [Fact]
    public async Task CreateUser_ShouldReturnSuccessMessage()
    {
        // Arrange
        _userStore.GetByPolicyNumberAsync("POL123").Returns(Task.FromResult<User?>(null));
        _userStore.GetByEmailAsync("john@example.com").Returns(Task.FromResult<User?>(null));
        _userStore.AddAsync(Arg.Any<User>()).Returns(Task.FromResult(new User
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            PolicyNumber = "POL123",
            Email = "john@example.com",
            OwnerSessionId = _context.ConversationId,
            CreatedAt = DateTime.UtcNow
        }));

        // Act
        var result = await _plugin.CreateUser("John", "Doe", "POL123", "john@example.com");

        // Assert
        result.Should().Contain("TOOL RESULT: Policy record created successfully!");
        result.Should().Contain("John Doe");
        result.Should().Contain("POL123");
    }

    [Fact]
    public async Task CreateUser_ShouldReturnErrorWhenMissingFields()
    {
        // Act
        var result = await _plugin.CreateUser("", "Doe", "POL123", "john@example.com");

        // Assert
        result.Should().Contain("⚠️ Missing required fields");
    }

    [Fact]
    public async Task CreateUser_ShouldReturnErrorWhenPolicyNumberExists()
    {
        // Arrange
        _userStore.GetByPolicyNumberAsync("POL123").Returns(Task.FromResult<User?>(new User
        {
            Id = 1,
            FirstName = "Existing",
            LastName = "User",
            PolicyNumber = "POL123",
            Email = "existing@example.com",
            OwnerSessionId = _context.ConversationId,
            CreatedAt = DateTime.UtcNow
        }));

        // Act
        var result = await _plugin.CreateUser("John", "Doe", "POL123", "john@example.com");

        // Assert
        result.Should().Contain("⚠️ Policy number 'POL123' already exists");
    }

    [Fact]
    public async Task CreateUser_ShouldReturnErrorWhenEmailExists()
    {
        // Arrange
        _userStore.GetByPolicyNumberAsync("POL123").Returns(Task.FromResult<User?>(null));
        _userStore.GetByEmailAsync("john@example.com").Returns(Task.FromResult<User?>(new User
        {
            Id = 1,
            FirstName = "Existing",
            LastName = "User",
            PolicyNumber = "POL456",
            Email = "john@example.com",
            OwnerSessionId = _context.ConversationId,
            CreatedAt = DateTime.UtcNow
        }));

        // Act
        var result = await _plugin.CreateUser("John", "Doe", "POL123", "john@example.com");

        // Assert
        result.Should().Contain("⚠️ Email 'john@example.com' is already in use");
    }

    [Fact]
    public async Task ViewUser_ShouldReturnUserDetails()
    {
        // Arrange
        _userStore.GetByPolicyNumberAsync("POL123").Returns(Task.FromResult<User?>(new User
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            PolicyNumber = "POL123",
            Email = "john@example.com",
            OwnerSessionId = _context.ConversationId,
            CreatedAt = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc)
        }));

        // Act
        var result = await _plugin.ViewUser("POL123");

        // Assert
        result.Should().Contain("TOOL RESULT: Record found:");
        result.Should().Contain("John Doe");
        result.Should().Contain("POL123");
        result.Should().Contain("john@example.com");
    }

    [Fact]
    public async Task ViewUser_ShouldReturnErrorWhenNotFound()
    {
        // Arrange
        _userStore.GetByPolicyNumberAsync("POL123").Returns((User?)null);

        // Act
        var result = await _plugin.ViewUser("POL123");

        // Assert
        result.Should().Contain("❌ No record found");
    }

    [Fact]
    public async Task ViewUser_ShouldReturnAccessDeniedWhenDifferentSession()
    {
        // Arrange
        _userStore.GetByPolicyNumberAsync("POL123").Returns(new User
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            PolicyNumber = "POL123",
            Email = "john@example.com",
            OwnerSessionId = "different-conversation-id",
            CreatedAt = DateTime.UtcNow
        });

        // Act
        var result = await _plugin.ViewUser("POL123");

        // Assert
        result.Should().Contain("⛔ Access Denied");
    }

    [Fact]
    public async Task ViewUser_ShouldReturnDetailsWhenSystemRecord()
    {
        // Arrange
        _userStore.GetByPolicyNumberAsync("POL123").Returns(new User
        {
            Id = 1,
            FirstName = "System",
            LastName = "User",
            PolicyNumber = "POL123",
            Email = "system@example.com",
            OwnerSessionId = "", // Seeded record
            CreatedAt = DateTime.UtcNow
        });

        // Act
        var result = await _plugin.ViewUser("POL123");

        // Assert
        result.Should().Contain("TOOL RESULT: Record found:");
        result.Should().Contain("System User");
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnSuccessMessage()
    {
        // Arrange
        _userStore.GetByPolicyNumberAsync("POL123").Returns(Task.FromResult<User?>(new User
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            PolicyNumber = "POL123",
            Email = "john@example.com",
            OwnerSessionId = _context.ConversationId,
            CreatedAt = DateTime.UtcNow
        }));
        _userStore.DeleteByPolicyNumberAsync("POL123").Returns(Task.FromResult(true));

        // Act
        var result = await _plugin.DeleteUser("POL123");

        // Assert
        result.Should().Contain("TOOL RESULT: Record with policy number 'POL123' has been deleted successfully.");
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnErrorWhenNotFound()
    {
        // Arrange
        _userStore.GetByPolicyNumberAsync("POL123").Returns((User?)null);

        // Act
        var result = await _plugin.DeleteUser("POL123");

        // Assert
        result.Should().Contain("❌ No record found");
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnAccessDeniedWhenDifferentSession()
    {
        // Arrange
        _userStore.GetByPolicyNumberAsync("POL123").Returns(new User
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            PolicyNumber = "POL123",
            Email = "john@example.com",
            OwnerSessionId = "different-conversation-id",
            CreatedAt = DateTime.UtcNow
        });

        // Act
        var result = await _plugin.DeleteUser("POL123");

        // Assert
        result.Should().Contain("⛔ Access Denied");
    }

    [Fact]
    public async Task ListAllUsers_ShouldReturnAllRecordsForSessionIncludingSystem()
    {
        // Arrange
        var users = new List<User>
        {
            new User
            {
                Id = 1,
                FirstName = "Session",
                LastName = "User",
                PolicyNumber = "POL123",
                Email = "session@example.com",
                OwnerSessionId = _context.ConversationId,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = 2,
                FirstName = "System",
                LastName = "Seeded",
                PolicyNumber = "POL456",
                Email = "system@example.com",
                OwnerSessionId = "", // System record
                CreatedAt = DateTime.UtcNow
            }
        };
        _userStore.GetAllAsync().Returns(users);

        // Act
        var result = await _plugin.ListAllUsers();

        // Assert
        result.Should().Contain("TOOL RESULT: Found 2 policy record(s):");
        result.Should().Contain("Session User");
        result.Should().Contain("System Seeded");
    }

    [Fact]
    public async Task ListAllUsers_ShouldReturnMessageWhenNoRecords()
    {
        // Arrange
        _userStore.GetAllAsync().Returns(Task.FromResult(new List<User>()));

        // Act
        var result = await _plugin.ListAllUsers();

        // Assert
        result.Should().Contain("📭 No policy records found");
    }

    [Fact]
    public async Task ListAllUsers_ShouldReturnMessageWhenNoRecordsForSession()
    {
        // Arrange
        var users = new List<User>
        {
            new User
            {
                Id = 1,
                FirstName = "Other",
                LastName = "User",
                PolicyNumber = "POL789",
                Email = "other@example.com",
                OwnerSessionId = "different-session-id",
                CreatedAt = DateTime.UtcNow
            }
        };
        _userStore.GetAllAsync().Returns(users);

        // Act
        var result = await _plugin.ListAllUsers();

        // Assert
        result.Should().Contain("📭 You haven't created any policy records yet");
    }
}
