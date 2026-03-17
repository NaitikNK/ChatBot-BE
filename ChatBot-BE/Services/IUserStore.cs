using ChatBot_BE.Data;

namespace ChatBot_BE.Services
{
    public interface IUserStore
    {
        Task<List<User>> GetAllAsync();
        Task<User?> GetAsync(int id);
        Task<User?> GetByPolicyNumberAsync(string policyNumber);
        Task<User?> GetByEmailAsync(string email);
        Task<User> AddAsync(User user);
        Task<bool> UpdateAsync(int id, User updated);
        Task<bool> DeleteAsync(int id);
        Task<bool> DeleteByPolicyNumberAsync(string policyNumber);
    }
}
