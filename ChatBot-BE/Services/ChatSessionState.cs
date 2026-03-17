using ChatBot_BE.Models;

namespace ChatBot_BE.Services
{
    public class ChatSessionState
    {
        public List<ConversationMessage> History { get; set; } = new();

        public void AddUserMessage(string content)
        {
            History.Add(new ConversationMessage { Role = "user", Content = content });
            TrimHistory();
        }

        public void AddModelMessage(string content)
        {
            History.Add(new ConversationMessage { Role = "model", Content = content });
            TrimHistory();
        }

        private void TrimHistory()
        {
            const int MaxMessages = 20; // Keep last 20 messages to prevent token limit issues
            if (History.Count > MaxMessages)
            {
                History.RemoveRange(0, History.Count - MaxMessages);
            }
        }
    }
}

