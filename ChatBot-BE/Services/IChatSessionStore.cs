using ChatBot_BE.Model;

namespace ChatBot_BE.Services
{
    public interface IChatSessionStore
    {
        Task<ChatSessionState> GetOrCreateAsync(string conversationId);
        void Reset(string conversationId);
        Task SaveMessageAsync(string conversationId, string authorType, string content, int? userId, int? roleId);
        Task<List<string>> GetSessionsForUserAsync(int userId);
        Task<List<ChatMessage>> GetHistoryAsync(string conversationId);
        Task DeleteHistoryAsync(string conversationId);
        Task DeleteAllHistoryAsync(int userId);
    }
}

