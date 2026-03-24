using ChatBot_BE.Model;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ChatBot_BE.Data
{
    public static class MasterDataSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // Seed Policy Types
            if (!await context.PolicyTypes.AnyAsync())
            {
                var types = new List<PolicyTypeMaster>
                {
                    new() { Id = 1, Name = "Personal" },
                    new() { Id = 2, Name = "Vehicle" },
                    new() { Id = 3, Name = "Medical" }
                };

                await context.PolicyTypes.AddRangeAsync(types);
                await context.SaveChangesAsync();
            }

            // Seed Policy Names
            if (!await context.PolicyNames.AnyAsync())
            {
                var names = new List<PolicyNameMaster>
                {
                    // Personal (PolicyTypeId = 1)
                    new() { Id = 11, PolicyTypeId = 1, Name = "Personal Shield Plan" },
                    new() { Id = 12, PolicyTypeId = 1, Name = "Family Protection Plan" },
                    new() { Id = 13, PolicyTypeId = 1, Name = "Individual Cover Plan" },

                    // Vehicle (PolicyTypeId = 2)
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

                await context.PolicyNames.AddRangeAsync(names);
                await context.SaveChangesAsync();
            }

            Log.Information("Policy master data seed completed successfully.");
        }
    }
}
