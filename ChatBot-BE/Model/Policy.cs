using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatBot_BE.Model
{
    public class Policy
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Policy number is required.")]
        [MaxLength(64, ErrorMessage = "Policy number is too long.")]
        public string PolicyNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email is invalid.")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.com$", ErrorMessage = "Email must be a valid .com address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Policy type is required.")]
        public string PolicyType { get; set; } = "Personal";

        public string? PolicyName { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be exactly 10 digits.")]
        public string? PhoneNumber { get; set; }

        [MaxLength(256)]
        public string? Address { get; set; }

        [MaxLength(64)]
        public string? City { get; set; }

        [MaxLength(64)]
        public string? State { get; set; }

        [Required(ErrorMessage = "Zip code is required.")]
        [RegularExpression(@"^\d{5,6}$", ErrorMessage = "Zip code must be 5 or 6 digits.")]
        public string? PostalCode { get; set; }

        [MaxLength(64)]
        public string? Country { get; set; }

        [Required(ErrorMessage = "Date of birth is required.")]
        public DateTime? DateOfBirth { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public string OwnerSessionId { get; set; } = string.Empty;

        [ForeignKey("User")]
        public int? UserId { get; set; }
        public User? User { get; set; }
    }
}
