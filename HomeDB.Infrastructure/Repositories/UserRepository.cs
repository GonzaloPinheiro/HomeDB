using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeDB.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        //Variables y objetos globales
        private readonly AppDbContext _context;

        //Constructores
        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        //Comprueba si existe un usuario con ese nombre
        public async Task<bool> UserExistsAsync(string username, CancellationToken cToken)
        {
            return await _context.Users.AnyAsync(u => u.Username == username, cToken);
        }

        //Comprueba si existe un usuario con ese Id
        public async Task<bool> UserExistsAsync(int userId, CancellationToken cToken)
        {
            return await _context.Users.AnyAsync(u => u.Id == userId, cToken);
        }

        //Comprueba si ya existe un usuario con ese email
        public async Task<bool> EmailExistsAsync(string email, CancellationToken cToken)
        {
            return await _context.Users.AnyAsync(u => u.Email == email, cToken);
        }

        //Devuelve un usuario por su userId
        public async Task<User?> GetUserByIdAsync(int userId, CancellationToken cToken, bool asNoTracking = true)
        {
            IQueryable<User> query = _context.Users;

            //Si se especifica, se ejecuta la consulta sin seguimiento de cambios para mejorar el rendimiento
            if (asNoTracking)
                query = query.AsNoTracking();

            //Devolver resultado
            return await query.FirstOrDefaultAsync(u => u.Id == userId, cToken);
        }

        //Devuelve un usuario con sus roles buscando por el userId
        public async Task<User?> GetUserByIdWithRolesAsync(int userId, CancellationToken cToken, bool asNoTracking = true)
        {
            IQueryable<User> query = _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role);

            if (asNoTracking)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(u => u.Id == userId, cToken);
        }

        //Devuelve el usuario con sus roles asignados
        public async Task<User?> GetByUsernameWithRolesAsync(string username, CancellationToken cToken, bool asNoTracking = true)
        {
            IQueryable<User> query = _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role);

            //Si se especifica, se ejecuta la consulta sin seguimiento de cambios para mejorar el rendimiento
            if (asNoTracking)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(u => u.Username == username, cToken);
        }

        //Devuelve una lista paginada y filtrada de usuarios con sus roles
        public async Task<(IEnumerable<User> Users, int TotalCount)> GetUsersAsync(
            int page, int pageSize,
            int? userId, string? userName, string? email,
            DateTimeOffset? from, DateTimeOffset? to,
            int? roleId, string? roleName,
            CancellationToken cToken)
        {
            IQueryable<User> query = _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .AsNoTracking();

            if (userId.HasValue)
                query = query.Where(u => u.Id == userId.Value);

            if (!string.IsNullOrEmpty(userName))
                query = query.Where(u => u.Username.Contains(userName));

            if (!string.IsNullOrEmpty(email))
                query = query.Where(u => u.Email.Contains(email));

            if (from.HasValue)
                query = query.Where(u => u.CreatedAt >= from.Value.UtcDateTime);

            if (to.HasValue)
                query = query.Where(u => u.CreatedAt <= to.Value.UtcDateTime);

            if (roleId.HasValue)
                query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId.Value));

            if (!string.IsNullOrEmpty(roleName))
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role.RoleName.Contains(roleName)));

            int totalCount = await query.CountAsync(cToken);

            IEnumerable<User> users = await query
                .OrderBy(u => u.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cToken);

            return (users, totalCount);
        }

        //Agrega un usuario a la DB
        public async Task AddUserAsync(User user, CancellationToken cToken)
        {
            await _context.Users.AddAsync(user, cToken);
        }

        //Elimina un usuario de la base de datos
        public void DeleteUser(User user)
        {
            _context.Users.Remove(user);
        }

        //Confirma todos los cambios sobre la base de datos
        public async Task SaveChangesAsync(CancellationToken cToken)
        {
            await _context.SaveChangesAsync(cToken);
        }
    }
}