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
        private const string SeededRecordId = "SEEDED_RECORD";

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
            [Description("User's phone number (10 digits)")] string? phoneNumber = null,
            [Description("User's date of birth (ISO format: YYYY-MM-DD)")] string? dateOfBirth = null,
            [Description("User's zip/postal code (5-6 digits)")] string? postalCode = null)
        {
            if (!_context.IsAuthenticated)
            {
                return "[ERROR: AUTHENTICATION_REQUIRED]";
            }

            if (string.IsNullOrWhiteSpace(policyNumber))
            {
                policyNumber = "POL-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            }

            var validationError = ValidateUserData(email, phoneNumber, dateOfBirth, postalCode, out var dob, isUpdate: false);
            if (validationError != null) return validationError;

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
                    DateOfBirth = dob,
                    PostalCode = postalCode,
                    OwnerSessionId = _context.ConversationId,
                    UserId = _context.UserId // Assign the current user's ID
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

            // Admin can view any policy; regular users can only view their own
            var isAdmin = _context.Role == "Admin";
            if (!isAdmin && policy.UserId != _context.UserId)
            {
                return $"⛔ Access Denied. You are not authorized to view this record.";
            }

            var response = $"TOOL RESULT: Record found:\n";
            response += $"- Name: {policy.FirstName} {policy.LastName}\n";
            response += $"- Policy Number: {policy.PolicyNumber}\n";
            response += $"- Policy Type: {policy.PolicyType}\n";
            response += $"- Email: {policy.Email}";

            if (policy.OwnerSessionId == SeededRecordId)
            {
                response += "\n- STATUS: [System Default - Read Only]";
            }

            return response;
        }

        [KernelFunction("list_all_users")]
        [Description("List all user policy records accessible to the current user")]
        public async Task<string> ListAllUsers()
        {
            if (!_context.IsAuthenticated)
            {
                return "[ERROR: AUTHENTICATION_REQUIRED]";
            }

            var isAdmin = _context.Role == "Admin";
            List<Policy> policies;

            if (isAdmin)
            {
                // Admin can see ALL policies
                policies = await _policies.GetAllAsync();
            }
            else
            {
                // Regular users only see their own policies
                policies = _context.UserId.HasValue
                    ? await _policies.GetAllByUserAsync(_context.UserId.Value)
                    : new List<Policy>();
            }

            if (policies.Count == 0)
            {
                return "📭 No policy records found.";
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"TOOL RESULT: Found {policies.Count} policy record(s):");
            foreach (var p in policies)
            {
                var label = p.OwnerSessionId == SeededRecordId ? " (System Default - Read Only)" : "";
                sb.AppendLine($"- {p.FirstName} {p.LastName} (Policy: {p.PolicyNumber}){label}");
            }

            return sb.ToString().TrimEnd();
        }

        [KernelFunction("update_user")]
        [Description("Update an existing user policy record")]
        public async Task<string> UpdateUser(
            [Description("User's policy number")] string policyNumber,
            [Description("User's new first name")] string? firstName = null,
            [Description("User's new last name")] string? lastName = null,
            [Description("User's new email address")] string? email = null,
            [Description("User's new phone number (10 digits)")] string? phoneNumber = null,
            [Description("User's new date of birth (ISO format: YYYY-MM-DD)")] string? dateOfBirth = null,
            [Description("User's new zip/postal code (5-6 digits)")] string? postalCode = null)
        {
            if (!_context.IsAuthenticated)
            {
                return "[ERROR: AUTHENTICATION_REQUIRED]";
            }

            var policy = await _policies.GetByPolicyNumberAsync(policyNumber);
            if (policy == null) return $"❌ No record found.";

            // Admin can update any policy; regular users can only update their own
            var isAdmin = _context.Role == "Admin";
            if (!isAdmin && policy.UserId != _context.UserId)
            {
                return "⛔ Access Denied. You are not authorized to update this record.";
            }

            if (firstName != null) policy.FirstName = firstName;
            if (lastName != null) policy.LastName = lastName;

            var validationError = ValidateUserData(email, phoneNumber, dateOfBirth, postalCode, out var dob, isUpdate: true);
            if (validationError != null) return validationError;

            if (email != null) policy.Email = email;
            if (phoneNumber != null) policy.PhoneNumber = phoneNumber;
            if (postalCode != null) policy.PostalCode = postalCode;
            if (dob != null) policy.DateOfBirth = dob;

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

            // Admin can delete any policy; regular users can only delete their own
            var isAdmin = _context.Role == "Admin";
            if (!isAdmin && policy.UserId != _context.UserId)
            {
                return "⛔ Access Denied. You are not authorized to delete this record.";
            }

            // [NEW] Block deletion of system-default records
            if (policy.OwnerSessionId == SeededRecordId)
            {
                return "❌ Error: System-default records cannot be deleted.";
            }

            var ok = await _policies.DeleteAsync(policy.Id);
            return ok ? $"TOOL RESULT: Record deleted successfully." : "❌ Failed to delete record.";
        }

        private string? ValidateUserData(string? email, string? phoneNumber, string? dateOfBirth, string? postalCode, out DateTime? dob, bool isUpdate)
        {
            dob = null;

            // Date of Birth Validation
            if (!string.IsNullOrWhiteSpace(dateOfBirth))
            {
                if (DateTime.TryParse(dateOfBirth, out var result))
                {
                    if (result > DateTime.Today) return "❌ Error: Date of birth cannot be in the future.";
                    if (result > DateTime.Today.AddYears(-1)) return "❌ Error: User must be at least 1 year old.";
                    dob = result;
                }
                else
                {
                    return "❌ Error: Invalid date format for Date of Birth. Please use YYYY-MM-DD.";
                }
            }
            else if (!isUpdate)
            {
                return "❌ Error: Date of birth is required.";
            }

            // Email Validation
            if (email != null)
            {
                if (string.IsNullOrWhiteSpace(email) || !email.EndsWith(".com", StringComparison.OrdinalIgnoreCase))
                {
                    return "❌ Error: Email must be a valid .com address.";
                }
            }
            else if (!isUpdate)
            {
                return "❌ Error: Email is required.";
            }

            // Phone Validation
            if (phoneNumber != null)
            {
                if (string.IsNullOrWhiteSpace(phoneNumber) || !Regex.IsMatch(phoneNumber, @"^\d{10}$"))
                {
                    return "❌ Error: Phone number must be exactly 10 digits.";
                }
            }
            else if (!isUpdate)
            {
                return "❌ Error: Phone number is required.";
            }

            // Zip Code Validation
            if (postalCode != null)
            {
                if (string.IsNullOrWhiteSpace(postalCode) || !Regex.IsMatch(postalCode, @"^\d{5,6}$"))
                {
                    return "❌ Error: Zip code must be 5 or 6 digits.";
                }
            }
            else if (!isUpdate)
            {
                return "❌ Error: Zip code is required.";
            }

            return null;
        }
    }
}
