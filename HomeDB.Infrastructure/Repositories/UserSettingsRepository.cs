using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeDB.Infrastructure.Repositories
{
    public class UserSettingsRepository : IUserSettingsRepository
    {
        //Variables y objetos globales
        private readonly AppDbContext _context;

        //Constructores
        public UserSettingsRepository(AppDbContext context)
        {
            _context = context;
        }

        //Devuelve la configuración del usuario por su Id
        public async Task<UserSettings?> GetByUserIdAsync(int userId, CancellationToken cToken, bool asNoTracking = true)
        {
            var query = _context.UserSettings.Where(s => s.UserId == userId);

            //Si se solicita, se aplica AsNoTracking para mejorar el rendimiento en consultas de solo lectura
            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            //Devuelve la primera configuración encontrada o null si no existe
            return await query.FirstOrDefaultAsync(cToken);
        }

        //Persiste los cambios en la DB
        public async Task SaveChangesAsync(CancellationToken cToken)
        {
            await _context.SaveChangesAsync(cToken);
        }
    }
}