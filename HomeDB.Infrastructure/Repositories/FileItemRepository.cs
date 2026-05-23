using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeDB.Infrastructure.Repositories
{
    public class FileItemRepository : IFileItemRepository
    {
        //Variables y objetos globales
        private readonly AppDbContext _context;

        //Constructores
        public FileItemRepository(AppDbContext context)
        {
            _context = context;
        }

        //Agregar un nuevo FileItem(archivo) a la base de datos
        public async Task AddAsync(FileItem fileItem, CancellationToken cToken)
        {
            await _context.FileItems.AddAsync(fileItem, cToken);
        }

        //Busca el archivo por su id
        public async Task<FileItem?> GetByIdAsync(int id, CancellationToken cToken)
        {
            return await _context.FileItems
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id, cToken);
        }

        //Busca los archivos por su propietario y carpeta (si se especifica)
        public async Task<IEnumerable<FileItem>> GetByOwnerAndFolderAsync(int ownerId, int? folderId, CancellationToken cToken)
        {
            IQueryable<FileItem> query = _context.FileItems
                .Where(f => f.OwnerId == ownerId);

            query = folderId.HasValue
                ? query.Where(f => f.FolderId == folderId.Value)
                : query.Where(f => f.FolderId == null);

            return await query.AsNoTracking().ToListAsync(cToken);
        }

        //Elimina un archivo de la base de datos
        public void DeleteFile(FileItem fileItem)
        {
            //Eliminarlo si lo encuentra
            _context.FileItems.Remove(fileItem);
        }

        //Persistir los cambios en la base de datos
        public async Task SaveChangesAsync(CancellationToken cToken)
        {
            await _context.SaveChangesAsync(cToken);
        }
    }
}
