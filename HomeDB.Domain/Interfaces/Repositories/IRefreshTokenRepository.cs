using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces.Repositories
{
    public interface IRefreshTokenRepository
    {
        /// <summary>
        /// Agregar un refresh token
        /// </summary>
        Task AddRefreshTokenAsync(RefreshToken rt, CancellationToken cToken);
            
        /// <summary>
        /// Busca un token por su valor
        /// </summary>
        Task<RefreshToken?> GetByTokenAsync(string refreshToken, CancellationToken cToken);

        Task RevokeAllByUserIdAsync(int userId, CancellationToken cToken);

        /// <summary>
        /// Confirma los cambios sobre la base de datos
        /// </summary>
        Task SaveChangesAsync(CancellationToken cToken);
    }
}