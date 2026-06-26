using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeDB.Infrastructure.Repositories
{
    public class RolesRepository : IRolesRepository
    {
        //Variables y objetos globales
        private readonly AppDbContext _context;

        //Constructores
        public RolesRepository(AppDbContext context)
        {
            _context = context;
        }

        //Devuelve el rol solicitado
        public async Task<Role?> GetRoleAsync(int roleId, CancellationToken cToken, bool asNoTracking = true)
        {
            IQueryable<Role> query = _context.Roles;

            //Aplicar AsNoTracking si se especifica
            if (asNoTracking)
                query = query.AsNoTracking();

            //Devolver resultado
            return await query.FirstOrDefaultAsync(r => r.Id == roleId, cToken);
        }

        //Devuelve una lista de todos los roles
        public async Task<IEnumerable<Role>> GetRolesAsync(CancellationToken cToken, bool asNoTracking = true)
        {
            IQueryable<Role> query = _context.Roles;

            //Aplicar AsNoTracking si se especifica
            if (asNoTracking)
                query = query.AsNoTracking();

            //Devolver resultado
            return await query
                .OrderByDescending(r => r.Id)
                .ToListAsync(cToken);
        }

        //Confirma los cambios sobre la base de datos
        public async Task SaveChangesAsync(CancellationToken cToken)
        {
            await _context.SaveChangesAsync(cToken);
        }
    }
}