using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces.Repositories
{
    public interface IUserModulePermissionsRepository
    {
        /// <summary>
        /// Devuelve los permisos de módulos para un usuario específico por su ID.
        /// </summary>
        Task<UserModulePermissions?> GetByUserIdAsync(int userId, CancellationToken cToken, bool asNoTracking = true);

        /// <summary>
        /// Agrega nuevos permisos de módulos para un usuario.
        /// </summary>
        Task AddAsync(UserModulePermissions permissions, CancellationToken cToken);

        /// <summary>
        /// Persiste los cambios realizados en la base de datos de manera asíncrona.
        /// </summary>
        Task SaveChangesAsync(CancellationToken cToken);
    }
}