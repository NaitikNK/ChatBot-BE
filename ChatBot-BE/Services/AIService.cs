using ChatBot_BE.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace ChatBot_BE.Services
{
    public class AIService : IAIService
    {
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatCompletion;
        private readonly IChatSessionStore _sessions;
        private readonly UserManagementPlugin _plugin;
        private readonly ILogger<AIService> _logger;

        public AIService(
            Kernel kernel,
            IChatCompletionService chatCompletion,
            IChatSessionStore sessions,
            UserManagementPlugin plugin,
            ILogger<AIService> logger)
        {
            _kernel = kernel;
            _chatCompletion = chatCompletion;
            _sessions = sessions;
            _plugin = plugin;
            _logger = logger;
        }

        public async Task<ChatReply> GetResponse(ChatRequest request)
        {
            var conversationId = string.IsNullOrWhiteSpace(request.ConversationId)
                ? Guid.NewGuid().ToString("N")
                : request.ConversationId.Trim();

            var message = (request.Message ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(message))
            {
                return new ChatReply
                {
                    ConversationId = conversationId,
                    Answer = "Please type a message to get started."
                };
            }

            // Set the conversation context on the plugin so it can do session-ownership checks
            _plugin.ConversationId = conversationId;

            var session = _sessions.GetOrCreate(conversationId);

            // Add the system prompt if this is a fresh conversation
            if (session.History.Count == 0)
            {
                session.History.AddSystemMessage(Shared.AiPrompts.SystemPrompt);
            }

            // Add user message
            session.History.AddUserMessage(message);

            // Configure Gemini to automatically invoke plugin functions
            var executionSettings = new GeminiPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            try
            {
                var response = await _chatCompletion.GetChatMessageContentAsync(
                    session.History,
                    executionSettings,
                    _kernel);

                var answer = response.Content?.Trim() ?? "I'm processing that for you.";

                // Add assistant response to history
                session.History.AddAssistantMessage(answer);

                // Trim history to prevent token limit issues
                session.TrimHistory();

                return new ChatReply
                {
                    ConversationId = conversationId,
                    Answer = answer
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get response from Gemini via Semantic Kernel. Message: {Msg}, Inner: {Inner}",
                    ex.Message, ex.InnerException?.Message ?? "none");

                // Temporarily write full error to a file for debugging
                var errorDetails = $"[{DateTime.UtcNow:O}] Exception: {ex.GetType().FullName}\nMessage: {ex.Message}\nInner: {ex.InnerException?.Message}\nInnerType: {ex.InnerException?.GetType().FullName}\nStack: {ex.StackTrace}";
                await File.AppendAllTextAsync("error_log.txt", errorDetails + "\n\n");

                // Remove the user message we just added since we failed
                if (session.History.Count > 0)
                    session.History.RemoveAt(session.History.Count - 1);

                return new ChatReply
                {
                    ConversationId = conversationId,
                    Answer = "Sorry, I'm having trouble connecting right now. Please try again."
                };
            }
        }
    }
}
