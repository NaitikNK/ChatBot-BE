using ChatBot_BE.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChatBot_BE.Services
{
    public interface IPolicyStore
    {
        Task<List<Policy>> GetAllAsync();
        Task<List<Policy>> GetAllByUserAsync(int userId);
        Task<(List<Policy> Policies, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);
        Task<(List<Policy> Policies, int TotalCount)> GetPagedByUserAsync(int userId, int pageNumber, int pageSize);
        Task<Policy?> GetAsync(int id);
        Task<Policy?> GetByPolicyNumberAsync(string policyNumber);
        Task<Policy> AddAsync(Policy policy);
        Task<bool> UpdateAsync(int id, Policy updated);
        Task<bool> DeleteAsync(int id);
        Task<bool> DeleteByPolicyNumberAsync(string policyNumber);
    }
}
