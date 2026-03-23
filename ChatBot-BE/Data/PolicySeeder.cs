using ChatBot_BE.Model;
using ChatBot_BE.Services;
using Microsoft.EntityFrameworkCore;

namespace ChatBot_BE.Data
{
    public static class PolicySeeder
    {
        public static async Task SeedAsync(IPolicyStore policyStore, AppDbContext context)
        {
            // We need a user to associate these policies with.
            // Let's use the 'Naitik Patel' user for testing or the first Admin.
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@chatbot.com") 
                       ?? await context.Users.FirstOrDefaultAsync();

            if (user == null) return;

            var policies = new List<Policy>
            {
                new()
                {
                    FirstName = "Naitik",
                    LastName = "Patel",
                    PolicyNumber = "POL1001",
                    Email = "naitik.patel@example.com",
                    PolicyType = "Personal",
                    PolicyName = "Personal Shield Plan",
                    PhoneNumber = "9876543210",
                    Address = "123 MG Road",
                    City = "Vadodara",
                    State = "Gujarat",
                    PostalCode = "390001",
                    Country = "India",
                    DateOfBirth = DateTime.SpecifyKind(DateTime.Parse("1998-05-12"), DateTimeKind.Utc),
                    OwnerSessionId = "SEEDED_RECORD",
                    UserId = user.Id
                },
                new()
                {
                    FirstName = "Amit",
                    LastName = "Shah",
                    PolicyNumber = "POL1002",
                    Email = "amit.shah@example.com",
                    PolicyType = "Vehicle",
                    PolicyName = "Car Protection Plan",
                    PhoneNumber = "9123456780",
                    Address = "45 SG Highway",
                    City = "Ahmedabad",
                    State = "Gujarat",
                    PostalCode = "380015",
                    Country = "India",
                    DateOfBirth = DateTime.SpecifyKind(DateTime.Parse("1990-11-25"), DateTimeKind.Utc),
                    OwnerSessionId = "SEEDED_RECORD",
                    UserId = user.Id
                },
                new()
                {
                    FirstName = "Priya",
                    LastName = "Mehta",
                    PolicyNumber = "POL1003",
                    Email = "priya.mehta@example.com",
                    PolicyType = "Medical",
                    PolicyName = "Health Secure Plan",
                    PhoneNumber = "9988776655",
                    Address = "78 Marine Drive",
                    City = "Mumbai",
                    State = "Maharashtra",
                    PostalCode = "400002",
                    Country = "India",
                    DateOfBirth = DateTime.SpecifyKind(DateTime.Parse("1995-03-18"), DateTimeKind.Utc),
                    OwnerSessionId = "SEEDED_RECORD",
                    UserId = user.Id
                }
            };

            foreach (var policy in policies)
            {
                var existing = await policyStore.GetByPolicyNumberAsync(policy.PolicyNumber);
                if (existing == null)
                {
                    await policyStore.AddAsync(policy);
                }
            }
        }
    }
}
