using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeDB.Infrastructure.Repositories
{
    public class UserModulePermissionsRepository : IUserModulePermissionsRepository
    {
        private readonly AppDbContext _context;

        public UserModulePermissionsRepository(AppDbContext context)
        {
            _context = context;
        }

        //Busca los modulos del usuario por su ID
        public async Task<UserModulePermissions?> GetByUserIdAsync(int userId, CancellationToken cToken, bool asNoTracking = true)
        {
            var query = _context.UserModulePermissions.AsQueryable();

            //Aplica AsNoTracking si se solicita
            if (asNoTracking)
                query = query.AsNoTracking();

            //Busca el registro de permisos del usuario por su ID
            return await query.FirstOrDefaultAsync(p => p.UserId == userId, cToken);
        }

        //Agrega un registro de permisos de un usuario
        public async Task AddAsync(UserModulePermissions permissions, CancellationToken cToken)
        {
            await _context.UserModulePermissions.AddAsync(permissions, cToken);
        }

        //Periste los cambios en DB
        public async Task SaveChangesAsync(CancellationToken cToken)
        {
            await _context.SaveChangesAsync(cToken);
        }
    }
}