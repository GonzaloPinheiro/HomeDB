using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces;
using HomeDB.Infrastructure.Data;

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

        //Persistir los cambios en la base de datos
        public async Task SaveChangesAsync(CancellationToken cToken)
        {
            await _context.SaveChangesAsync(cToken);
        }
    }
}
