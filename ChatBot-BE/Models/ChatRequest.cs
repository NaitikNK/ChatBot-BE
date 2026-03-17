namespace ChatBot_BE.Models
{
    public class ChatRequest
    {
        public string? ConversationId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
