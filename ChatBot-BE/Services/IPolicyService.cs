using System.Collections.Generic;
using System.Threading.Tasks;
using ChatBot_BE.Dto;

namespace ChatBot_BE.Services
{
    /// <summary>
    /// Service for managing policy types and names.
    /// </summary>
    public interface IPolicyService
    {
        /// <summary>
        /// Gets all available policy types.
        /// </summary>
        Task<List<DropdownOptionDto>> GetPolicyTypesAsync();

        /// <summary>
        /// Gets all policy names associated with a specific type.
        /// </summary>
        /// <param name="typeId">The ID or name of the policy type.</param>
        Task<List<DropdownOptionDto>> GetPolicyNamesByTypeAsync(string typeId);

        /// <summary>
        /// Resolves a policy type from a string (ID or Name).
        /// </summary>
        Task<(int? Id, string Name)> ResolvePolicyTypeAsync(string? typeInput);

        /// <summary>
        /// Resolves a policy name from a string (ID or Name).
        /// </summary>
        Task<(int? Id, string Name)> ResolvePolicyNameAsync(string? nameInput);
    }
}
