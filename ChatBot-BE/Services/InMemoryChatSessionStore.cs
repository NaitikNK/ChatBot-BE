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

        public ChatSessionState GetOrCreate(string conversationId)
        {
            var session = _cache.GetOrCreate(conversationId, entry =>
            {
                // Expire memory-heavy chat sessions after 12 hours of inactivity
                entry.SlidingExpiration = TimeSpan.FromHours(12);
                return new ChatSessionState();
            });

            return session ?? new ChatSessionState();
        }

        public void Reset(string conversationId)
        {
            _cache.Remove(conversationId);
        }
    }
}

