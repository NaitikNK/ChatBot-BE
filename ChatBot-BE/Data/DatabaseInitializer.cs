using ChatBot_BE.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ChatBot_BE.Data
{
    /// <summary>
    /// Single entry point for all database initialization: migrations + seeding.
    /// Call DatabaseInitializer.InitializeAsync() once from Program.cs.
    /// </summary>
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(
            AppDbContext context,
            IUserStore userStore,
            IPolicyStore policyStore,
            IKnowledgeBaseService knowledgeBase,
            IWebHostEnvironment environment)
        {
            // Step 1: Apply all pending migrations (creates tables + schema changes)
            Log.Information("Applying database migrations...");
            await context.Database.MigrateAsync();
            Log.Information("Database migrated successfully.");

            // Step 2: Seed master data (Policy Types and Names)
            await MasterDataSeeder.SeedAsync(context);
            Log.Information("Master data seeded.");

            // Step 3: Seed roles (required before users)
            await RoleSeeder.SeedAsync(context);
            Log.Information("Roles seeded.");

            // Step 4: Seed users (required before policies)
            await UserSeeder.SeedAsync(userStore, context);
            Log.Information("Users seeded.");

            // Step 5: Seed policies
            await PolicySeeder.SeedAsync(policyStore, context);
            Log.Information("Policies seeded.");

            // Step 6: Seed knowledge base
            await KnowledgeBaseSeeder.SeedAsync(knowledgeBase, environment);
            Log.Information("Knowledge base seeded.");

            Log.Information("Database initialization completed successfully.");
        }
    }
}
