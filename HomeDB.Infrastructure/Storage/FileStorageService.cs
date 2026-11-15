using HomeDB.Domain.Common;
using HomeDB.Domain.Interfaces.Services;

namespace HomeDB.Infrastructure.Storage
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IStorageLocationResolver _locationResolver;

        public FileStorageService(IStorageLocationResolver locationResolver)
        {
            _locationResolver = locationResolver;
        }

        public async Task SaveAsync(Stream stream, string storedName, CancellationToken cancellationToken) //TODO MODIFICAR PARA ADAPTARLO AL SISTEMA DE CHUNKS Y SU NOMENCLATURA DE NOMBRES DE ARCHIVOS
        {
            string basePath = _locationResolver.ResolveDefaultBasePath(LocationUsage.Personal);

            //Si no existe el directorio, lo crea
            Directory.CreateDirectory(basePath);

            //Combina la ruta base con el nombre del archivo para obtener la ruta completa
            string fullPath = Path.Combine(basePath, storedName);

            //Crea un nuevo archivo y copia el contenido del stream al archivo
            await using FileStream fileStream = new FileStream(fullPath, FileMode.Create);

            //Copia el contenido del stream al archivo de forma asíncrona
            await stream.CopyToAsync(fileStream, cancellationToken);
        }

        public Task SaveFromPathAsync(string sourceFilePath, string storedName, CancellationToken cancellationToken)
        {
            string basePath = _locationResolver.ResolveDefaultBasePath(LocationUsage.Personal);

            //Si no existe el directorio, lo crea
            Directory.CreateDirectory(basePath);

            //Combina la ruta base con el nombre del archivo para obtener la ruta completa
            string fullPath = Path.Combine(basePath, storedName);

            //Mueve el archivo ya ensamblado a su ubicación final. Si origen y destino están en el mismo volumen
            //es prácticamente instantáneo; si no, .NET hace copy+delete internamente sin lanzar excepción.
            File.Move(sourceFilePath, fullPath, overwrite: true);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(string storedName, CancellationToken cancellationToken)
        {
            //Combina la ruta base con el nombre del archivo para obtener la ruta completa
            string fullPath = Path.Combine(_locationResolver.ResolveDefaultBasePath(LocationUsage.Personal), storedName);

            //Si el archivo existe, lo elimina
            if (File.Exists(fullPath))
                File.Delete(fullPath);

            //Devuelve una tarea completada ya que la operación de eliminación es sincrónica
            return Task.CompletedTask;
        }

        public bool Exists(string storedName)
        {
            //Combina la ruta base con el nombre del archivo para obtener la ruta completa
            string fullPath = Path.Combine(_locationResolver.ResolveDefaultBasePath(LocationUsage.Personal), storedName);

            //Devuelve true si el archivo existe, de lo contrario devuelve false
            return File.Exists(fullPath);
        }

        public string GetFilePath(string storedName)
        {
            //Devuelva la ruta completa del archivo combinando la ruta base con el nombre del archivo almacenado
            return Path.Combine(_locationResolver.ResolveDefaultBasePath(LocationUsage.Personal), storedName);
        }
    }
}