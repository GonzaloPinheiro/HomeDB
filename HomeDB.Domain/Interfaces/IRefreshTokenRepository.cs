
using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces
{
    public interface IRefreshTokenRepository
    {
        //Agregar un refresh token
        Task AddRefreshTokenAsync(RefreshToken rt, CancellationToken cToken);
            
        //Busca un token por su valor
        Task<RefreshToken?> GetByTokenAsync(string rt, CancellationToken cToken);

        //Confirma los cambios sobre la base de datos
        Task SaveChangesAsync(CancellationToken cToken);
    }
}