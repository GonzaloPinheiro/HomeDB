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
        public async Task<FolderItem?> GetByIdAsync(int folderId, CancellationToken cToken, bool asNoTracking = true)
        {
            IQueryable<FolderItem> query = _context.FolderItems;

            //Aplicar AsNoTracking si se especifica
            if (asNoTracking)
                query = query.AsNoTracking();

            //Devolver resultado
            return await query.FirstOrDefaultAsync(f => f.Id == folderId, cToken);
        }

        //Busca los folders hijos del folder padre indicado. (Filtra por el ownerId)
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
        public Task DeleteAsync(FolderItem folderItem, CancellationToken cToken)
        {
            _context.FolderItems.Remove(folderItem);
            return Task.CompletedTask;
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

        //Comprueba si potentialDescendantId es descendiente de folderId (o el propio folderId).
        public async Task<bool> IsDescendantAsync(int folderId, int potentialDescendantId, CancellationToken cToken)
        {
            int count = await _context.Database
                .SqlQuery<int>($"""
                    WITH RECURSIVE descendants AS (
                        SELECT id FROM folder_items WHERE id = {folderId}
                        UNION ALL
                        SELECT f.id FROM folder_items f
                        INNER JOIN descendants d ON f.parent_folder_id = d.id
                    )
                    SELECT CAST(COUNT(1) AS int) AS "Value" FROM descendants WHERE id = {potentialDescendantId}
                    """)
                .FirstAsync(cToken);

            return count > 0;
        }

        //Sube por el árbol desde potentialDescendantId hacia la raíz buscando folderId (menos eficiente: una query por nivel).
        public async Task<bool> IsDescendantAsync2(int folderId, int potentialDescendantId, CancellationToken cToken)
        {
            //Comprueba si son la misma carpeta
            if (folderId == potentialDescendantId)
                return true;

            int? currentId = potentialDescendantId;

            //Itera sobre las carpetas padre del potentialDescendantId de forma recursiva hasta llegar a la raiz
            while (currentId.HasValue)
            {
                FolderItem? current = await _context.FolderItems
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == currentId.Value, cToken);

                //Padre referenciado no existe en BD (integridad rota), se trata como raíz
                //TODO Agregar log o ver que hacer
                if (current == null)
                    break;

                //El potentialDescendantId es descendiente de folderId
                if (current.ParentFolderId == folderId)
                    return true;

                //Se asigna el parentFolder para seguir iterando
                currentId = current.ParentFolderId;
            }

            //No es descendiente
            return false;
        }
    }
}