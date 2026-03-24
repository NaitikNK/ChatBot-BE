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
            // Step 1: Apply all pending migrations (creates tables + schema changes)
            Log.Information("Checking for database migrations...");
            var assembly = typeof(AppDbContext).Assembly;
            var assemblyName = assembly.GetName().Name;
            Log.Information("EF Core is searching for migrations in assembly: {Assembly}", assemblyName);
            
            // DEBUG: Manually search for migration classes via reflection
            try
            {
                var migrationTypes = assembly.GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(Microsoft.EntityFrameworkCore.Migrations.Migration)))
                    .ToList();
                Log.Information("Reflection found {Count} migration classes in assembly: {Classes}", 
                    migrationTypes.Count, string.Join(", ", migrationTypes.Select(t => t.Name)));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to scan assembly types via reflection.");
            }

            var pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToList();
            var appliedMigrations = (await context.Database.GetAppliedMigrationsAsync()).ToList();
            
            Log.Information("Migrations Found - Applied: {Applied}, Pending: {Pending}", appliedMigrations.Count, pendingMigrations.Count);
            
            if (pendingMigrations.Any())
            {
                Log.Information("Applying {Count} migrations...", pendingMigrations.Count);
                await context.Database.MigrateAsync();
                Log.Information("Migrations applied successfully.");
            }
            else if (appliedMigrations.Count == 0 && pendingMigrations.Count == 0)
            {
                Log.Error("❌ CRITICAL: EF Core found 0 migrations in assembly '{Assembly}'.", assemblyName);
                Log.Error("This is likely a build/deployment issue where Migrations folder is not being compiled.");
            }
            else
            {
                Log.Information("No pending migrations. Database is up to date according to history.");
            }

            // [CRITICAL CHECK] Verify core tables actually exist
            // Sometimes __EFMigrationsHistory is out of sync with actual tables on Render
            Log.Information("Verifying critical tables exist...");
            var tableExists = await DoesTableExistAsync(context, "PolicyTypes");
            if (!tableExists)
            {
                Log.Warning("⚠️ Table 'PolicyTypes' is missing despite Migrations history.");
                Log.Warning("Attempting emergency recovery using EnsureCreatedAsync...");
                
                // EnsureCreated creates the database and all tables if they don't exist
                // It will bypass __EFMigrationsHistory if that table is corrupted
                await context.Database.EnsureCreatedAsync();
                Log.Information("Recovery EnsureCreated completed.");
                
                // Final check
                if (!await DoesTableExistAsync(context, "PolicyTypes"))
                {
                    throw new InvalidOperationException("FATAL: Database schema could not be created. Relation 'PolicyTypes' still missing.");
                }
            }

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

        private static async Task<bool> DoesTableExistAsync(AppDbContext context, string tableName)
        {
            try
            {
                using var command = context.Database.GetDbConnection().CreateCommand();
                command.CommandText = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '{tableName}'";
                
                if (context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                {
                    await context.Database.OpenConnectionAsync();
                }
                
                var count = (long?)await command.ExecuteScalarAsync();
                return count > 0;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error checking table existence for {TableName}", tableName);
                return false;
            }
        }
    }
}
