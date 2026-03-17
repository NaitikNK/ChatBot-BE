namespace ChatBot_BE.Services
{
    public interface IChatSessionStore
    {
        ChatSessionState GetOrCreate(string conversationId);
        void Reset(string conversationId);
    }
}

