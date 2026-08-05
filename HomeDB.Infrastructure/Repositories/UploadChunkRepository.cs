using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeDB.Infrastructure.Repositories
{
    public class UploadChunkRepository : IUploadChunkRepository
    {
        //Varibles y objetos globales
        private readonly AppDbContext _context;

        //Constructores
        public UploadChunkRepository(AppDbContext context)
        {
            _context = context;
        }

        //Verifica si un chunk ya existe en la base de datos para una sesión de subida específica
        public async Task<bool> ExistsAsync(int uploadSessionId, int chunkNumber, CancellationToken cToken)
        {
            return await _context.UploadChunks
                .AsNoTracking()
                .AnyAsync(chunk => chunk.UploadSessionId == uploadSessionId && chunk.ChunkNumber == chunkNumber, cToken);
        }

        //Agrega un nuevo chunk a la base de datos
        public async Task AddAsync(UploadChunk chunk, CancellationToken cToken)
        {
            await _context.UploadChunks.AddAsync(chunk, cToken);
        }

        //Obtiene una lista de números de chunks recibidos para una sesión de subida específica
        public async Task<List<int>> GetReceivedChunkNumbersAsync(int uploadSessionId, CancellationToken cToken)
        {
            return await _context.UploadChunks
                .AsNoTracking()
                .Where(chunk => chunk.UploadSessionId == uploadSessionId)
                .Select(chunk => chunk.ChunkNumber)
                .OrderBy(number => number)
                .ToListAsync(cToken);
        }

        //Persiste los cambios realizados en el repositorio de chunks de subida.
        public async Task SaveChangesAsync(CancellationToken cToken)
        {
            await _context.SaveChangesAsync(cToken);
        }
    }
}