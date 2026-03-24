namespace ChatBot_BE.Services
{
    /// <summary>
    /// Holds per-request conversation context for use in plugins.
    /// </summary>
    public interface IConversationContext
    {
        string ConversationId { get; set; }
        int? UserId { get; set; }
        int? RoleId { get; set; }
        bool IsAuthenticated { get; set; }
        string Role { get; set; }
    }

    public class ConversationContext : IConversationContext
    {
        public string ConversationId { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public int? RoleId { get; set; }
        public bool IsAuthenticated { get; set; } = false;
        public string Role { get; set; } = "Guest";
    }
}
