namespace ChatBot_BE.Models
{
    public class ConversationMessage
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
    }
}
