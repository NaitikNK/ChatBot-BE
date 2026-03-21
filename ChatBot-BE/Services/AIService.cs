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
        private readonly UserManagementPlugin _plugin;
        private readonly ILogger<AIService> _logger;

        public AIService(
            Kernel kernel,
            IChatCompletionService chatCompletion,
            IChatSessionStore sessions,
            IConversationContext context,
            UserManagementPlugin plugin,
            ILogger<AIService> logger)
        {
            _kernel = kernel;
            _chatCompletion = chatCompletion;
            _sessions = sessions;
            _context = context;
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
            _context.ConversationId = conversationId;

            var session = _sessions.GetOrCreate(conversationId);

            // Add the system prompt if this is a fresh conversation
            if (session.History.Count == 0)
            {
                session.History.AddSystemMessage(Shared.AiPrompts.SystemPrompt);
            }

            // Add user message
            session.History.AddUserMessage(message);

            // Configure execution settings for Kimi (OpenAI API)
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                Temperature = 1,        // Set according to user preference
                TopP = 0.9,             // Set according to user preference
                MaxTokens = 2000        // Maximum response length
            };

            try
            {
                _logger.LogDebug("Sending message to OpenAI. ConversationId: {ConversationId}, MessageLength: {Length}",
                    conversationId, message.Length);

                var response = await _chatCompletion.GetChatMessageContentAsync(
                    session.History,
                    executionSettings,
                    _kernel);

                var answer = response.Content?.Trim() ?? "I'm processing that for you.";

                // Add assistant response to history
                session.History.AddAssistantMessage(answer);

                // Trim history to prevent token limit issues
                session.TrimHistory();

                _logger.LogDebug("Received response from OpenAI. ConversationId: {ConversationId}, AnswerLength: {Length}",
                    conversationId, answer.Length);

                return new ChatReply
                {
                    ConversationId = conversationId,
                    Answer = answer
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Failed to get response from OpenAI. Message: {Message}, InnerException: {InnerException}",
                    ex.Message, 
                    ex.InnerException?.Message ?? "none");

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

        public async Task<string> GetGreetingAsync()
        {
            try
            {
                var history = new ChatHistory();
                // Send a concise system instruction specifically for the greeting
                history.AddSystemMessage(@"You are Allison, an Insurance specialist. Follow these greeting rules EXACTLY:
1. Start with exactly: 'Hello! I'm Allison, your personal Insurance Agent.'
2. State that you're here to help explore insurance offerings, manage records, and answer questions.
3. End with: 'How can I assist you today?'
4. Never mention being an AI assistant.");
                
                history.AddUserMessage("Please introduce yourself according to your greeting instructions.");

                var executionSettings = new OpenAIPromptExecutionSettings
                {
                    Temperature = 0.5, 
                    MaxTokens = 1000 // Increased to allow for 'thinking' tokens in Kimi models
                };

                var response = await _chatCompletion.GetChatMessageContentAsync(
                    history,
                    executionSettings,
                    _kernel);

                var content = response.Content?.Trim();
                if (string.IsNullOrWhiteSpace(content))
                {
                    _logger.LogWarning("AI returned empty content for dynamic greeting.");
                    return "Allison: [Dynamic Greeting Failed to Generate]";
                }
                
                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate dynamic greeting from OpenAI.");
                return $"Allison: [Error generating dynamic greeting: {ex.Message}]";
            }
        }
    }
}
