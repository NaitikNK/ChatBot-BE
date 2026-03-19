using Microsoft.EntityFrameworkCore;
using ChatBot_BE.Model;

namespace ChatBot_BE.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<PolicyTypeMaster> PolicyTypes { get; set; }
        public DbSet<PolicyNameMaster> PolicyNames { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
