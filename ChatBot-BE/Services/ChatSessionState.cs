using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ChatBot_BE.Services
{
    public class ChatSessionState
    {
        public ChatHistory History { get; set; } = new();
        
        /// <summary>
        /// Stores the conversation summary for long-term context
        /// </summary>
        public string? Summary { get; set; }
        
        /// <summary>
        /// Tracks the number of messages exchanged in this session
        /// </summary>
        public int MessageCount { get; set; }
        
        /// <summary>
        /// Timestamp of last activity
        /// </summary>
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Trims conversation history to prevent token limit issues.
        /// Keeps the system message plus the last N user/assistant messages.
        /// After trimming, creates a summary to preserve context.
        /// </summary>
        public void TrimHistory()
        {
            const int MaxMessages = 30; // Keep last 30 messages (excluding system message)
            const int SummaryThreshold = 20; // Create summary after this many messages

            // Separate system and non-system messages
            var systemMessages = History.Where(m => m.Role == AuthorRole.System).ToList();
            var otherMessages = History.Where(m => m.Role != AuthorRole.System).ToList();

            if (otherMessages.Count <= MaxMessages) return;

            // Create summary before trimming if we have enough messages
            if (otherMessages.Count >= SummaryThreshold && string.IsNullOrEmpty(Summary))
            {
                Summary = CreateSummaryFromHistory(otherMessages);
            }

            // Keep only the most recent messages
            var messagesToKeep = otherMessages.Skip(otherMessages.Count - MaxMessages).ToList();

            // Rebuild history
            History = new ChatHistory();
            
            // Add system message
            foreach (var msg in systemMessages)
            {
                History.Add(msg);
            }
            
            // Add summary as context if available
            if (!string.IsNullOrEmpty(Summary))
            {
                History.AddSystemMessage($"[Conversation Summary]\n{Summary}\n[End of Summary]");
            }
            
            // Add recent messages
            foreach (var msg in messagesToKeep)
            {
                History.Add(msg);
            }
        }

        /// <summary>
        /// Creates a brief summary from conversation history
        /// </summary>
        private static string CreateSummaryFromHistory(List<ChatMessageContent> messages)
        {
            var summary = new System.Text.StringBuilder();
            summary.AppendLine("Key topics discussed:");
            
            // Extract key topics from user messages
            var userMessages = messages.Where(m => m.Role == AuthorRole.User).TakeLast(10);
            foreach (var msg in userMessages)
            {
                var text = msg.Content?.Trim();
                if (!string.IsNullOrEmpty(text) && text.Length < 100)
                {
                    summary.AppendLine($"- User inquiry about: {text}");
                }
            }
            
            return summary.ToString();
        }
    }
}
