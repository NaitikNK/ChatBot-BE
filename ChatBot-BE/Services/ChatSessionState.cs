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
            const int MaxMessages = 5;  // Keep only last 5 messages for token efficiency
            const int SummaryThreshold = 5; // Summarize as soon as we exceed 5

            // Separate system and non-system messages
            var systemMessages = History.Where(m => m.Role == AuthorRole.System).ToList();
            var otherMessages = History.Where(m => m.Role != AuthorRole.System).ToList();

            if (otherMessages.Count <= MaxMessages) return;

            // Build a rolling summary from the messages we're about to trim
            var messagesToTrim = otherMessages.Take(otherMessages.Count - MaxMessages).ToList();
            Summary = BuildRollingSummary(Summary, messagesToTrim);

            // Keep only the most recent messages
            var messagesToKeep = otherMessages.Skip(otherMessages.Count - MaxMessages).ToList();

            // Rebuild history
            History = new ChatHistory();
            
            // Add system message
            foreach (var msg in systemMessages)
            {
                History.Add(msg);
            }
            
            // Inject summary as memory context
            if (!string.IsNullOrEmpty(Summary))
            {
                History.AddSystemMessage($"[Conversation Memory]\n{Summary}\n[End Memory]");
            }
            
            // Add recent messages
            foreach (var msg in messagesToKeep)
            {
                History.Add(msg);
            }
        }

        /// <summary>
        /// Builds a rolling summary by appending new trimmed messages to existing summary.
        /// Captures both user questions and key assistant actions for full context.
        /// </summary>
        private static string BuildRollingSummary(string? existingSummary, List<ChatMessageContent> trimmedMessages)
        {
            var sb = new System.Text.StringBuilder();

            // Carry forward existing summary
            if (!string.IsNullOrWhiteSpace(existingSummary))
            {
                sb.AppendLine(existingSummary.Trim());
            }

            // Add new context from trimmed messages
            foreach (var msg in trimmedMessages)
            {
                var text = msg.Content?.Trim();
                if (string.IsNullOrEmpty(text) || text.Length > 200) continue;

                if (msg.Role == AuthorRole.User)
                {
                    sb.AppendLine($"- User asked: {Truncate(text, 80)}");
                }
                else if (msg.Role == AuthorRole.Assistant)
                {
                    // Capture key actions (policy created, viewed, etc.)
                    if (text.Contains("Policy Number", StringComparison.OrdinalIgnoreCase) ||
                        text.Contains("successfully", StringComparison.OrdinalIgnoreCase) ||
                        text.Contains("created", StringComparison.OrdinalIgnoreCase) ||
                        text.Contains("updated", StringComparison.OrdinalIgnoreCase) ||
                        text.Contains("deleted", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.AppendLine($"- Allison: {Truncate(text, 100)}");
                    }
                }
            }

            // Cap total summary length to prevent unbounded growth
            var result = sb.ToString().Trim();
            if (result.Length > 600)
            {
                result = result[..600] + "...";
            }

            return result;
        }

        private static string Truncate(string text, int maxLen)
        {
            return text.Length <= maxLen ? text : text[..(maxLen - 1)] + "…";
        }
    }
}
