namespace ChatBot_BE.Services
{
    /// <summary>
    /// Holds per-request conversation context for use in plugins.
    /// </summary>
    public interface IConversationContext
    {
        string ConversationId { get; set; }
    }

    public class ConversationContext : IConversationContext
    {
        public string ConversationId { get; set; } = string.Empty;
    }
}
