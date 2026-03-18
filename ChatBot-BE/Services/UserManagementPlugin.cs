using ChatBot_BE.Data;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace ChatBot_BE.Services
{
    /// <summary>
    /// Semantic Kernel plugin that exposes user management functions to the LLM.
    /// The Kernel automatically invokes these methods when the model decides to call them.
    /// </summary>
    public class UserManagementPlugin
    {
        private readonly IUserStore _users;
        private readonly ILogger<UserManagementPlugin> _logger;
        private readonly IConversationContext _context;

        public UserManagementPlugin(IUserStore users, ILogger<UserManagementPlugin> logger, IConversationContext context)
        {
            _users = users;
            _logger = logger;
            _context = context;
        }

        [KernelFunction("create_user")]
        [Description("Create a new user policy record")]
        public async Task<string> CreateUser(
            [Description("User's first name")] string firstName,
            [Description("User's last name")] string lastName,
            [Description("User's policy number")] string policyNumber,
            [Description("User's email address")] string email)
        {
            _logger.LogDebug("CreateUser called. PolicyNumber: {PolicyNumber}, Email: {Email}, ConversationId: {ConversationId}",
                policyNumber, email, _context.ConversationId);

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(policyNumber) || string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("CreateUser failed: Missing required fields. PolicyNumber: {PolicyNumber}", policyNumber);
                return "⚠️ Missing required fields. Please provide all details (first name, last name, policy number, and email).";
            }

            var existingByPolicy = await _users.GetByPolicyNumberAsync(policyNumber);
            if (existingByPolicy != null)
            {
                _logger.LogWarning("CreateUser failed: Policy number already exists. PolicyNumber: {PolicyNumber}", policyNumber);
                return $"⚠️ Policy number **{policyNumber}** already exists. Please provide a different policy number.";
            }

            var existingByEmail = await _users.GetByEmailAsync(email);
            if (existingByEmail != null)
            {
                _logger.LogWarning("CreateUser failed: Email already in use. Email: {Email}", email);
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
                    OwnerSessionId = _context.ConversationId
                });

                _logger.LogInformation("User created successfully. PolicyNumber: {PolicyNumber}, Email: {Email}", policyNumber, email);
                return $"✅ Record created successfully!\n- **Name**: {firstName} {lastName}\n- **Policy**: {policyNumber}\n- **Email**: {email}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create user record. PolicyNumber: {PolicyNumber}, Email: {Email}", policyNumber, email);
                return $"⚠️ {ex.Message}";
            }
        }

        [KernelFunction("view_user")]
        [Description("View an existing user policy record")]
        public async Task<string> ViewUser(
            [Description("User's policy number")] string policyNumber)
        {
            _logger.LogDebug("ViewUser called. PolicyNumber: {PolicyNumber}, ConversationId: {ConversationId}",
                policyNumber, _context.ConversationId);

            if (string.IsNullOrWhiteSpace(policyNumber))
            {
                _logger.LogWarning("ViewUser failed: Policy number is required");
                return "⚠️ Policy number is required to view a record.";
            }

            var user = await _users.GetByPolicyNumberAsync(policyNumber);
            if (user == null)
            {
                _logger.LogWarning("ViewUser failed: No record found. PolicyNumber: {PolicyNumber}", policyNumber);
                return $"❌ No record found for policy number **{policyNumber}**.";
            }

            if (!string.Equals(user.OwnerSessionId, _context.ConversationId, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("ViewUser access denied. PolicyNumber: {PolicyNumber}, UserSessionId: {UserSessionId}, CurrentSessionId: {ConversationId}",
                    policyNumber, user.OwnerSessionId, _context.ConversationId);
                return $"⛔ Access Denied. You are not authorized to view the record for policy number **{policyNumber}** as it was created in a different session.";
            }

            _logger.LogInformation("User record viewed successfully. PolicyNumber: {PolicyNumber}", policyNumber);
            return $"📋 Record found:\n- **Name**: {user.FirstName} {user.LastName}\n- **Policy**: {user.PolicyNumber}\n- **Email**: {user.Email}\n- **Created**: {user.CreatedAt:yyyy-MM-dd HH:mm} UTC";
        }

        [KernelFunction("list_all_users")]
        [Description("List all user policy records in the system")]
        public async Task<string> ListAllUsers()
        {
            _logger.LogDebug("ListAllUsers called. ConversationId: {ConversationId}", _context.ConversationId);

            var users = await _users.GetAllAsync();

            if (users.Count == 0)
            {
                _logger.LogInformation("ListAllUsers: No records found");
                return "📭 No policy records found in the system.";
            }

            // Filter to show only records from current session (for security)
            var sessionUsers = users
                .Where(u => string.Equals(u.OwnerSessionId, _context.ConversationId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (sessionUsers.Count == 0)
            {
                _logger.LogInformation("ListAllUsers: No records for current session");
                return "📭 You haven't created any policy records yet. Would you like to create one?";
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"📋 Found **{sessionUsers.Count}** policy record(s):");
            sb.AppendLine();

            foreach (var user in sessionUsers)
            {
                sb.AppendLine($"• **{user.FirstName} {user.LastName}**");
                sb.AppendLine($"  - Policy: {user.PolicyNumber}");
                sb.AppendLine($"  - Email: {user.Email}");
                sb.AppendLine($"  - Created: {user.CreatedAt:yyyy-MM-dd HH:mm} UTC");
                sb.AppendLine();
            }

            _logger.LogInformation("ListAllUsers: Returned {Count} records for session", sessionUsers.Count);
            return sb.ToString().TrimEnd();
        }

        [KernelFunction("delete_user")]
        [Description("Delete an existing user policy record")]
        public async Task<string> DeleteUser(
            [Description("User's policy number")] string policyNumber)
        {
            _logger.LogDebug("DeleteUser called. PolicyNumber: {PolicyNumber}, ConversationId: {ConversationId}",
                policyNumber, _context.ConversationId);

            if (string.IsNullOrWhiteSpace(policyNumber))
            {
                _logger.LogWarning("DeleteUser failed: Policy number is required");
                return "⚠️ Policy number is required to delete a record.";
            }

            var user = await _users.GetByPolicyNumberAsync(policyNumber);
            if (user == null)
            {
                _logger.LogWarning("DeleteUser failed: No record found. PolicyNumber: {PolicyNumber}", policyNumber);
                return $"❌ No record found for policy number **{policyNumber}**.";
            }

            if (!string.Equals(user.OwnerSessionId, _context.ConversationId, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("DeleteUser access denied. PolicyNumber: {PolicyNumber}, UserSessionId: {UserSessionId}, CurrentSessionId: {ConversationId}",
                    policyNumber, user.OwnerSessionId, _context.ConversationId);
                return $"⛔ Access Denied. You are not authorized to delete the record for policy number **{policyNumber}** as it was created in a different session.";
            }

            var ok = await _users.DeleteByPolicyNumberAsync(policyNumber);
            if (ok)
            {
                _logger.LogInformation("User record deleted successfully. PolicyNumber: {PolicyNumber}", policyNumber);
                return $"🗑️ Record with policy number **{policyNumber}** has been deleted.";
            }
            else
            {
                _logger.LogError("DeleteUser failed: Could not delete record. PolicyNumber: {PolicyNumber}", policyNumber);
                return $"❌ Failed to delete record for policy number **{policyNumber}**.";
            }
        }
    }
}
