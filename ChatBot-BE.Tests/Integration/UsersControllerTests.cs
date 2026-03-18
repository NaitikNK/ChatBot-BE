using System.Net;
using System.Net.Http.Json;
using ChatBot_BE.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ChatBot_BE.Tests.Integration;

public class UsersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public UsersControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ShouldReturnSuccessWithList()
    {
        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByPolicyNumber_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/users/by-policy/NONEXISTENT");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ShouldCreateUser()
    {
        // Arrange
        var request = new UserCreateRequest
        {
            FirstName = "John",
            LastName = "Doe",
            PolicyNumber = "POL123",
            Email = "john@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.FirstName.Should().Be("John");
        result.Data.LastName.Should().Be("Doe");
        result.Data.PolicyNumber.Should().Be("POL123");
        result.Data.Email.Should().Be("john@example.com");
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequestWhenMissingFields()
    {
        // Arrange
        var request = new UserCreateRequest
        {
            FirstName = "", // Invalid
            LastName = "Doe",
            PolicyNumber = "POL123",
            Email = "john@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturnConflictWhenPolicyNumberExists()
    {
        // Arrange - Create first user
        var request1 = new UserCreateRequest
        {
            FirstName = "John",
            LastName = "Doe",
            PolicyNumber = "POL123",
            Email = "john@example.com"
        };
        await _client.PostAsJsonAsync("/api/users", request1);

        // Act - Try to create with same policy number
        var request2 = new UserCreateRequest
        {
            FirstName = "Jane",
            LastName = "Doe",
            PolicyNumber = "POL123", // Duplicate
            Email = "jane@example.com"
        };
        var response = await _client.PostAsJsonAsync("/api/users", request2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_ShouldReturnConflictWhenEmailExists()
    {
        // Arrange - Create first user
        var request1 = new UserCreateRequest
        {
            FirstName = "John",
            LastName = "Doe",
            PolicyNumber = "POL123",
            Email = "john@example.com"
        };
        await _client.PostAsJsonAsync("/api/users", request1);

        // Act - Try to create with same email
        var request2 = new UserCreateRequest
        {
            FirstName = "Jane",
            LastName = "Doe",
            PolicyNumber = "POL456",
            Email = "john@example.com" // Duplicate
        };
        var response = await _client.PostAsJsonAsync("/api/users", request2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteByPolicyNumber_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/api/users/by-policy/NONEXISTENT");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
