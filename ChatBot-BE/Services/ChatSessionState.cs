using Microsoft.SemanticKernel.ChatCompletion;

namespace ChatBot_BE.Services
{
    public class ChatSessionState
    {
        public ChatHistory History { get; set; } = new();

        /// <summary>
        /// Trims conversation history to prevent token limit issues.
        /// Keeps the system message (index 0) plus the last N user/assistant messages.
        /// </summary>
        public void TrimHistory()
        {
            const int MaxMessages = 20; // Keep last 20 messages (excluding system message)

            // Count non-system messages
            int nonSystemCount = 0;
            for (int i = 0; i < History.Count; i++)
            {
                if (History[i].Role != AuthorRole.System)
                    nonSystemCount++;
            }

            if (nonSystemCount <= MaxMessages) return;

            int toRemove = nonSystemCount - MaxMessages;
            int removed = 0;

            for (int i = History.Count - 1; i >= 0 && removed < toRemove; i--)
            {
                // Find the earliest non-system messages to remove
            }

            // Rebuild: keep system message + trim oldest non-system messages
            var systemMessages = History.Where(m => m.Role == AuthorRole.System).ToList();
            var otherMessages = History.Where(m => m.Role != AuthorRole.System).ToList();

            if (otherMessages.Count > MaxMessages)
            {
                otherMessages = otherMessages.Skip(otherMessages.Count - MaxMessages).ToList();
            }

            History = new ChatHistory();
            foreach (var msg in systemMessages)
            {
                History.Add(msg);
            }
            foreach (var msg in otherMessages)
            {
                History.Add(msg);
            }
        }
    }
}
