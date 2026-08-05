using HomeDB.Domain.Common.Enums;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeDB.Infrastructure.Repositories
{
    public class UploadSessionRepository : IUploadSessionRepository
    {
        private readonly AppDbContext _context;

        public UploadSessionRepository(AppDbContext context)
        {
            _context = context;
        }

        //Inserta un nuevo UploadSession en la base de datos y devuelve la entidad insertada.
        public async Task<UploadSession> AddAsync(UploadSession session, CancellationToken cToken)
        {
            await _context.UploadSessions.AddAsync(session, cToken);
            return session;
        }

        //Busca un UploadSession por su SessionId en la base de datos y devuelve la entidad encontrada o null si no existe.
        public async Task<UploadSession?> GetBySessionIdAsync(Guid sessionId, CancellationToken cToken, bool asNoTracking = true)
        {
            IQueryable<UploadSession> query = _context.UploadSessions;

            //Si se especifica, se ejecuta la consulta sin seguimiento de cambios para mejorar el rendimiento
            if (asNoTracking)
                query = query.AsNoTracking();

            //Devolver resultado
            return await query
                .FirstOrDefaultAsync(session => session.SessionId == sessionId, cToken);
        }

        //Busca todas las sesiones de subida que han sido finalizadas (completadas o canceladas) y devuelve una lista de entidades.
        public async Task<List<UploadSession>> GetFinishedSessionsAsync(CancellationToken cToken)
        {
            //Con tracking
            return await _context.UploadSessions
                .Where(session => session.Status == UploadSessionStatus.Completed || session.Status == UploadSessionStatus.Cancelled)
                .ToListAsync(cToken);
        }

        //Incrementa el valor del chunksize con el recibido. Devuelve false si superaba el máximo
        public async Task<bool> TryIncrementReceivedSizeBytesAsync(int sessionId, long chunkSizeBytes, long maxSizeBytes, CancellationToken cToken)
        {
            int rowsAffected = await _context.UploadSessions
                .Where(session => session.Id == sessionId
                    && session.ReceivedSizeBytes + chunkSizeBytes <= maxSizeBytes)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(session => session.ReceivedSizeBytes, session => session.ReceivedSizeBytes + chunkSizeBytes),
                    cToken);

            return rowsAffected > 0;
        }

        //Elimina un conjunto de UploadSessions de la base de datos.
        public void RemoveRange(IEnumerable<UploadSession> sessions)
        {
            _context.UploadSessions.RemoveRange(sessions);
        }

        //Persiste los cambios realizados en el repositorio de sesiones de subida.
        public async Task SaveChangesAsync(CancellationToken cToken)
        {
            await _context.SaveChangesAsync(cToken);
        }
    }
}
