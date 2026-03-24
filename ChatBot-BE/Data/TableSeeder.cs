using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Serilog;

namespace ChatBot_BE.Data
{
    /// <summary>
    /// Ensures all tables defined in AppDbContext exist in the database.
    /// Reads table definitions dynamically from the EF model — nothing is hardcoded.
    /// </summary>
    public static class TableSeeder
    {
        public static async Task EnsureTablesAsync(AppDbContext context)
        {
            var entityTypes = context.Model.GetEntityTypes();
            var existingTables = await GetExistingTablesAsync(context);

            foreach (var entityType in entityTypes)
            {
                var tableName = entityType.GetTableName();
                if (string.IsNullOrEmpty(tableName)) continue;

                if (existingTables.Contains(tableName, StringComparer.OrdinalIgnoreCase))
                {
                    Log.Information("Table '{TableName}' already exists. Skipping.", tableName);
                    continue;
                }

                Log.Warning("Table '{TableName}' is MISSING. Creating manually...", tableName);
                var createSql = GenerateCreateTableSql(entityType, tableName);
                try
                {
                    await context.Database.ExecuteSqlRawAsync(createSql);
                    Log.Information("Table '{TableName}' created successfully.", tableName);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to create table '{TableName}'.", tableName);
                }
            }
        }

        private static async Task<HashSet<string>> GetExistingTablesAsync(AppDbContext context)
        {
            var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'";
            await context.Database.OpenConnectionAsync();
            try
            {
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tables.Add(reader.GetString(0));
                }
            }
            finally
            {
                await context.Database.CloseConnectionAsync();
            }
            return tables;
        }

        private static string GenerateCreateTableSql(IEntityType entityType, string tableName)
        {
            var columns = new List<string>();
            string? pkColumn = null;
            var isAutoIncrement = false;

            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName();
                var columnType = property.GetColumnType();
                var isNullable = property.IsNullable;
                var isPk = property.IsPrimaryKey();

                if (isPk)
                {
                    pkColumn = columnName;
                    // Check if auto-increment (SERIAL)
                    isAutoIncrement = property.ValueGenerated == ValueGenerated.OnAdd;
                }

                // Use SERIAL for auto-increment primary keys
                if (isPk && isAutoIncrement)
                {
                    columns.Add($"\"{columnName}\" SERIAL PRIMARY KEY");
                }
                else if (isPk)
                {
                    columns.Add($"\"{columnName}\" {columnType} PRIMARY KEY");
                }
                else
                {
                    var nullConstraint = isNullable ? "" : " NOT NULL";
                    columns.Add($"\"{columnName}\" {columnType}{nullConstraint}");
                }
            }

            // Add foreign key constraints
            foreach (var fk in entityType.GetForeignKeys())
            {
                var fkColumns = string.Join(", ", fk.Properties.Select(p => $"\"{p.GetColumnName()}\""));
                var principalTable = fk.PrincipalEntityType.GetTableName();
                var principalColumns = string.Join(", ", fk.PrincipalKey.Properties.Select(p => $"\"{p.GetColumnName()}\""));
                var constraintName = fk.GetConstraintName();
                var deleteAction = fk.DeleteBehavior switch
                {
                    DeleteBehavior.Cascade => "CASCADE",
                    DeleteBehavior.SetNull => "SET NULL",
                    DeleteBehavior.Restrict => "RESTRICT",
                    _ => "NO ACTION"
                };

                columns.Add($"CONSTRAINT \"{constraintName}\" FOREIGN KEY ({fkColumns}) REFERENCES \"{principalTable}\" ({principalColumns}) ON DELETE {deleteAction}");
            }

            var sql = $"CREATE TABLE IF NOT EXISTS \"{tableName}\" (\n    {string.Join(",\n    ", columns)}\n);\n";

            // Add indexes
            foreach (var index in entityType.GetIndexes())
            {
                var indexColumns = string.Join(", ", index.Properties.Select(p => $"\"{p.GetColumnName()}\""));
                var indexName = index.GetDatabaseName();
                var unique = index.IsUnique ? "UNIQUE " : "";
                sql += $"CREATE {unique}INDEX IF NOT EXISTS \"{indexName}\" ON \"{tableName}\" ({indexColumns});\n";
            }

            return sql;
        }
    }
}
