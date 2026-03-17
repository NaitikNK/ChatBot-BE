namespace ChatBot_BE.Models
{
    public class UserUpdateRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PolicyNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}

