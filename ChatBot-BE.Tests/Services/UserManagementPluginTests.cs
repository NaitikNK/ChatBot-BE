using ChatBot_BE.Data;
using ChatBot_BE.Services;
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
        _userStore.GetByPolicyNumberAsync("POL123").Returns((User?)null);
        _userStore.GetByEmailAsync("john@example.com").Returns((User?)null);
        _userStore.AddAsync(Arg.Any<User>()).Returns(new User
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            PolicyNumber = "POL123",
            Email = "john@example.com",
            OwnerSessionId = _context.ConversationId,
            CreatedAt = DateTime.UtcNow
        });

        // Act
        var result = await _plugin.CreateUser("John", "Doe", "POL123", "john@example.com");

        // Assert
        result.Should().Contain("✅ Record created successfully");
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
        _userStore.GetByPolicyNumberAsync("POL123").Returns(new User
        {
            Id = 1,
            FirstName = "Existing",
            LastName = "User",
            PolicyNumber = "POL123",
            Email = "existing@example.com",
            OwnerSessionId = _context.ConversationId,
            CreatedAt = DateTime.UtcNow
        });

        // Act
        var result = await _plugin.CreateUser("John", "Doe", "POL123", "john@example.com");

        // Assert
        result.Should().Contain("⚠️ Policy number **POL123** already exists");
    }

    [Fact]
    public async Task CreateUser_ShouldReturnErrorWhenEmailExists()
    {
        // Arrange
        _userStore.GetByPolicyNumberAsync("POL123").Returns((User?)null);
        _userStore.GetByEmailAsync("john@example.com").Returns(new User
        {
            Id = 1,
            FirstName = "Existing",
            LastName = "User",
            PolicyNumber = "POL456",
            Email = "john@example.com",
            OwnerSessionId = _context.ConversationId,
            CreatedAt = DateTime.UtcNow
        });

        // Act
        var result = await _plugin.CreateUser("John", "Doe", "POL123", "john@example.com");

        // Assert
        result.Should().Contain("⚠️ Email **john@example.com** is already in use");
    }

    [Fact]
    public async Task ViewUser_ShouldReturnUserDetails()
    {
        // Arrange
        _userStore.GetByPolicyNumberAsync("POL123").Returns(new User
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            PolicyNumber = "POL123",
            Email = "john@example.com",
            OwnerSessionId = _context.ConversationId,
            CreatedAt = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc)
        });

        // Act
        var result = await _plugin.ViewUser("POL123");

        // Assert
        result.Should().Contain("📋 Record found");
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
    public async Task DeleteUser_ShouldReturnSuccessMessage()
    {
        // Arrange
        _userStore.GetByPolicyNumberAsync("POL123").Returns(new User
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            PolicyNumber = "POL123",
            Email = "john@example.com",
            OwnerSessionId = _context.ConversationId,
            CreatedAt = DateTime.UtcNow
        });
        _userStore.DeleteByPolicyNumberAsync("POL123").Returns(true);

        // Act
        var result = await _plugin.DeleteUser("POL123");

        // Assert
        result.Should().Contain("🗑️ Record with policy number **POL123** has been deleted");
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
    public async Task ListAllUsers_ShouldReturnAllRecordsForSession()
    {
        // Arrange
        var users = new List<User>
        {
            new User
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                PolicyNumber = "POL123",
                Email = "john@example.com",
                OwnerSessionId = _context.ConversationId,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = 2,
                FirstName = "Jane",
                LastName = "Smith",
                PolicyNumber = "POL456",
                Email = "jane@example.com",
                OwnerSessionId = _context.ConversationId,
                CreatedAt = DateTime.UtcNow
            }
        };
        _userStore.GetAllAsync().Returns(users);

        // Act
        var result = await _plugin.ListAllUsers();

        // Assert
        result.Should().Contain("📋 Found **2** policy record(s)");
        result.Should().Contain("John Doe");
        result.Should().Contain("Jane Smith");
    }

    [Fact]
    public async Task ListAllUsers_ShouldReturnMessageWhenNoRecords()
    {
        // Arrange
        _userStore.GetAllAsync().Returns(new List<User>());

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
