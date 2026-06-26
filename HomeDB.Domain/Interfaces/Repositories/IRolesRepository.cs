
using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces.Repositories
{
    public interface IRolesRepository
    {
        /// <summary>
        /// Devuelve un rol por su id
        /// </summary>
        public Task<Role?> GetRoleAsync(int roleId, CancellationToken cToken, bool asNoTracking = true);

        /// <summary>
        /// Devuelve la lista de roles existentes
        /// </summary>
        public Task<IEnumerable<Role>> GetRolesAsync(CancellationToken cToken, bool asNoTracking = true);

        //Confirma los cambios sobre la base de datos
        Task SaveChangesAsync(CancellationToken cToken);
    }
}