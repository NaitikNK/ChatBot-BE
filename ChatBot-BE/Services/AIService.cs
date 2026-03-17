using ChatBot_BE.Data;
using ChatBot_BE.Models;
using System.Text.Json;

namespace ChatBot_BE.Services
{
    public class AIService : IAIService
    {
        private readonly IUserStore _users;
        private readonly IChatSessionStore _sessions;
        private readonly GeminiClient _gemini;
        private readonly ILogger<AIService> _logger;

        public AIService(IUserStore users, IChatSessionStore sessions, GeminiClient gemini, ILogger<AIService> logger)
        {
            _users = users;
            _sessions = sessions;
            _gemini = gemini;
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

            var session = _sessions.GetOrCreate(conversationId);

            // Add user message to history
            session.AddUserMessage(message);

            // Define tools for Function Calling specific to this service
            var tools = new[]
            {
                new
                {
                    function_declarations = new[]
                    {
                        new
                        {
                            name = "create_user",
                            description = "Create a new user policy record",
                            parameters = new
                            {
                                type = "OBJECT",
                                properties = new Dictionary<string, object>
                                {
                                    ["firstName"] = new { type = "STRING", description = "User's first name" },
                                    ["lastName"] = new { type = "STRING", description = "User's last name" },
                                    ["policyNumber"] = new { type = "STRING", description = "User's policy number" },
                                    ["email"] = new { type = "STRING", description = "User's email address" }
                                },
                                required = new[] { "firstName", "lastName", "policyNumber", "email" }
                            }
                        },
                        new
                        {
                            name = "view_user",
                            description = "View an existing user policy record",
                            parameters = new
                            {
                                type = "OBJECT",
                                properties = new Dictionary<string, object>
                                {
                                    ["policyNumber"] = new { type = "STRING", description = "User's policy number" }
                                },
                                required = new[] { "policyNumber" }
                            }
                        },
                        new
                        {
                            name = "delete_user",
                            description = "Delete an existing user policy record",
                            parameters = new
                            {
                                type = "OBJECT",
                                properties = new Dictionary<string, object>
                                {
                                    ["policyNumber"] = new { type = "STRING", description = "User's policy number" }
                                },
                                required = new[] { "policyNumber" }
                            }
                        }
                    }
                }
            };

            // Get LLM response
            GeminiResult result;
            try
            {
                result = await _gemini.GenerateAsync(Shared.AiPrompts.SystemPrompt, session.History, tools: tools);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get response from Gemini API");
                // Remove the user message we just added since we failed
                if (session.History.Count > 0)
                    session.History.RemoveAt(session.History.Count - 1);
                return new ChatReply
                {
                    ConversationId = conversationId,
                    Answer = "Sorry, I'm having trouble connecting right now. Please try again."
                };
            }

            // Execute corresponding action if LLM provided a function call
            var finalAnswer = result.Text;

            if (!string.IsNullOrEmpty(result.FunctionCallName))
            {
                string actionResult = string.Empty;
                switch (result.FunctionCallName)
                {
                    case "create_user":
                        actionResult = await HandleCreateUser(result.FunctionCallArgs, conversationId);
                        break;
                    case "view_user":
                        actionResult = await HandleViewUser(result.FunctionCallArgs, conversationId);
                        break;
                    case "delete_user":
                        actionResult = await HandleDeleteUser(result.FunctionCallArgs, conversationId);
                        break;
                    default:
                        _logger.LogWarning("Unknown action type from LLM: {Action}", result.FunctionCallName);
                        break;
                }

                if (!string.IsNullOrEmpty(actionResult))
                {
                    if (string.IsNullOrWhiteSpace(finalAnswer))
                    {
                        finalAnswer = actionResult;
                    }
                    else
                    {
                        finalAnswer = $"{finalAnswer}\n\n{actionResult}";
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(finalAnswer))
            {
                finalAnswer = "I'm processing that for you.";
            }

            // Add model response to history
            session.AddModelMessage(finalAnswer);

            return new ChatReply
            {
                ConversationId = conversationId,
                Answer = finalAnswer.Trim()
            };
        }

        private async Task<string> HandleCreateUser(JsonElement root, string conversationId)
        {
            var firstName = GetJsonString(root, "firstName");
            var lastName = GetJsonString(root, "lastName");
            var policyNumber = GetJsonString(root, "policyNumber");
            var email = GetJsonString(root, "email");

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(policyNumber) || string.IsNullOrWhiteSpace(email))
            {
                return "⚠️ Missing required fields. Please provide all details (first name, last name, policy number, and email).";
            }

            var existingByPolicy = await _users.GetByPolicyNumberAsync(policyNumber);
            if (existingByPolicy != null)
            {
                return $"⚠️ Policy number **{policyNumber}** already exists. Please provide a different policy number.";
            }

            var existingByEmail = await _users.GetByEmailAsync(email);
            if (existingByEmail != null)
            {
                return $"⚠️ Email **{email}** is already in use. Please provide a different email.";
            }

            try
            {
                await _users.AddAsync(new User
                {
                    FirstName = firstName,
                    LastName = lastName,
                    PolicyNumber = policyNumber,
                    Email = email,
                    OwnerSessionId = conversationId
                });

                return $"✅ Record created successfully!\n- **Name**: {firstName} {lastName}\n- **Policy**: {policyNumber}\n- **Email**: {email}";
            }
            catch (Exception ex)
            {
                return $"⚠️ {ex.Message}";
            }
        }

        private async Task<string> HandleViewUser(JsonElement root, string conversationId)
        {
            var policyNumber = GetJsonString(root, "policyNumber");
            if (string.IsNullOrWhiteSpace(policyNumber))
            {
                return "⚠️ Policy number is required to view a record.";
            }

            var user = await _users.GetByPolicyNumberAsync(policyNumber);
            if (user == null)
            {
                return $"❌ No record found for policy number **{policyNumber}**.";
            }

            if (!string.Equals(user.OwnerSessionId, conversationId, StringComparison.OrdinalIgnoreCase))
            {
                return $"⛔ Access Denied. You are not authorized to view the record for policy number **{policyNumber}** as it was created in a different session.";
            }

            return $"📋 Record found:\n- **Name**: {user.FirstName} {user.LastName}\n- **Policy**: {user.PolicyNumber}\n- **Email**: {user.Email}\n- **Created**: {user.CreatedAt:yyyy-MM-dd HH:mm} UTC";
        }

        private async Task<string> HandleDeleteUser(JsonElement root, string conversationId)
        {
            var policyNumber = GetJsonString(root, "policyNumber");
            if (string.IsNullOrWhiteSpace(policyNumber))
            {
                return "⚠️ Policy number is required to delete a record.";
            }

            var user = await _users.GetByPolicyNumberAsync(policyNumber);
            if (user == null)
            {
                return $"❌ No record found for policy number **{policyNumber}**.";
            }

            if (!string.Equals(user.OwnerSessionId, conversationId, StringComparison.OrdinalIgnoreCase))
            {
                return $"⛔ Access Denied. You are not authorized to delete the record for policy number **{policyNumber}** as it was created in a different session.";
            }

            var ok = await _users.DeleteByPolicyNumberAsync(policyNumber);
            return ok
                ? $"🗑️ Record with policy number **{policyNumber}** has been deleted."
                : $"❌ Failed to delete record for policy number **{policyNumber}**.";
        }

        private static string GetJsonString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
                ? prop.GetString()?.Trim() ?? string.Empty
                : string.Empty;
        }
    }
}
