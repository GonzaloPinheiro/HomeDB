using HomeDB.Domain.Common;
using HomeDB.Domain.Interfaces.Services;

namespace HomeDB.Infrastructure.Storage
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IStorageLocationResolver _locationResolver;
        private readonly IStorageAvailabilityService _availabilityService;

        public FileStorageService(IStorageLocationResolver locationResolver, IStorageAvailabilityService availabilityService)
        {
            _locationResolver = locationResolver;
            _availabilityService = availabilityService;
        }

        public async Task<long> SaveAsync(Stream stream, string locationKey, string storedName, CancellationToken cancellationToken)
        {
            string basePath = PrepareForWrite(locationKey);

            //Combina la ruta base con el nombre del archivo para obtener la ruta completa
            string fullPath = Path.Combine(basePath, storedName);

            //Se escribe a un fichero temporal junto al final y se renombra al terminar: un fallo o una cancelación
            //a mitad no deja un fichero truncado con el nombre definitivo.
            string partialPath = fullPath + ".partial";
            long writtenBytes;

            try
            {
                await using (FileStream fileStream = new FileStream(partialPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                {
                    //Copia el contenido del stream al archivo de forma asíncrona
                    await stream.CopyToAsync(fileStream, cancellationToken);
                    writtenBytes = fileStream.Length;
                }

                //Mismo directorio, mismo volumen: el renombrado es atómico
                File.Move(partialPath, fullPath, overwrite: false);
            }
            catch (Exception)
            {
                if (File.Exists(partialPath))
                    File.Delete(partialPath);
                throw;
            }

            return writtenBytes;
        }

        public async Task SaveTempAsync(Stream stream, string tempFilePath, CancellationToken cancellationToken)
        {
            //Se comprueba el disco del área temporal antes de crear ninguna carpeta
            string tempDiskKey = _locationResolver.ResolveDefaultDiskKey(LocationUsage.Temp);
            _availabilityService.EnsureAvailable(tempDiskKey, forWrite: true);

            //Si no existe la carpeta que contiene el fichero, la crea
            Directory.CreateDirectory(Path.GetDirectoryName(tempFilePath)!);

            await using FileStream fileStream = new FileStream(tempFilePath, FileMode.Create);
            await stream.CopyToAsync(fileStream, cancellationToken);
        }

        public Task SaveFromPathAsync(string sourceFilePath, string locationKey, string storedName, CancellationToken cancellationToken)
        {
            string basePath = PrepareForWrite(locationKey);

            //Combina la ruta base con el nombre del archivo para obtener la ruta completa
            string fullPath = Path.Combine(basePath, storedName);

            //Mueve el archivo ya ensamblado a su ubicación final. Si origen y destino están en el mismo volumen
            //es prácticamente instantáneo; si no, .NET hace copy+delete internamente sin lanzar excepción.
            File.Move(sourceFilePath, fullPath, overwrite: true);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(string locationKey, string storedName, CancellationToken cancellationToken)
        {
            //Sin esta comprobación, un disco ausente haría que el fichero "no existiera" y el borrado pasara en
            //silencio, dejando el fichero huérfano mientras el llamador borra su registro en base de datos.
            _availabilityService.EnsureAvailable(_locationResolver.ResolveDiskKey(locationKey), forWrite: true);

            //Combina la ruta base con el nombre del archivo para obtener la ruta completa
            string fullPath = Path.Combine(_locationResolver.ResolveBasePath(locationKey), storedName);

            //Si el archivo existe, lo elimina
            if (File.Exists(fullPath))
                File.Delete(fullPath);

            //Devuelve una tarea completada ya que la operación de eliminación es sincrónica
            return Task.CompletedTask;
        }

        public bool Exists(string locationKey, string storedName)
        {
            //Sin esta comprobación, un disco ausente daría "el archivo no existe" en vez de "ubicación no disponible"
            _availabilityService.EnsureAvailable(_locationResolver.ResolveDiskKey(locationKey), forWrite: false);

            //Combina la ruta base con el nombre del archivo para obtener la ruta completa
            string fullPath = Path.Combine(_locationResolver.ResolveBasePath(locationKey), storedName);

            //Devuelve true si el archivo existe, de lo contrario devuelve false
            return File.Exists(fullPath);
        }

        public string GetFilePath(string locationKey, string storedName)
        {
            _availabilityService.EnsureAvailable(_locationResolver.ResolveDiskKey(locationKey), forWrite: false);

            //Devuelva la ruta completa del archivo combinando la ruta base con el nombre del archivo almacenado
            return Path.Combine(_locationResolver.ResolveBasePath(locationKey), storedName);
        }

        /// <summary>
        /// Comprueba que el disco de la ubicación es escribible y, solo entonces, crea su carpeta si no existe.
        /// Devuelve la ruta base de la ubicación.
        /// </summary>
        private string PrepareForWrite(string locationKey)
        {
            _availabilityService.EnsureAvailable(_locationResolver.ResolveDiskKey(locationKey), forWrite: true);

            string basePath = _locationResolver.ResolveBasePath(locationKey);

            //Si no existe el directorio, lo crea (solo tras confirmar con el marcador que el disco es el correcto)
            Directory.CreateDirectory(basePath);

            return basePath;
        }
    }
}
