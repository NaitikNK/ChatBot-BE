using System.Threading;
using ChatBot_BE.Data;
using System.Text.RegularExpressions;

namespace ChatBot_BE.Services
{
    public class InMemoryUserStore : IUserStore
    {
        private readonly object _lock = new();
        private readonly Dictionary<int, User> _users = new();
        private readonly Dictionary<string, int> _policyIndex = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _emailIndex = new(StringComparer.OrdinalIgnoreCase);
        private int _nextId = 0;

        public Task<List<User>> GetAllAsync()
        {
            lock (_lock)
            {
                var users = _users.Values
                    .OrderByDescending(u => u.CreatedAt)
                    .ToList();
                return Task.FromResult(users);
            }
        }

        public Task<User?> GetAsync(int id)
        {
            lock (_lock)
            {
                User? user = _users.TryGetValue(id, out var found) ? Clone(found) : null;
                return Task.FromResult(user);
            }
        }

        public Task<User?> GetByPolicyNumberAsync(string policyNumber)
        {
            if (string.IsNullOrWhiteSpace(policyNumber)) return Task.FromResult<User?>(null);

            lock (_lock)
            {
                if (!_policyIndex.TryGetValue(policyNumber.Trim(), out var id)) return Task.FromResult<User?>(null);
                return Task.FromResult(_users.TryGetValue(id, out var found) ? Clone(found) : null);
            }
        }

        public Task<User?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return Task.FromResult<User?>(null);

            lock (_lock)
            {
                if (!_emailIndex.TryGetValue(email.Trim(), out var id)) return Task.FromResult<User?>(null);
                return Task.FromResult(_users.TryGetValue(id, out var found) ? Clone(found) : null);
            }
        }

        public Task<User> AddAsync(User user)
        {
            ValidateRequired(user);
            var policy = (user.PolicyNumber ?? string.Empty).Trim();
            var email = (user.Email ?? string.Empty).Trim();

            var created = new User
            {
                Id = Interlocked.Increment(ref _nextId),
                FirstName = user.FirstName,
                LastName = user.LastName,
                PolicyNumber = policy,
                Email = email,
                CreatedAt = DateTime.UtcNow
            };

            lock (_lock)
            {
                if (_policyIndex.ContainsKey(created.PolicyNumber))
                {
                    throw new InvalidOperationException("PolicyNumber already exists.");
                }

                if (_emailIndex.ContainsKey(created.Email))
                {
                    throw new InvalidOperationException("Email already exists.");
                }

                _users[created.Id] = created;
                _policyIndex[created.PolicyNumber] = created.Id;
                _emailIndex[created.Email] = created.Id;
            }

            return Task.FromResult(Clone(created));
        }

        public Task<bool> UpdateAsync(int id, User updated)
        {
            ValidateRequired(updated);
            var newPolicy = (updated.PolicyNumber ?? string.Empty).Trim();
            var newEmail = (updated.Email ?? string.Empty).Trim();

            lock (_lock)
            {
                if (!_users.TryGetValue(id, out var existing)) return Task.FromResult(false);

                if (!string.Equals(existing.PolicyNumber, newPolicy, StringComparison.OrdinalIgnoreCase))
                {
                    if (_policyIndex.TryGetValue(newPolicy, out var otherId) && otherId != id)
                    {
                        throw new InvalidOperationException("PolicyNumber already exists.");
                    }

                    _policyIndex.Remove(existing.PolicyNumber);
                    _policyIndex[newPolicy] = id;
                    existing.PolicyNumber = newPolicy;
                }

                if (!string.Equals(existing.Email, newEmail, StringComparison.OrdinalIgnoreCase))
                {
                    if (_emailIndex.TryGetValue(newEmail, out var otherId) && otherId != id)
                    {
                        throw new InvalidOperationException("Email already exists.");
                    }

                    _emailIndex.Remove(existing.Email);
                    _emailIndex[newEmail] = id;
                    existing.Email = newEmail;
                }

                existing.FirstName = updated.FirstName;
                existing.LastName = updated.LastName;
                return Task.FromResult(true);
            }
        }

        public Task<bool> DeleteAsync(int id)
        {
            lock (_lock)
            {
                if (!_users.TryGetValue(id, out var existing)) return Task.FromResult(false);
                _users.Remove(id);
                _policyIndex.Remove(existing.PolicyNumber);
                _emailIndex.Remove(existing.Email);
                return Task.FromResult(true);
            }
        }

        public Task<bool> DeleteByPolicyNumberAsync(string policyNumber)
        {
            if (string.IsNullOrWhiteSpace(policyNumber)) return Task.FromResult(false);

            lock (_lock)
            {
                if (!_policyIndex.TryGetValue(policyNumber.Trim(), out var id)) return Task.FromResult(false);
                if (!_users.TryGetValue(id, out var existing)) return Task.FromResult(false);
                _users.Remove(id);
                _policyIndex.Remove(existing.PolicyNumber);
                _emailIndex.Remove(existing.Email);
                return Task.FromResult(true);
            }
        }

        private static void ValidateRequired(User user)
        {
            var context = new System.ComponentModel.DataAnnotations.ValidationContext(user);
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(user, context, results, true))
            {
                var error = string.Join("; ", results.Select(r => r.ErrorMessage));
                throw new ArgumentException(error);
            }
        }

        private static User Clone(User user)
        {
            return new User
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PolicyNumber = user.PolicyNumber,
                Email = user.Email,
                OwnerSessionId = user.OwnerSessionId,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
