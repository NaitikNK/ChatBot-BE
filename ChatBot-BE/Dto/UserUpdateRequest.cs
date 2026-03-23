using System.ComponentModel.DataAnnotations;

namespace ChatBot_BE.Dto
{
    public class UserUpdateRequest
    {
        [Required(ErrorMessage = "First name is required.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email is invalid.")]
        public string Email { get; set; } = string.Empty;

        public string? Password { get; set; }
        
        [Required]
        public int RoleId { get; set; }
    }
}
