using ChatBot_BE.Model;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace ChatBot_BE.Services
{
    public class UserManagementPlugin
    {
        private readonly IPolicyStore _policies;
        private readonly ILogger<UserManagementPlugin> _logger;
        private readonly IConversationContext _context;

        public UserManagementPlugin(IPolicyStore policies, ILogger<UserManagementPlugin> logger, IConversationContext context)
        {
            _policies = policies;
            _logger = logger;
            _context = context;
        }

        [KernelFunction("create_user")]
        [Description("Create a new user policy record with all required details")]
        public async Task<string> CreateUser(
            [Description("User's first name")] string firstName,
            [Description("User's last name")] string lastName,
            [Description("User's email address")] string email,
            [Description("User's policy number. Leave empty to auto-generate.")] string? policyNumber = null,
            [Description("Policy type: Personal, Vehicle, or Medical")] string policyType = "Personal",
            [Description("Policy name (e.g., Personal Shield Plan, Auto Insurance, Car Protection Plan, etc.)")] string? policyName = null,
            [Description("User's phone number")] string? phoneNumber = null)
        {
            if (!_context.IsAuthenticated)
            {
                return "[ERROR: AUTHENTICATION_REQUIRED]";
            }

            if (string.IsNullOrWhiteSpace(policyNumber))
            {
                policyNumber = "POL-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            }

            try
            {
                await _policies.AddAsync(new Policy
                {
                    FirstName = firstName,
                    LastName = lastName,
                    PolicyNumber = policyNumber,
                    Email = email,
                    PolicyType = policyType,
                    PolicyName = policyName,
                    PhoneNumber = phoneNumber,
                    OwnerSessionId = _context.ConversationId
                });

                return $"TOOL RESULT: Policy record created successfully! Policy Number: {policyNumber}";
            }
            catch (Exception ex)
            {
                return $"⚠️ {ex.Message}";
            }
        }

        [KernelFunction("view_user")]
        [Description("View an existing user policy record")]
        public async Task<string> ViewUser(
            [Description("User's policy number")] string policyNumber)
        {
            if (!_context.IsAuthenticated)
            {
                return "[ERROR: AUTHENTICATION_REQUIRED]";
            }

            var policy = await _policies.GetByPolicyNumberAsync(policyNumber);
            if (policy == null)
            {
                return $"❌ No record found for policy number '{policyNumber}'.";
            }

            if (!string.IsNullOrEmpty(policy.OwnerSessionId) && 
                policy.OwnerSessionId != "SEEDED_RECORD" &&
                !string.Equals(policy.OwnerSessionId, _context.ConversationId, StringComparison.OrdinalIgnoreCase))
            {
                return $"⛔ Access Denied. You are not authorized to view this record.";
            }

            var response = $"TOOL RESULT: Record found:\n";
            response += $"- Name: {policy.FirstName} {policy.LastName}\n";
            response += $"- Policy Number: {policy.PolicyNumber}\n";
            response += $"- Policy Type: {policy.PolicyType}\n";
            response += $"- Email: {policy.Email}";
            return response;
        }

        [KernelFunction("list_all_users")]
        [Description("List all user policy records in the system")]
        public async Task<string> ListAllUsers()
        {
            if (!_context.IsAuthenticated)
            {
                return "[ERROR: AUTHENTICATION_REQUIRED]";
            }

            var policies = await _policies.GetAllAsync();
            var sessionPolicies = policies
                .Where(p => string.IsNullOrEmpty(p.OwnerSessionId) || 
                            p.OwnerSessionId == "SEEDED_RECORD" ||
                            string.Equals(p.OwnerSessionId, _context.ConversationId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (sessionPolicies.Count == 0)
            {
                return "📭 No policy records found for your session.";
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"TOOL RESULT: Found {sessionPolicies.Count} policy record(s):");
            foreach (var p in sessionPolicies)
            {
                sb.AppendLine($"- {p.FirstName} {p.LastName} (Policy: {p.PolicyNumber})");
            }

            return sb.ToString().TrimEnd();
        }

        [KernelFunction("update_user")]
        [Description("Update an existing user policy record")]
        public async Task<string> UpdateUser(
            [Description("User's policy number")] string policyNumber,
            [Description("User's new first name")] string? firstName = null,
            [Description("User's new last name")] string? lastName = null,
            [Description("User's new email address")] string? email = null)
        {
            if (!_context.IsAuthenticated)
            {
                return "[ERROR: AUTHENTICATION_REQUIRED]";
            }

            var policy = await _policies.GetByPolicyNumberAsync(policyNumber);
            if (policy == null) return $"❌ No record found.";

            if (firstName != null) policy.FirstName = firstName;
            if (lastName != null) policy.LastName = lastName;
            if (email != null) policy.Email = email;

            try
            {
                await _policies.UpdateAsync(policy.Id, policy);
                return $"TOOL RESULT: Record updated successfully.";
            }
            catch (Exception ex)
            {
                return $"⚠️ {ex.Message}";
            }
        }

        [KernelFunction("delete_user")]
        [Description("Delete an existing user policy record")]
        public async Task<string> DeleteUser(
            [Description("User's policy number")] string policyNumber)
        {
            if (!_context.IsAuthenticated)
            {
                return "[ERROR: AUTHENTICATION_REQUIRED]";
            }

            var policy = await _policies.GetByPolicyNumberAsync(policyNumber);
            if (policy == null) return $"❌ No record found.";

            var ok = await _policies.DeleteAsync(policy.Id);
            return ok ? $"TOOL RESULT: Record deleted successfully." : "❌ Failed to delete record.";
        }
    }
}
