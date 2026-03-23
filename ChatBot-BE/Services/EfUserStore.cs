using ChatBot_BE.Data;
using ChatBot_BE.Model;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace ChatBot_BE.Services
{
    public class EfUserStore : IUserStore
    {
        private readonly AppDbContext _context;

        public EfUserStore(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<User>> GetAllAsync()
        {
            return _context.Users.Include(u => u.Role).OrderByDescending(u => u.CreatedAt).ToListAsync();
        }

        public Task<User?> GetAsync(int id)
        {
            return _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
        }

        public Task<User?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return Task.FromResult<User?>(null);
            return _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email.ToLower() == email.Trim().ToLower());
        }

        public async Task<User> AddAsync(User user)
        {
            ValidateRequired(user);
            user.CreatedAt = DateTime.UtcNow;

            var existingEmail = await GetByEmailAsync(user.Email);
            if (existingEmail != null) throw new InvalidOperationException("Email already exists.");

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> UpdateAsync(int id, User updated)
        {
            ValidateRequired(updated);
            var existing = await _context.Users.FindAsync(id);
            if (existing == null) return false;

            if (!string.Equals(existing.Email, updated.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailConflict = await GetByEmailAsync(updated.Email);
                if (emailConflict != null && emailConflict.Id != id) throw new InvalidOperationException("Email already exists.");
            }

            existing.FirstName = updated.FirstName;
            existing.LastName = updated.LastName;
            existing.Email = updated.Email;
            existing.RoleId = updated.RoleId;
            existing.UpdatedAt = DateTime.UtcNow;

            if (updated.PasswordHash != null) existing.PasswordHash = updated.PasswordHash;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        private static void ValidateRequired(User user)
        {
            var context = new ValidationContext(user);
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(user, context, results, true))
            {
                var error = string.Join("; ", results.Select(r => r.ErrorMessage));
                throw new ArgumentException(error);
            }
        }
    }
}
