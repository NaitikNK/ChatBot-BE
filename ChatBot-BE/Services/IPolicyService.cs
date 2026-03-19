using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatBot_BE.Model;
using ChatBot_BE.Dto;
using ChatBot_BE.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatBot_BE.Services
{
    public interface IPolicyService
    {
        Task<List<DropdownOptionDto>> GetPolicyTypesAsync();
        Task<List<DropdownOptionDto>> GetPolicyNamesByTypeAsync(string typeId);
    }

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
                // If not a number, try to find the type by name (case-insensitive)
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
    }
}
