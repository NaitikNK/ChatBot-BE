using System;
using System.ComponentModel.DataAnnotations;

namespace ChatBot_BE.Model
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Policy number is required.")]
        [MaxLength(64, ErrorMessage = "Policy number is too long.")]
        public string PolicyNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email is invalid. Please enter a valid email (example: name@example.com).")]
        [StringLength(254, ErrorMessage = "Email is too long.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Policy type is required.")]
        public string PolicyType { get; set; } = "Personal";

        public string? PolicyName { get; set; }

        [MaxLength(128, ErrorMessage = "Phone number is too long.")]
        public string? PhoneNumber { get; set; }

        [MaxLength(256, ErrorMessage = "Address is too long.")]
        public string? Address { get; set; }

        [MaxLength(64, ErrorMessage = "City is too long.")]
        public string? City { get; set; }

        [MaxLength(64, ErrorMessage = "State is too long.")]
        public string? State { get; set; }

        [MaxLength(20, ErrorMessage = "Postal code is too long.")]
        public string? PostalCode { get; set; }

        [MaxLength(64, ErrorMessage = "Country is too long.")]
        public string? Country { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public string OwnerSessionId { get; set; } = string.Empty;
    }
}
