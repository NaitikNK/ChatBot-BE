using ChatBot_BE.Data;
using ChatBot_BE.Model;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace ChatBot_BE.Services
{
    public class EfPolicyStore : IPolicyStore
    {
        private readonly AppDbContext _context;

        public EfPolicyStore(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<Policy>> GetAllAsync()
        {
            return _context.Policies.OrderByDescending(p => p.CreatedAt).ToListAsync();
        }

        public async Task<(List<Policy> Policies, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize)
        {
            var totalCount = await _context.Policies.CountAsync();
            var policies = await _context.Policies
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (policies, totalCount);
        }

        public Task<Policy?> GetAsync(int id)
        {
            return _context.Policies.FirstOrDefaultAsync(p => p.Id == id);
        }

        public Task<Policy?> GetByPolicyNumberAsync(string policyNumber)
        {
            if (string.IsNullOrWhiteSpace(policyNumber)) return Task.FromResult<Policy?>(null);
            return _context.Policies.FirstOrDefaultAsync(p => p.PolicyNumber.ToLower() == policyNumber.Trim().ToLower());
        }

        public async Task<Policy> AddAsync(Policy policy)
        {
            ValidateRequired(policy);
            policy.CreatedAt = DateTime.UtcNow;

            var existingPolicy = await GetByPolicyNumberAsync(policy.PolicyNumber);
            if (existingPolicy != null) throw new InvalidOperationException("PolicyNumber already exists.");

            _context.Policies.Add(policy);
            await _context.SaveChangesAsync();
            return policy;
        }

        public async Task<bool> UpdateAsync(int id, Policy updated)
        {
            ValidateRequired(updated);
            var existing = await _context.Policies.FindAsync(id);
            if (existing == null) return false;

            if (!string.Equals(existing.PolicyNumber, updated.PolicyNumber, StringComparison.OrdinalIgnoreCase))
            {
                var policyConflict = await GetByPolicyNumberAsync(updated.PolicyNumber);
                if (policyConflict != null && policyConflict.Id != id) throw new InvalidOperationException("PolicyNumber already exists.");
            }

            existing.FirstName = updated.FirstName;
            existing.LastName = updated.LastName;
            existing.PolicyNumber = updated.PolicyNumber;
            existing.Email = updated.Email;
            existing.PolicyType = updated.PolicyType;
            existing.PolicyName = updated.PolicyName;
            existing.PhoneNumber = updated.PhoneNumber;
            existing.Address = updated.Address;
            existing.City = updated.City;
            existing.State = updated.State;
            existing.PostalCode = updated.PostalCode;
            existing.Country = updated.Country;
            existing.DateOfBirth = updated.DateOfBirth;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UserId = updated.UserId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var policy = await _context.Policies.FindAsync(id);
            if (policy == null) return false;

            _context.Policies.Remove(policy);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteByPolicyNumberAsync(string policyNumber)
        {
            var policy = await GetByPolicyNumberAsync(policyNumber);
            if (policy == null) return false;

            _context.Policies.Remove(policy);
            await _context.SaveChangesAsync();
            return true;
        }

        private static void ValidateRequired(Policy policy)
        {
            var context = new ValidationContext(policy);
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(policy, context, results, true))
            {
                var error = string.Join("; ", results.Select(r => r.ErrorMessage));
                throw new ArgumentException(error);
            }
        }
    }
}
