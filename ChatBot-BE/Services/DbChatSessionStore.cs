using ChatBot_BE.Data;
using ChatBot_BE.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ChatBot_BE.Services
{
    public class DbChatSessionStore : IChatSessionStore
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DbChatSessionStore> _logger;

        public DbChatSessionStore(AppDbContext context, IMemoryCache cache, ILogger<DbChatSessionStore> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ChatSessionState> GetOrCreateAsync(string conversationId)
        {
            var cacheKey = $"chat_session_{conversationId}";
            if (_cache.TryGetValue(cacheKey, out ChatSessionState? session) && session != null)
            {
                return session;
            }

            session = new ChatSessionState();
            
            // Load messages from DB
            var messages = await _context.ChatMessages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            if (messages.Any())
            {
                session.History = new ChatHistory();
                foreach (var msg in messages)
                {
                    var role = msg.Role.ToLower() switch
                    {
                        "system" => AuthorRole.System,
                        "user" => AuthorRole.User,
                        "assistant" => AuthorRole.Assistant,
                        _ => AuthorRole.User
                    };
                    session.History.AddMessage(role, msg.Content);
                }
                session.MessageCount = messages.Count;
                session.LastActivity = messages.Last().CreatedAt;
            }

            _cache.Set(cacheKey, session, TimeSpan.FromHours(12));
            return session;
        }

        public void Reset(string conversationId)
        {
            var cacheKey = $"chat_session_{conversationId}";
            _cache.Remove(cacheKey);
            // Optionally we could mark messages as deleted in DB, but usually Reset just clears session state
        }

        public async Task SaveMessageAsync(string conversationId, string role, string content, int? userId)
        {
            var message = new ChatMessage
            {
                ConversationId = conversationId,
                Role = role,
                Content = content,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            // Only persist to DB if there is a real user ID
            if (userId != null)
            {
                _context.ChatMessages.Add(message);
                await _context.SaveChangesAsync();
            }

            // Always update cache for the current session
            var cacheKey = $"chat_session_{conversationId}";
            if (_cache.TryGetValue(cacheKey, out ChatSessionState? session) && session != null)
            {
                session.History.AddMessage(role.ToLower() switch
                {
                    "system" => AuthorRole.System,
                    "user" => AuthorRole.User,
                    "assistant" => AuthorRole.Assistant,
                    _ => AuthorRole.User
                }, content);
                session.MessageCount++;
                session.LastActivity = message.CreatedAt;
            }
        }

        public async Task<List<string>> GetSessionsForUserAsync(int userId)
        {
            return await _context.ChatMessages
                .Where(m => m.UserId == userId)
                .Select(m => m.ConversationId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<ChatMessage>> GetHistoryAsync(string conversationId)
        {
            return await _context.ChatMessages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task DeleteHistoryAsync(string conversationId)
        {
            var messages = await _context.ChatMessages
                .Where(m => m.ConversationId == conversationId)
                .ToListAsync();

            if (messages.Count > 0)
            {
                _context.ChatMessages.RemoveRange(messages);
                await _context.SaveChangesAsync();
            }

            _cache.Remove($"chat_session_{conversationId}");
        }

        public async Task DeleteAllHistoryAsync(int userId)
        {
            var messages = await _context.ChatMessages
                .Where(m => m.UserId == userId)
                .ToListAsync();

            if (messages.Count > 0)
            {
                var sessionIds = messages.Select(m => m.ConversationId).Distinct();
                foreach (var id in sessionIds)
                {
                    _cache.Remove($"chat_session_{id}");
                }

                _context.ChatMessages.RemoveRange(messages);
                await _context.SaveChangesAsync();
            }
        }
    }
}
