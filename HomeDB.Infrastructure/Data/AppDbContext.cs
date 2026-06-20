using HomeDB.Domain.Common;
using HomeDB.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeDB.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>(); //Tabla con usuarios
        public DbSet<Role> Roles => Set<Role>(); //Tabla con roles
        public DbSet<UserRole> UserRoles => Set<UserRole>(); //Tabla de relaciones entre usuarios y roles
        public DbSet<FolderItem> FolderItems => Set<FolderItem>(); //Tabla con elementos de carpeta
        public DbSet<FileItem> FileItems => Set<FileItem>(); //Tabla con elementos de archivo
        public DbSet<LogEntry> Logs => Set<LogEntry>(); //Tabla para logs de la aplicación
        public DbSet<AuditLogEntry> AuditEntries => Set<AuditLogEntry>(); //Tabla para auditoría de cambios
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>(); //Tabla para tokens de actualización
        public DbSet<SystemMetricsEntry> SystemMetricsEntries => Set<SystemMetricsEntry>(); //Tabla para las métricas del sistema

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Agregar datos iniciales para los roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, RoleName = RolesList.Admin.ToString() },
                new Role { Id = 2, RoleName = RolesList.User.ToString() }
            );
        }
    }
}
