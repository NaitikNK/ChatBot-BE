using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatBot_BE.Model;
using ChatBot_BE.Dto;
using ChatBot_BE.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatBot_BE.Services
{
    /// <summary>
    /// Implementation of IPolicyService using Entity Framework.
    /// </summary>
    public class PolicyService : IPolicyService
    {
        private readonly AppDbContext _context;

        public PolicyService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<DropdownOptionDto>> GetPolicyTypesAsync()
        {
            var types = await _context.PolicyTypes.ToListAsync();
            return types.Select(t => new DropdownOptionDto
            {
                Id = t.Id,
                Name = t.Name,
                Label = t.Name,
                Value = t.Id.ToString()
            }).ToList();
        }

        public async Task<List<DropdownOptionDto>> GetPolicyNamesByTypeAsync(string typeId)
        {
            int numericTypeId;
            if (!int.TryParse(typeId, out numericTypeId))
            {
                var type = await _context.PolicyTypes
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == typeId.ToLower());
                
                if (type == null)
                {
                    return new List<DropdownOptionDto>();
                }
                numericTypeId = type.Id;
            }

            var names = await _context.PolicyNames
                .Where(n => n.PolicyTypeId == numericTypeId)
                .ToListAsync();

            return names.Select(n => new DropdownOptionDto
            {
                Id = n.Id,
                Name = n.Name,
                Label = n.Name,
                Value = n.Id.ToString()
            }).ToList();
        }

        public async Task<(int? Id, string Name)> ResolvePolicyTypeAsync(string? typeInput)
        {
            if (string.IsNullOrEmpty(typeInput)) return (null, typeInput ?? string.Empty);

            if (int.TryParse(typeInput, out int id))
            {
                var typeMaster = await _context.PolicyTypes.FindAsync(id);
                if (typeMaster != null) return (typeMaster.Id, typeMaster.Name);
            }
            else
            {
                var typeMaster = await _context.PolicyTypes.FirstOrDefaultAsync(t => t.Name == typeInput)
                                ?? (await _context.PolicyTypes.ToListAsync()).FirstOrDefault(t => 
                                    t.Name.Replace(" ", "").Equals(typeInput.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));

                if (typeMaster != null) return (typeMaster.Id, typeMaster.Name);
            }

            return (null, typeInput);
        }

        public async Task<(int? Id, string Name)> ResolvePolicyNameAsync(string? nameInput)
        {
            if (string.IsNullOrEmpty(nameInput)) return (null, nameInput ?? string.Empty);

            if (int.TryParse(nameInput, out int id))
            {
                var nameMaster = await _context.PolicyNames.FindAsync(id);
                if (nameMaster != null) return (nameMaster.Id, nameMaster.Name);
            }
            else
            {
                var nameMaster = await _context.PolicyNames.FirstOrDefaultAsync(n => n.Name == nameInput)
                                ?? (await _context.PolicyNames.ToListAsync()).FirstOrDefault(n => 
                                    n.Name.Replace(" ", "").Equals(nameInput.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));

                if (nameMaster != null) return (nameMaster.Id, nameMaster.Name);
            }

            return (null, nameInput);
        }
    }
}
