using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeDB.Infrastructure.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        //Variables y objetos globales
        private readonly AppDbContext _context;

        //Constructores
        public RefreshTokenRepository(AppDbContext context)
        {
            _context = context;
        }

        //Agregar un refresh token
        public async Task AddRefreshTokenAsync(RefreshToken rt, CancellationToken cToken)
        {
            await _context.RefreshTokens.AddAsync(rt, cToken);
        }
        //Busca un token por su valor
        public async Task<RefreshToken?> GetByTokenAsync(string rt, CancellationToken cToken)
        {
            return await _context.RefreshTokens
                .Include(r => r.User!)
                .ThenInclude(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(r => r.Token == rt, cToken);
        }

        //Hace revoke de todos los refresk tokens del usuario indicado
        public async Task RevokeAllByUserIdAsync(int userId, CancellationToken cToken)
        {
            await _context.RefreshTokens
                    .Where(r => r.UserId == userId && !r.IsRevoked)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(r => r.IsRevoked, true));

        }

        //Confirma los cambios sobre la base de datos
        public async Task SaveChangesAsync(CancellationToken cToken)
        {
            await _context.SaveChangesAsync(cToken);
        }
    }
}
