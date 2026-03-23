using ChatBot_BE.Model;
using Microsoft.EntityFrameworkCore;

namespace ChatBot_BE.Data
{
    public static class RoleSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (await context.Roles.AnyAsync()) 
            {
                // Already has data, skip
                return;
            }

            var roles = new List<Role>
            {
                new() { RoleName = "Admin" },
                new() { RoleName = "User" }
            };

            context.Roles.AddRange(roles);
            await context.SaveChangesAsync();
        }
    }
}
