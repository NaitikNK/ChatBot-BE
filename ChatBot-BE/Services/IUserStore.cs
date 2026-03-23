using ChatBot_BE.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChatBot_BE.Services
{
    public interface IUserStore
    {
        Task<List<User>> GetAllAsync();
        Task<User?> GetAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<User> AddAsync(User user);
        Task<bool> UpdateAsync(int id, User updated);
        Task<bool> DeleteAsync(int id);
    }
}
