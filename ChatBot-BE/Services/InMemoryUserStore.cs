using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChatBot_BE.Model;
using System.ComponentModel.DataAnnotations;

namespace ChatBot_BE.Services
{
    public class InMemoryUserStore : IUserStore
    {
        private readonly object _lock = new();
        private readonly Dictionary<int, User> _users = new();
        private readonly Dictionary<string, int> _emailIndex = new(StringComparer.OrdinalIgnoreCase);
        private int _nextId = 0;

        public Task<List<User>> GetAllAsync()
        {
            lock (_lock)
            {
                return Task.FromResult(_users.Values.OrderByDescending(u => u.CreatedAt).Select(Clone).ToList());
            }
        }

        public Task<User?> GetAsync(int id)
        {
            lock (_lock)
            {
                return Task.FromResult(_users.TryGetValue(id, out var user) ? Clone(user) : null);
            }
        }

        public Task<User?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return Task.FromResult<User?>(null);
            lock (_lock)
            {
                if (_emailIndex.TryGetValue(email.Trim(), out var id))
                {
                    return Task.FromResult(_users.TryGetValue(id, out var user) ? Clone(user) : null);
                }
                return Task.FromResult<User?>(null);
            }
        }

        public Task<User> AddAsync(User user)
        {
            ValidateRequired(user);
            lock (_lock)
            {
                if (_emailIndex.ContainsKey(user.Email.Trim()))
                    throw new InvalidOperationException("Email already exists.");

                user.Id = Interlocked.Increment(ref _nextId);
                user.CreatedAt = DateTime.UtcNow;
                var stored = Clone(user);
                _users[stored.Id] = stored;
                _emailIndex[stored.Email.Trim()] = stored.Id;
                return Task.FromResult(Clone(stored));
            }
        }

        public Task<bool> UpdateAsync(int id, User updated)
        {
            ValidateRequired(updated);
            lock (_lock)
            {
                if (!_users.TryGetValue(id, out var existing)) return Task.FromResult(false);

                if (!string.Equals(existing.Email, updated.Email, StringComparison.OrdinalIgnoreCase))
                {
                    if (_emailIndex.ContainsKey(updated.Email.Trim()))
                        throw new InvalidOperationException("Email already exists.");

                    _emailIndex.Remove(existing.Email.Trim());
                    _emailIndex[updated.Email.Trim()] = id;
                }

                existing.FirstName = updated.FirstName;
                existing.LastName = updated.LastName;
                existing.Email = updated.Email;
                existing.RoleId = updated.RoleId;
                existing.UpdatedAt = DateTime.UtcNow;
                if (updated.PasswordHash != null) existing.PasswordHash = updated.PasswordHash;

                return Task.FromResult(true);
            }
        }

        public Task<bool> DeleteAsync(int id)
        {
            lock (_lock)
            {
                if (!_users.TryGetValue(id, out var existing)) return Task.FromResult(false);
                _emailIndex.Remove(existing.Email.Trim());
                _users.Remove(id);
                return Task.FromResult(true);
            }
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

        private static User Clone(User u)
        {
            return new User
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                PasswordHash = u.PasswordHash,
                RoleId = u.RoleId,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                OwnerSessionId = u.OwnerSessionId
            };
        }
    }
}
