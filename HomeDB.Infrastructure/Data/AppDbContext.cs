using HomeDB.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using EFCore.NamingConventions;

namespace HomeDB.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<FolderItem> FolderItems => Set<FolderItem>();
        public DbSet<FileItem> FileItems => Set<FileItem>();
        public DbSet<LogEntry> Logs => Set<LogEntry>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
