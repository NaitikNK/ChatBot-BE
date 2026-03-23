using ChatBot_BE.Model;
using Microsoft.Extensions.Caching.Memory;

namespace ChatBot_BE.Services
{
    public class InMemoryChatSessionStore : IChatSessionStore
    {
        private readonly IMemoryCache _cache;

        public InMemoryChatSessionStore(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task<ChatSessionState> GetOrCreateAsync(string conversationId)
        {
            var session = _cache.GetOrCreate(conversationId, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(12);
                return new ChatSessionState();
            });

            return Task.FromResult(session ?? new ChatSessionState());
        }

        public void Reset(string conversationId)
        {
            _cache.Remove(conversationId);
        }

        public Task SaveMessageAsync(string conversationId, string role, string content, int? userId)
        {
            return Task.CompletedTask;
        }

        public Task<List<string>> GetSessionsForUserAsync(int userId)
        {
            return Task.FromResult(new List<string>());
        }

        public Task<List<ChatMessage>> GetHistoryAsync(string conversationId)
        {
            return Task.FromResult(new List<ChatMessage>());
        }

        public Task DeleteHistoryAsync(string conversationId)
        {
            _cache.Remove(conversationId);
            return Task.CompletedTask;
        }

        public Task DeleteAllHistoryAsync(int userId)
        {
            // In-memory cache doesn't track users easily, but we can't do much here without major refactor
            return Task.CompletedTask;
        }
    }
}

