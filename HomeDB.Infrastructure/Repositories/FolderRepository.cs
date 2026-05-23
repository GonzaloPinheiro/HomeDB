using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeDB.Infrastructure.Repositories
{
    public class FolderRepository : IFolderRepository
    {
        //Variables y objetos globales
        private readonly AppDbContext _context;

        //Constructores
        public FolderRepository(AppDbContext context)
        {
            _context = context;
        }

        //Busca un folder por su id
        public async Task<FolderItem?> GetByIdAsync(int folderId, CancellationToken cToken)
        {
            return await _context.FolderItems
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == folderId, cToken);
        }

        //Busca los folers hijos del folder padre indicado. (Filtra por el ownerId)
        public async Task<IEnumerable<FolderItem>> GetByParentAsync(int ownerId, int? parentFolderId, CancellationToken cToken)
        {
            return await _context.FolderItems
                .AsNoTracking()
                .Where(f => f.OwnerId == ownerId && f.ParentFolderId == parentFolderId)
                .ToListAsync(cToken);
        }

        //Agrega un nuevo folder a la base de datos.
        public async Task CreateAsync(FolderItem folderItem, CancellationToken cToken)
        {
            await _context.FolderItems.AddAsync(folderItem, cToken);
        }

        //Elimina un folder de la base de datos.
        public async Task DeleteAsync(FolderItem folderItem, CancellationToken cToken)
        {
            _context.FolderItems.Remove(folderItem);
        }

        //Comprueba si un folder tiene archivos asociados en la base de datos.
        public Task<bool> HasFilesAsync(int folderId, CancellationToken cToken)
        {
            return _context.FileItems.AnyAsync(f => f.FolderId == folderId, cToken);
        }

        //Comprueba si un folder tiene subcarpetas asociadas en la base de datos.
        public Task<bool> HasSubfoldersAsync(int folderId, CancellationToken cToken)
        {
            return _context.FolderItems.AnyAsync(f => f.ParentFolderId == folderId, cToken);
        }

        //Persiste los cambios realizados en la base de datos.
        public async Task SaveChangesAsync(CancellationToken cToken)
        {
            await _context.SaveChangesAsync(cToken);
        }
    }
}