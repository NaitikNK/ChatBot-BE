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

        // Set per-request by AIService before invoking the kernel
        internal string ConversationId { get; set; } = string.Empty;

        public UserManagementPlugin(IUserStore users, ILogger<UserManagementPlugin> logger)
        {
            _users = users;
            _logger = logger;
        }

        [KernelFunction("create_user")]
        [Description("Create a new user policy record")]
        public async Task<string> CreateUser(
            [Description("User's first name")] string firstName,
            [Description("User's last name")] string lastName,
            [Description("User's policy number")] string policyNumber,
            [Description("User's email address")] string email)
        {
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
                    OwnerSessionId = ConversationId
                });

                return $"✅ Record created successfully!\n- **Name**: {firstName} {lastName}\n- **Policy**: {policyNumber}\n- **Email**: {email}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create user record");
                return $"⚠️ {ex.Message}";
            }
        }

        [KernelFunction("view_user")]
        [Description("View an existing user policy record")]
        public async Task<string> ViewUser(
            [Description("User's policy number")] string policyNumber)
        {
            Console.WriteLine($"[DEBUG] ViewUser Executing on Plugin Hash: {this.GetHashCode()} with ConversationId: '{ConversationId}'");
            if (string.IsNullOrWhiteSpace(policyNumber))
            {
                return "⚠️ Policy number is required to view a record.";
            }

            var user = await _users.GetByPolicyNumberAsync(policyNumber);
            if (user == null)
            {
                return $"❌ No record found for policy number **{policyNumber}**.";
            }

            if (!string.Equals(user.OwnerSessionId, ConversationId, StringComparison.OrdinalIgnoreCase))
            {
                return $"⛔ Access Denied. You are not authorized to view the record for policy number **{policyNumber}** as it was created in a different session.";
            }

            return $"📋 Record found:\n- **Name**: {user.FirstName} {user.LastName}\n- **Policy**: {user.PolicyNumber}\n- **Email**: {user.Email}\n- **Created**: {user.CreatedAt:yyyy-MM-dd HH:mm} UTC";
        }

        [KernelFunction("delete_user")]
        [Description("Delete an existing user policy record")]
        public async Task<string> DeleteUser(
            [Description("User's policy number")] string policyNumber)
        {
            if (string.IsNullOrWhiteSpace(policyNumber))
            {
                return "⚠️ Policy number is required to delete a record.";
            }

            var user = await _users.GetByPolicyNumberAsync(policyNumber);
            if (user == null)
            {
                return $"❌ No record found for policy number **{policyNumber}**.";
            }

            if (!string.Equals(user.OwnerSessionId, ConversationId, StringComparison.OrdinalIgnoreCase))
            {
                return $"⛔ Access Denied. You are not authorized to delete the record for policy number **{policyNumber}** as it was created in a different session.";
            }

            var ok = await _users.DeleteByPolicyNumberAsync(policyNumber);
            return ok
                ? $"🗑️ Record with policy number **{policyNumber}** has been deleted."
                : $"❌ Failed to delete record for policy number **{policyNumber}**.";
        }
    }
}
