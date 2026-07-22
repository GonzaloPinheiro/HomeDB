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

        #region Getets
        //Busca el archivo por su id
        public async Task<FileItem?> GetByIdAsync(int id, CancellationToken cToken, bool asNoTracking = true)
        {
            //return await _context.FileItems
            //    .AsNoTracking()
            //    .FirstOrDefaultAsync(f => f.Id == id, cToken);

            IQueryable<FileItem> query = _context.FileItems;

            //Aplicar AsNoTracking si se especifica
            if (asNoTracking)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(f => f.Id == id, cToken);
        }

        //Busca entre los archivos filtrando por los parámetros recibidos
        public async Task<(IEnumerable<FileItem> Items, int TotalCount)> SearchFileAsync(
                    string? query, int ownerId, int? folderId, string? contentType,
                    long? minSizeBytes, long? maxSizeBytes, DateTime? uploadedFrom, DateTime? uploadedTo,
                    int pageNumber, int pageSize, CancellationToken cToken)
        {
            //Crear la query
            IQueryable<FileItem> dbQuery = _context.FileItems.AsNoTracking()
                                                             .Where(f => f.OwnerId == ownerId);
            //Aplicar los filtros recibidos
            if (!string.IsNullOrEmpty(query))
                dbQuery = dbQuery.Where(f => EF.Functions.ILike(f.FileName, $"%{query}%"));

            if (folderId.HasValue)
                dbQuery = dbQuery.Where(f => f.FolderId == folderId.Value);

            if (!string.IsNullOrEmpty(contentType))
                dbQuery = dbQuery.Where(f => f.ContentType == contentType);

            if (minSizeBytes.HasValue)
                dbQuery = dbQuery.Where(f => f.SizeBytes >= minSizeBytes.Value);

            if (maxSizeBytes.HasValue)
                dbQuery = dbQuery.Where(f => f.SizeBytes <= maxSizeBytes.Value);

            if (uploadedFrom.HasValue)
                dbQuery = dbQuery.Where(f => f.UploadedAt >= uploadedFrom.Value);

            if (uploadedTo.HasValue)
                dbQuery = dbQuery.Where(f => f.UploadedAt <= uploadedTo.Value);

            //Contar los elementos encontrados
            int totalCount = await dbQuery.CountAsync(cToken);

            //Realizar query
            IEnumerable<FileItem> items = await dbQuery
                .OrderBy(f => f.FileName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cToken);

            //Devolver lo encontrado
            return (items, totalCount);
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

        //TODO Replantear query para obtener las estadísticas de almacenamiento de forma más eficiente, evitando múltiples consultas a la base de datos.
        //Devolver estadísticas de almacenamiento para el usuario especificado: total de archivos, tamaño total en bytes y total de carpetas
        public async Task<(int TotalFiles, long TotalSizeBytes, int TotalFolders)> GetUserStatsAsync(int ownerId, CancellationToken cToken)
        {
            //Contar la cantidad de archivos del usuario (excluyendo carpetas)
            int totalFiles = await _context.FileItems
                .Where(f => f.OwnerId == ownerId)
                .CountAsync(cToken);

            //Sumar el tamaño total en bytes de los archivos del usuario
            long totalSizeBytes = await _context.FileItems
                .Where(f => f.OwnerId == ownerId)
                .SumAsync(f => f.SizeBytes, cToken);

            //Contar la cantidad de carpetas del usuario
            int totalFolders = await _context.FolderItems
                .Where(f => f.OwnerId == ownerId)
                .CountAsync(cToken);

            //Devolver las estadísticas como una tupla
            return (totalFiles, totalSizeBytes, totalFolders);
        }
        #endregion

        //Elimina un archivo de la base de datos
        public void DeleteFile(FileItem fileItem)
        {
            //Eliminarlo si lo encuentra
            _context.FileItems.Remove(fileItem);
        }

        //Comprueba si un usuario tiene archivos asociados en la base de datos
        public Task<bool> UserHasFilesAsync(int ownerId, CancellationToken cToken)
        {
            return _context.FileItems.AnyAsync(f => f.OwnerId == ownerId, cToken);
        }

        //Persistir los cambios en la base de datos
        public async Task SaveChangesAsync(CancellationToken cToken)
        {
            await _context.SaveChangesAsync(cToken);
        }
    }
}