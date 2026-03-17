namespace ChatBot_BE.Services
{
    public interface IAIService
    {
        Task<ChatBot_BE.Models.ChatReply> GetResponse(ChatBot_BE.Models.ChatRequest request);
    }
}
