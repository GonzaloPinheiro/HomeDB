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
        public async Task<bool> UsernameExistsAsync(string username, CancellationToken cToken)
        {
            return await _context.Users.AnyAsync(u => u.Username == username, cToken);
        }

        //Devuelve el usuario con sus roles asignados
        public async Task<User?> GetByUsernameWithRolesAsync(string username, CancellationToken cToken)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == username, cToken);
        }

        //Agrega un usuario a la DB
        public async Task AddUserAsync(User user, CancellationToken cToken)
        {
            await _context.Users.AddAsync(user, cToken);
        }

        //Confirma todos los cambios sobre la base de datos
        public async Task SaveChangesAsync(CancellationToken cToken)
        {
            await _context.SaveChangesAsync(cToken);
        }
    }
}
