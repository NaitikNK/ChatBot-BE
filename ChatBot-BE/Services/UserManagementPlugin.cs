using ChatBot_BE.Model;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.RegularExpressions;

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
        [Description("Create a new user policy record with all required details")]
        public async Task<string> CreateUser(
            [Description("User's first name")] string firstName,
            [Description("User's last name")] string lastName,
            [Description("User's email address")] string email,
            [Description("User's policy number. Leave empty to auto-generate.")] string? policyNumber = null,
            [Description("Policy type: Personal, Vehicle, or Medical")] string policyType = "Personal",
            [Description("Policy name (e.g., Personal Shield Plan, Auto Insurance, Health Insurance, Car Protection Plan, Bike Insurance Plan, Group Health, etc.)")] string? policyName = null,
            [Description("User's phone number (Must be exactly 10 digits)")] string? phoneNumber = null)
        {
            if (!string.IsNullOrWhiteSpace(phoneNumber) && !IsValidMobile(phoneNumber))
            {
                _logger.LogWarning("CreateUser failed: Invalid mobile number format. PhoneNumber: {PhoneNumber}", phoneNumber);
                return $"⚠️ Mobile number '{phoneNumber}' is invalid. Please provide exactly 10 numeric digits.";
            }

            if (string.IsNullOrWhiteSpace(policyNumber))
            {
                policyNumber = "POL-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                _logger.LogInformation("Auto-generated policy number: {PolicyNumber}", policyNumber);
            }

            _logger.LogDebug("CreateUser called. PolicyNumber: {PolicyNumber}, Email: {Email}, PolicyType: {PolicyType}, ConversationId: {ConversationId}",
                policyNumber, email, policyType, _context.ConversationId);

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(policyNumber) || string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("CreateUser failed: Missing required fields. PolicyNumber: {PolicyNumber}", policyNumber);
                return "⚠️ Missing required fields. Please provide all details (first name, last name, and email).";
            }

            if (!IsValidEmail(email))
            {
                _logger.LogWarning("CreateUser failed: Invalid email format. Email: {Email}", email);
                return $"⚠️ Email '{email}' is invalid. Please enter a valid email address (e.g., name@example.com).";
            }

            // Use policy type directly as string (default to "Personal")
            var parsedPolicyType = string.IsNullOrWhiteSpace(policyType) ? "Personal" : policyType;

            // Use policy name directly as string
            var parsedPolicyName = string.IsNullOrWhiteSpace(policyName) ? null : policyName;

            var existingByPolicy = await _users.GetByPolicyNumberAsync(policyNumber);
            if (existingByPolicy != null)
            {
                _logger.LogWarning("CreateUser failed: Policy number already exists. PolicyNumber: {PolicyNumber}", policyNumber);
                return $"⚠️ Policy number '{policyNumber}' already exists. Please provide a different policy number.";
            }

            var existingByEmail = await _users.GetByEmailAsync(email);
            if (existingByEmail != null)
            {
                _logger.LogWarning("CreateUser failed: Email already in use. Email: {Email}", email);
                return $"⚠️ Email '{email}' is already in use. Please provide a different email.";
            }

            try
            {
                await _users.AddAsync(new User
                {
                    FirstName = firstName,
                    LastName = lastName,
                    PolicyNumber = policyNumber,
                    Email = email,
                    PolicyType = parsedPolicyType,
                    PolicyName = parsedPolicyName,
                    PhoneNumber = phoneNumber,
                    OwnerSessionId = _context.ConversationId
                });

                _logger.LogInformation("User created successfully. PolicyNumber: {PolicyNumber}, Email: {Email}, PolicyType: {PolicyType}", policyNumber, email, parsedPolicyType);
                var response = $"TOOL RESULT: Policy record created successfully!\n";
                response += $"- Name: {firstName} {lastName}\n";
                response += $"- Policy Number: {policyNumber}\n";
                response += $"- Policy Type: {parsedPolicyType}\n";
                if (!string.IsNullOrEmpty(parsedPolicyName))
                    response += $"- Policy Name: {parsedPolicyName}\n";
                response += $"- Email: {email}";
                if (!string.IsNullOrEmpty(phoneNumber))
                    response += $"\n- Phone: {phoneNumber}";
                return response;
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
                return $"❌ No record found for policy number '{policyNumber}'.";
            }

            // Allow access if it's the current session OR if it's a system record (empty/null ID or SEEDED_RECORD)
            if (!string.IsNullOrEmpty(user.OwnerSessionId) && 
                user.OwnerSessionId != "SEEDED_RECORD" &&
                !string.Equals(user.OwnerSessionId, _context.ConversationId, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("ViewUser access denied. PolicyNumber: {PolicyNumber}, UserSessionId: {UserSessionId}, CurrentSessionId: {ConversationId}",
                    policyNumber, user.OwnerSessionId, _context.ConversationId);
                return $"⛔ Access Denied. You are not authorized to view the record for policy number '{policyNumber}' as it was created in a different session.";
            }

            _logger.LogInformation("User record viewed successfully. PolicyNumber: {PolicyNumber}", policyNumber);
            var response = $"TOOL RESULT: Record found:\n";
            response += $"- Name: {user.FirstName} {user.LastName}\n";
            response += $"- Policy Number: {user.PolicyNumber}\n";
            response += $"- Policy Type: {user.PolicyType}\n";
            if (!string.IsNullOrEmpty(user.PolicyName))
                response += $"- Policy Name: {user.PolicyName}\n";
            response += $"- Email: {user.Email}\n";
            if (!string.IsNullOrEmpty(user.PhoneNumber))
                response += $"- Phone: {user.PhoneNumber}\n";
            if (!string.IsNullOrEmpty(user.City))
                response += $"- Location: {user.City}, {user.State}";
            return response.TrimEnd();
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

            // Filter to show records from current session OR system-seeded records (empty/null ID or SEEDED_RECORD)
            var sessionUsers = users
                .Where(u => string.IsNullOrEmpty(u.OwnerSessionId) || 
                            u.OwnerSessionId == "SEEDED_RECORD" ||
                            string.Equals(u.OwnerSessionId, _context.ConversationId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (sessionUsers.Count == 0)
            {
                _logger.LogInformation("ListAllUsers: No records for current session");
                return "📭 You haven't created any policy records yet. Would you like to create one?";
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"TOOL RESULT: Found {sessionUsers.Count} policy record(s):");
            sb.AppendLine();

            foreach (var user in sessionUsers)
            {
                sb.AppendLine($"- {user.FirstName} {user.LastName}");
                sb.AppendLine($"  - Policy: {user.PolicyNumber}");
                sb.AppendLine($"  - Email: {user.Email}");
                sb.AppendLine();
            }

            _logger.LogInformation("ListAllUsers: Returned {Count} records for session", sessionUsers.Count);
            return sb.ToString().TrimEnd();
        }

        [KernelFunction("update_user")]
        [Description("Update an existing user policy record with new details")]
        public async Task<string> UpdateUser(
            [Description("User's policy number")] string policyNumber,
            [Description("User's new first name")] string? firstName = null,
            [Description("User's new last name")] string? lastName = null,
            [Description("User's new email address")] string? email = null,
            [Description("New policy type: Personal, Vehicle, or Medical")] string? policyType = null,
            [Description("New policy name")] string? policyName = null,
            [Description("User's new phone number")] string? phoneNumber = null)
        {
            _logger.LogDebug("UpdateUser called. PolicyNumber: {PolicyNumber}, ConversationId: {ConversationId}",
                policyNumber, _context.ConversationId);

            if (string.IsNullOrWhiteSpace(policyNumber))
            {
                _logger.LogWarning("UpdateUser failed: Policy number is required");
                return "⚠️ Policy number is required to update a record.";
            }

            var user = await _users.GetByPolicyNumberAsync(policyNumber);
            if (user == null)
            {
                _logger.LogWarning("UpdateUser failed: No record found. PolicyNumber: {PolicyNumber}", policyNumber);
                return $"❌ No record found for policy number '{policyNumber}'.";
            }

            bool isUpdated = false;

            if (!string.IsNullOrWhiteSpace(firstName) && !string.Equals(user.FirstName, firstName, StringComparison.OrdinalIgnoreCase)) { user.FirstName = firstName; isUpdated = true; }
            if (!string.IsNullOrWhiteSpace(lastName) && !string.Equals(user.LastName, lastName, StringComparison.OrdinalIgnoreCase)) { user.LastName = lastName; isUpdated = true; }
            if (!string.IsNullOrWhiteSpace(email) && !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                if (!IsValidEmail(email))
                {
                    _logger.LogWarning("UpdateUser failed: Invalid email format. Email: {Email}", email);
                    return $"⚠️ Email '{email}' is invalid. Please enter a valid email address.";
                }

                var existingEmail = await _users.GetByEmailAsync(email);
                if (existingEmail != null && existingEmail.Id != user.Id)
                {
                    return $"⚠️ Email '{email}' is already in use by another record. Please provide a different email.";
                }
                user.Email = email;
                isUpdated = true;
            }
            if (!string.IsNullOrWhiteSpace(policyType) && !string.Equals(user.PolicyType, policyType, StringComparison.OrdinalIgnoreCase)) { user.PolicyType = policyType; isUpdated = true; }
            if (!string.IsNullOrWhiteSpace(policyName) && !string.Equals(user.PolicyName, policyName, StringComparison.OrdinalIgnoreCase)) { user.PolicyName = policyName; isUpdated = true; }
            if (!string.IsNullOrWhiteSpace(phoneNumber) && !string.Equals(user.PhoneNumber, phoneNumber, StringComparison.OrdinalIgnoreCase)) 
            {
                if (!IsValidMobile(phoneNumber))
                {
                    _logger.LogWarning("UpdateUser failed: Invalid mobile number format. PhoneNumber: {PhoneNumber}", phoneNumber);
                    return $"⚠️ Mobile number '{phoneNumber}' is invalid. Please provide exactly 10 numeric digits.";
                }
                user.PhoneNumber = phoneNumber; 
                isUpdated = true; 
            }

            if (!isUpdated)
            {
                return "⚠️ No valid fields were provided to update, or the provided values are identical to the existing ones.";
            }

            user.UpdatedAt = DateTime.UtcNow;

            try
            {
                var ok = await _users.UpdateAsync(user.Id, user);
                if (ok)
                {
                    _logger.LogInformation("User record updated successfully. PolicyNumber: {PolicyNumber}", policyNumber);
                    return $"TOOL RESULT: Record with policy number '{policyNumber}' has been updated successfully.";
                }
                else
                {
                    _logger.LogError("UpdateUser failed during UpdateAsync. PolicyNumber: {PolicyNumber}", policyNumber);
                    return $"❌ Failed to update record for policy number '{policyNumber}'.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user record. PolicyNumber: {PolicyNumber}", policyNumber);
                return $"⚠️ {ex.Message}";
            }
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            // Robust regex for email validation
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
        }

        private static bool IsValidMobile(string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile)) return false;
            // Clean non-numeric characters first
            var cleaned = Regex.Replace(mobile, @"[^\d]", "");
            return cleaned.Length == 10 && cleaned == mobile.Trim();
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
                return $"❌ No record found for policy number '{policyNumber}'.";
            }

            if (!string.Equals(user.OwnerSessionId, _context.ConversationId, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("DeleteUser access denied. PolicyNumber: {PolicyNumber}, UserSessionId: {UserSessionId}, CurrentSessionId: {ConversationId}",
                    policyNumber, user.OwnerSessionId, _context.ConversationId);
                return $"⛔ Access Denied. You are not authorized to delete the record for policy number '{policyNumber}' as it was created in a different session.";
            }

            var ok = await _users.DeleteByPolicyNumberAsync(policyNumber);
            if (ok)
            {
                _logger.LogInformation("User record deleted successfully. PolicyNumber: {PolicyNumber}", policyNumber);
                return $"TOOL RESULT: Record with policy number '{policyNumber}' has been deleted successfully.";
            }
            else
            {
                _logger.LogError("DeleteUser failed: Could not delete record. PolicyNumber: {PolicyNumber}", policyNumber);
                return $"❌ Failed to delete record for policy number '{policyNumber}'.";
            }
        }
    }
}
