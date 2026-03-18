using Microsoft.SemanticKernel.ChatCompletion;

namespace ChatBot_BE.Services
{
    public class ChatSessionState
    {
        public ChatHistory History { get; set; } = new();

        /// <summary>
        /// Trims conversation history to prevent token limit issues.
        /// Keeps the system message plus the last N user/assistant messages.
        /// </summary>
        public void TrimHistory()
        {
            const int MaxMessages = 20; // Keep last 20 messages (excluding system message)

            // Separate system and non-system messages
            var systemMessages = History.Where(m => m.Role == AuthorRole.System).ToList();
            var otherMessages = History.Where(m => m.Role != AuthorRole.System).ToList();

            if (otherMessages.Count <= MaxMessages) return;

            // Keep only the most recent messages
            var messagesToKeep = otherMessages.Skip(otherMessages.Count - MaxMessages).ToList();

            // Rebuild history
            History = new ChatHistory();
            foreach (var msg in systemMessages)
            {
                History.Add(msg);
            }
            foreach (var msg in messagesToKeep)
            {
                History.Add(msg);
            }
        }
    }
}
