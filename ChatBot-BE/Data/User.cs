using System;
using System.ComponentModel.DataAnnotations;

namespace ChatBot_BE.Data
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

        public string OwnerSessionId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
