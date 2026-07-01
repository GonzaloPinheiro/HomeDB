using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces.Repositories
{
    public interface IUserAdminSettingsRepository
    {
        /// <summary>
        /// Devuelve la configuración del usuario de administración del usuario por su Id.
        /// </summary>
        Task<UserAdminSettings?> GetByUserIdAsync(int userId, CancellationToken cToken, bool asNoTracking = true);

        /// <summary>
        /// Persiste los cambios realizados en la configuración del usuario de administración de manera asíncrona.
        /// </summary>
        Task SaveChangesAsync(CancellationToken cToken);
    }
}