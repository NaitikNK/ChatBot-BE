using System.Net;
using System.Net.Http.Json;
using ChatBot_BE.Dto;
using ChatBot_BE.Model;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ChatBot_BE.Tests.Integration
{
    public class RobustLookupTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public RobustLookupTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetUser_ShouldReturnPolicyNameId_EvenWithMissingSpaces()
        {
            // Arrange
            // Create a user with "FamilyProtectionPlan" (no spaces)
            // Note: User 5 (Sneha Reddy) already has this in UserSeeder (updated in my fix to have spaces, 
            // but the controller lookup should handle it regardless)
            
            var request = new UserCreateRequest
            {
                FirstName = "Test",
                LastName = "User",
                PolicyNumber = "POL_ROBUST_TEST",
                Email = "robust@example.com",
                PolicyType = "Personal",
                PolicyName = "FamilyProtectionPlan" // Missing spaces
            };

            var postResponse = await _client.PostAsJsonAsync("/api/users", request);
            postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            // Act
            var getResponse = await _client.GetAsync($"/api/users/by-policy/POL_ROBUST_TEST");
            
            // Assert
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await getResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
            
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.PolicyName.Should().Be("Family Protection Plan"); // Canonical name from master
            result.Data.PolicyNameId.Should().Be(3); // ID from master
        }

        [Fact]
        public async Task GetUser_ShouldReturnPolicyTypeId_EvenWithCaseMismatch()
        {
            // Arrange
            var request = new UserCreateRequest
            {
                FirstName = "Test",
                LastName = "User",
                PolicyNumber = "POL_CASE_TEST",
                Email = "case@example.com",
                PolicyType = "vehicle", // lowercase
                PolicyName = "Auto Insurance"
            };

            var postResponse = await _client.PostAsJsonAsync("/api/users", request);
            postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            // Act
            var getResponse = await _client.GetAsync($"/api/users/by-policy/POL_CASE_TEST");
            
            // Assert
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await getResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
            
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.PolicyType.Should().Be("Vehicle"); // Canonical name
            result.Data.PolicyTypeId.Should().Be(2); // ID for Vehicle
        }
    }
}
