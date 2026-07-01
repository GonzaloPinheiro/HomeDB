using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces.Repositories
{
    public interface IUserSettingsRepository
    {
        /// <summary>
        /// Obtiene la configuración del usuario por su Id de usuario.
        /// </summary>
        Task<UserSettings?> GetByUserIdAsync(int userId, CancellationToken cToken, bool asNoTracking = true);

        /// <summary>
        /// Persiste los cambios realizados en la configuración del usuario.
        /// </summary>
        Task SaveChangesAsync(CancellationToken cToken);
    }
}