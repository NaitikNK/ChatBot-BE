using ChatBot_BE.Model;
using ChatBot_BE.Services;
using Microsoft.EntityFrameworkCore;

namespace ChatBot_BE.Data
{
    public static class UserSeeder
    {
        public static async Task SeedAsync(IUserStore userStore, AppDbContext context)
        {
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
            var userRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "User");

            if (adminRole == null || userRole == null) 
            {
                // This is a sign that RoleSeeder failed or wasn't called
                throw new InvalidOperationException("Cannot seed users because mandatory Roles (Admin/User) are missing.");
            }

            var users = new List<User>
            {
                new()
                {
                    FirstName = "System",
                    LastName = "Admin",
                    Email = "admin@chatbot.com",
                    RoleId = adminRole.Id,
                    OwnerSessionId = "SEEDED_RECORD",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("1234")
                }
            };

            foreach (var user in users)
            {
                var existing = await userStore.GetByEmailAsync(user.Email);
                if (existing == null)
                {
                    await userStore.AddAsync(user);
                }
                else
                {
                    // Force update password if it's null or doesn't look like a BCrypt hash
                    if (string.IsNullOrEmpty(existing.PasswordHash) || !existing.PasswordHash.StartsWith("$2"))
                    {
                        existing.PasswordHash = user.PasswordHash;
                        await userStore.UpdateAsync(existing.Id, existing);
                    }
                }
            }
        }
    }
}
