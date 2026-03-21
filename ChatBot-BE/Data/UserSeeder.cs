using ChatBot_BE.Model;
using ChatBot_BE.Services;

namespace ChatBot_BE.Data
{
    public static class UserSeeder
    {
        public static async Task SeedAsync(IUserStore userStore)
        {
            var users = new List<User>
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
                    DateOfBirth = DateTime.Parse("1998-05-12"),
                    OwnerSessionId = "SEEDED_RECORD"
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
                    DateOfBirth = DateTime.Parse("1990-11-25"),
                    OwnerSessionId = "SEEDED_RECORD"
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
                    DateOfBirth = DateTime.Parse("1995-03-18"),
                    OwnerSessionId = "SEEDED_RECORD"
                },
                new()
                {
                    FirstName = "Rahul",
                    LastName = "Verma",
                    PolicyNumber = "POL1004",
                    Email = "rahul.verma@example.com",
                    PolicyType = "Vehicle",
                    PolicyName = "Bike Insurance Plan",
                    PhoneNumber = "9012345678",
                    Address = "22 Park Street",
                    City = "Kolkata",
                    State = "West Bengal",
                    PostalCode = "700016",
                    Country = "India",
                    DateOfBirth = DateTime.Parse("1988-07-09"),
                    OwnerSessionId = "SEEDED_RECORD"
                },
                new()
                {
                    FirstName = "Sneha",
                    LastName = "Reddy",
                    PolicyNumber = "POL1005",
                    Email = "sneha.reddy@example.com",
                    PolicyType = "Personal",
                    PolicyName = "Family Protection Plan",
                    PhoneNumber = "9090909090",
                    Address = "10 Banjara Hills",
                    City = "Hyderabad",
                    State = "Telangana",
                    PostalCode = "500034",
                    Country = "India",
                    DateOfBirth = DateTime.Parse("1992-01-30"),
                    OwnerSessionId = "SEEDED_RECORD"
                }
            };

            // Check if data already exists
            var existingUsers = await userStore.GetAllAsync();
            if (existingUsers.Count > 0)
            {
                return; // Data already seeded
            }

            foreach (var user in users)
            {
                try
                {
                    await userStore.AddAsync(user);
                }
                catch (Exception)
                {
                    // Skip if user already exists (duplicate policy number or email)
                }
            }
        }
    }
}
