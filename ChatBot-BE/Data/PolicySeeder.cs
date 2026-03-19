using ChatBot_BE.Model;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ChatBot_BE.Data
{
    public static class PolicySeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            var exists = await context.PolicyTypes.AnyAsync();
            if (exists)
            {
                var currentCount = await context.PolicyTypes.CountAsync();
                Log.Information("Policy background seed skipped: Already contains {count} types.", currentCount);
                return;
            }

            Log.Information("Policy background seed starting...");

            var types = new List<PolicyTypeMaster>
            {
                new() { Id = 1, Name = "Personal" },
                new() { Id = 2, Name = "Vehicle" },
                new() { Id = 3, Name = "Medical" }
            };

            var names = new List<PolicyNameMaster>
            {
                // Personal (PolicyTypeId = 1)
                new() { Id = 1, PolicyTypeId = 1, Name = "General Insurance" },
                new() { Id = 2, PolicyTypeId = 1, Name = "Personal Shield Plan" },
                new() { Id = 3, PolicyTypeId = 1, Name = "Family Protection Plan" },
                
                // Vehicle (PolicyTypeId = 2)
                new() { Id = 11, PolicyTypeId = 2, Name = "Auto Insurance" },
                new() { Id = 12, PolicyTypeId = 2, Name = "Commercial Auto" },
                new() { Id = 13, PolicyTypeId = 2, Name = "Motorcycle Insurance" },
                new() { Id = 14, PolicyTypeId = 2, Name = "EV Insurance" },
                new() { Id = 15, PolicyTypeId = 2, Name = "Car Protection Plan" },
                new() { Id = 16, PolicyTypeId = 2, Name = "Bike Insurance Plan" },

                // Medical (PolicyTypeId = 3)
                new() { Id = 21, PolicyTypeId = 3, Name = "Health Insurance" },
                new() { Id = 22, PolicyTypeId = 3, Name = "Group Health" },
                new() { Id = 23, PolicyTypeId = 3, Name = "Critical Illness" },
                new() { Id = 24, PolicyTypeId = 3, Name = "Senior Health" },
                new() { Id = 25, PolicyTypeId = 3, Name = "Health Secure Plan" }
            };

            await context.PolicyTypes.AddRangeAsync(types);
            await context.SaveChangesAsync();
            
            await context.PolicyNames.AddRangeAsync(names);
            await context.SaveChangesAsync();
            
            Log.Information("Policy background seed completed successfully.");
        }
    }
}
