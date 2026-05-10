using HomeDB.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace HomeDB.Infrastructure.Storage
{
    public class FileStorageService : IFileStorageService
    {
        private readonly StorageOptions _options;

        public FileStorageService(IOptions<StorageOptions> options)
        {
            _options = options.Value;
        }

        public async Task SaveAsync(Stream stream, string storedName, CancellationToken cancellationToken)
        {
            //Si no existe el directorio, lo crea
            Directory.CreateDirectory(_options.BasePath);

            //Combina la ruta base con el nombre del archivo para obtener la ruta completa
            string fullPath = Path.Combine(_options.BasePath, storedName);

            //Crea un nuevo archivo y copia el contenido del stream al archivo
            await using FileStream fileStream = new FileStream(fullPath, FileMode.Create);

            //Copia el contenido del stream al archivo de forma asíncrona
            await stream.CopyToAsync(fileStream, cancellationToken);
        }

        public Task DeleteAsync(string storedName, CancellationToken cancellationToken)
        {
            //Combina la ruta base con el nombre del archivo para obtener la ruta completa
            string fullPath = Path.Combine(_options.BasePath, storedName);

            //Si el archivo existe, lo elimina
            if (File.Exists(fullPath))
                File.Delete(fullPath);

            //Devuelve una tarea completada ya que la operación de eliminación es sincrónica
            return Task.CompletedTask;
        }

        public bool Exists(string storedName)
        {
            //Combina la ruta base con el nombre del archivo para obtener la ruta completa
            string fullPath = Path.Combine(_options.BasePath, storedName);

            //Devuelve true si el archivo existe, de lo contrario devuelve false
            return File.Exists(fullPath);
        }
    }
}