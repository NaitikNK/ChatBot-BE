namespace ChatBot_BE.Dto
{
    public class UserUpdateRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PolicyNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PolicyType { get; set; } = "Personal";
        public string? PolicyName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }
}
