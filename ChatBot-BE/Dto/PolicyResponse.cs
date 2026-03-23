using System;

namespace ChatBot_BE.Dto
{
    public class PolicyResponse
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PolicyNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int? PolicyTypeId { get; set; }
        public string? PolicyType { get; set; }
        public int? PolicyNameId { get; set; }
        public string? PolicyName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UserId { get; set; }
    }
}
