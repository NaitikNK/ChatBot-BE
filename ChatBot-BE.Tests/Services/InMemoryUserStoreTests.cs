using ChatBot_BE.Data;
using ChatBot_BE.Services;
using FluentAssertions;

namespace ChatBot_BE.Tests.Services;

public class InMemoryUserStoreTests
{
    private readonly InMemoryUserStore _store;

    public InMemoryUserStoreTests()
    {
        _store = new InMemoryUserStore();
    }

    [Fact]
    public async Task AddAsync_ShouldCreateUserWithAllFields()
    {
        // Arrange
        var user = new User
        {
            FirstName = "John",
            LastName = "Doe",
            PolicyNumber = "POL123",
            Email = "john@example.com"
        };

        // Act
        var created = await _store.AddAsync(user);

        // Assert
        created.Id.Should().BeGreaterThan(0);
        created.FirstName.Should().Be("John");
        created.LastName.Should().Be("Doe");
        created.PolicyNumber.Should().Be("POL123");
        created.Email.Should().Be("john@example.com");
        created.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AddAsync_ShouldThrowWhenPolicyNumberExists()
    {
        // Arrange
        var user1 = new User
        {
            FirstName = "John",
            LastName = "Doe",
            PolicyNumber = "POL123",
            Email = "john@example.com"
        };
        await _store.AddAsync(user1);

        var user2 = new User
        {
            FirstName = "Jane",
            LastName = "Doe",
            PolicyNumber = "POL123", // Duplicate
            Email = "jane@example.com"
        };

        // Act & Assert
        await _store.Invoking(s => s.AddAsync(user2))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("PolicyNumber already exists.");
    }

    [Fact]
    public async Task AddAsync_ShouldThrowWhenEmailExists()
    {
        // Arrange
        var user1 = new User
        {
            FirstName = "John",
            LastName = "Doe",
            PolicyNumber = "POL123",
            Email = "john@example.com"
        };
        await _store.AddAsync(user1);

        var user2 = new User
        {
            FirstName = "Jane",
            LastName = "Doe",
            PolicyNumber = "POL456",
            Email = "john@example.com" // Duplicate
        };

        // Act & Assert
        await _store.Invoking(s => s.AddAsync(user2))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email already exists.");
    }

    [Fact]
    public async Task GetByPolicyNumberAsync_ShouldReturnUser()
    {
        // Arrange
        var user = new User
        {
            FirstName = "John",
            LastName = "Doe",
            PolicyNumber = "POL123",
            Email = "john@example.com"
        };
        await _store.AddAsync(user);

        // Act
        var found = await _store.GetByPolicyNumberAsync("POL123");

        // Assert
        found.Should().NotBeNull();
        found!.PolicyNumber.Should().Be("POL123");
        found.Email.Should().Be("john@example.com");
    }

    [Fact]
    public async Task GetByPolicyNumberAsync_ShouldReturnNullWhenNotFound()
    {
        // Act
        var found = await _store.GetByPolicyNumberAsync("NONEXISTENT");

        // Assert
        found.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnUser()
    {
        // Arrange
        var user = new User
        {
            FirstName = "John",
            LastName = "Doe",
            PolicyNumber = "POL123",
            Email = "john@example.com"
        };
        await _store.AddAsync(user);

        // Act
        var found = await _store.GetByEmailAsync("john@example.com");

        // Assert
        found.Should().NotBeNull();
        found!.Email.Should().Be("john@example.com");
    }

    [Fact]
    public async Task DeleteByPolicyNumberAsync_ShouldRemoveUser()
    {
        // Arrange
        var user = new User
        {
            FirstName = "John",
            LastName = "Doe",
            PolicyNumber = "POL123",
            Email = "john@example.com"
        };
        await _store.AddAsync(user);

        // Act
        var deleted = await _store.DeleteByPolicyNumberAsync("POL123");

        // Assert
        deleted.Should().BeTrue();
        var found = await _store.GetByPolicyNumberAsync("POL123");
        found.Should().BeNull();
    }

    [Fact]
    public async Task DeleteByPolicyNumberAsync_ShouldReturnFalseWhenNotFound()
    {
        // Act
        var deleted = await _store.DeleteByPolicyNumberAsync("NONEXISTENT");

        // Assert
        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllUsersOrderedByCreatedAt()
    {
        // Arrange
        await _store.AddAsync(new User
        {
            FirstName = "John",
            LastName = "Doe",
            PolicyNumber = "POL123",
            Email = "john@example.com"
        });
        await Task.Delay(10); // Ensure different CreatedAt
        await _store.AddAsync(new User
        {
            FirstName = "Jane",
            LastName = "Doe",
            PolicyNumber = "POL456",
            Email = "jane@example.com"
        });

        // Act
        var users = await _store.GetAllAsync();

        // Assert
        users.Should().HaveCount(2);
        users[0].PolicyNumber.Should().Be("POL456"); // Most recent first
        users[1].PolicyNumber.Should().Be("POL123");
    }
}
