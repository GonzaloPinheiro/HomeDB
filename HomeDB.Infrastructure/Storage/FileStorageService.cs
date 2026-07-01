using HomeDB.Application.Options;
using HomeDB.Domain.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace HomeDB.Infrastructure.Storage
{
    public class FileStorageService : IFileStorageService
    {
        private readonly StorageOptions _storageOptions;

        public FileStorageService(IOptions<StorageOptions> options)
        {
            _storageOptions = options.Value;
        }

        public async Task SaveAsync(Stream stream, string storedName, CancellationToken cancellationToken)
        {
            //Si no existe el directorio, lo crea
            Directory.CreateDirectory(_storageOptions.BasePath);

            //Combina la ruta base con el nombre del archivo para obtener la ruta completa
            string fullPath = Path.Combine(_storageOptions.BasePath, storedName);

            //Crea un nuevo archivo y copia el contenido del stream al archivo
            await using FileStream fileStream = new FileStream(fullPath, FileMode.Create);

            //Copia el contenido del stream al archivo de forma asíncrona
            await stream.CopyToAsync(fileStream, cancellationToken);
        }

        public Task DeleteAsync(string storedName, CancellationToken cancellationToken)
        {
            //Combina la ruta base con el nombre del archivo para obtener la ruta completa
            string fullPath = Path.Combine(_storageOptions.BasePath, storedName);

            //Si el archivo existe, lo elimina
            if (File.Exists(fullPath))
                File.Delete(fullPath);

            //Devuelve una tarea completada ya que la operación de eliminación es sincrónica
            return Task.CompletedTask;
        }

        public bool Exists(string storedName)
        {
            //Combina la ruta base con el nombre del archivo para obtener la ruta completa
            string fullPath = Path.Combine(_storageOptions.BasePath, storedName);

            //Devuelve true si el archivo existe, de lo contrario devuelve false
            return File.Exists(fullPath);
        }

        public string GetFilePath(string storedName)
        {
            //Devuelva la ruta completa del archivo combinando la ruta base con el nombre del archivo almacenado
            return Path.Combine(_storageOptions.BasePath, storedName);
        }
    }
}