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
        public DbSet<UserSettings> UserSettings => Set<UserSettings>(); //Tabla con configuraciones de usuario
        public DbSet<UserAdminSettings> UserAdminSettings => Set<UserAdminSettings>(); //Tabla con configuraciones de usuario modificables solo por administradores
        public DbSet<UserModulePermissions> UserModulePermissions => Set<UserModulePermissions>(); //Tabla de permisos de módulos por usuario
        public DbSet<FolderItem> FolderItems => Set<FolderItem>(); //Tabla con elementos de carpeta
        public DbSet<FileItem> FileItems => Set<FileItem>(); //Tabla con elementos de archivo
        public DbSet<LogEntry> Logs => Set<LogEntry>(); //Tabla para logs de la aplicación
        public DbSet<AuditLogEntry> AuditEntries => Set<AuditLogEntry>(); //Tabla para auditoría de cambios
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>(); //Tabla para tokens de actualización
        public DbSet<SystemMetricsEntry> SystemMetricsEntries => Set<SystemMetricsEntry>(); //Tabla para las métricas del sistema
        public DbSet<UploadSession> UploadSessions => Set<UploadSession>(); //Tabla para las sesiones de carga de archivos
        public DbSet<UploadChunk> UploadChunks => Set<UploadChunk>(); //Tabla para los fragmentos de las sesiones de carga de archivos

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Relación 1 a 1 entre User y UserModulePermissions
            modelBuilder.Entity<User>()
                .HasOne(u => u.Settings)
                .WithOne(s => s.User)
                .HasForeignKey<UserSettings>(s => s.UserId);

            modelBuilder.Entity<User>()
                .HasOne(u => u.AdminSettings)
                .WithOne(s => s.User)
                .HasForeignKey<UserAdminSettings>(s => s.UserId);

            modelBuilder.Entity<User>()
                .HasOne(u => u.ModulePermissions)
                .WithOne(p => p.User)
                .HasForeignKey<UserModulePermissions>(p => p.UserId);

            //Agregar datos iniciales para los roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, RoleName = RolesList.Admin.ToString() },
                new Role { Id = 2, RoleName = RolesList.User.ToString() }
            );

            //Agregar las tablas de la carpeta "uploads" al esquema "uploads"
            modelBuilder.Entity<UploadSession>(entity =>
            {
                entity.ToTable("upload_sessions", "uploads");
                entity.HasKey(session => session.Id);
                entity.HasIndex(session => session.SessionId).IsUnique();
                entity.Property(session => session.Status).HasConversion<string>();
            });

            modelBuilder.Entity<UploadChunk>(entity =>
            {
                entity.ToTable("upload_chunks", "uploads");
                entity.HasKey(chunk => chunk.Id);
                entity.HasIndex(chunk => new { chunk.UploadSessionId, chunk.ChunkNumber }).IsUnique();

                entity.HasOne(chunk => chunk.UploadSession)
                    .WithMany(session => session.Chunks)
                    .HasForeignKey(chunk => chunk.UploadSessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
