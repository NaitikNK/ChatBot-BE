using ChatBot_BE.Dto;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace ChatBot_BE.Services
{
    public class AIService : IAIService
    {
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatCompletion;
        private readonly IChatSessionStore _sessions;
        private readonly IConversationContext _context;
        private readonly ILogger<AIService> _logger;

        public AIService(
            Kernel kernel,
            IChatCompletionService chatCompletion,
            IChatSessionStore sessions,
            IConversationContext context,
            ILogger<AIService> logger)
        {
            _kernel = kernel;
            _chatCompletion = chatCompletion;
            _sessions = sessions;
            _context = context;
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
            _context.ConversationId = conversationId;

            var session = await _sessions.GetOrCreateAsync(conversationId);

            var authStatus = _context.IsAuthenticated ? "Authenticated User" : "Guest User";
            var systemPrompt = Shared.AiPrompts.SystemPrompt + $"\n\nCURRENT USER STATUS: {authStatus}";

            // Ensure the system message is always up-to-date at the start of history
            if (session.History.Count == 0 || session.History[0].Role != AuthorRole.System)
            {
                session.History.Insert(0, new ChatMessageContent(AuthorRole.System, systemPrompt));
            }
            else
            {
                // Update existing system message
                session.History[0] = new ChatMessageContent(AuthorRole.System, systemPrompt);
            }
            
            // Persist system prompt if it was a new session (optional but helps)
            if (session.History.Count == 1)
            {
                await _sessions.SaveMessageAsync(conversationId, "system", systemPrompt, null, null);
            }

            // Add and persist user message
            session.History.AddUserMessage(message);
            int? currentUserId = _context.UserId;
            int? currentRoleId = _context.RoleId;
            await _sessions.SaveMessageAsync(conversationId, "user", message, currentUserId, currentRoleId);

            // Configure execution settings — optimized for speed & token efficiency
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                Temperature = 0.3,   // Low = concise, deterministic answers
                MaxTokens = 800      // Sufficient for most insurance replies
            };

            try
            {
                _logger.LogDebug("Sending message to Gemini. ConversationId: {ConversationId}, MessageLength: {Length}",
                    conversationId, message.Length);

                var response = await _chatCompletion.GetChatMessageContentAsync(
                    session.History,
                    executionSettings,
                    _kernel);

                var answer = response.Content?.Trim() ?? "I'm processing that for you.";

                // Add and persist assistant response
                session.History.AddAssistantMessage(answer);
                await _sessions.SaveMessageAsync(conversationId, "assistant", answer, currentUserId, currentRoleId);

                // Trim history to prevent token limit issues
                session.TrimHistory();

                _logger.LogDebug("Received response from Gemini. ConversationId: {ConversationId}, AnswerLength: {Length}",
                    conversationId, answer.Length);

                return new ChatReply
                {
                    ConversationId = conversationId,
                    Answer = answer
                };
            }
            catch (Exception ex)
            {
                var unavailable = GeminiServiceUnavailableException.FindIn(ex);
                if (unavailable is not null)
                {
                    throw unavailable;
                }

                _logger.LogError(ex, 
                    "Failed to get response from Gemini. Message: {Message}, InnerException: {InnerException}",
                    ex.Message, 
                    ex.InnerException?.Message ?? "none");

                // We keep the user message in DB as a record of what happened even if AI failed
                return new ChatReply
                {
                    ConversationId = conversationId,
                    Answer = "Sorry, I'm having trouble connecting right now. Please try again."
                };
            }
        }

        public async Task<string> GetGreetingAsync()
        {
            // Returning a static greeting to ensure instantaneous response times,
            // as the greeting rules are strict and constant.
            return await Task.FromResult("Hello! I'm Allison, your personal Insurance Agent. I'm here to help you explore our insurance offerings, manage your records, and answer any questions you may have. How can I assist you today?");
        }
    }
}
