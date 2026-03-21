using ChatBot_BE.Dto;

namespace ChatBot_BE.Services
{
    public interface IAIService
    {
        Task<ChatReply> GetResponse(ChatRequest request);
        Task<string> GetGreetingAsync();
    }
}
