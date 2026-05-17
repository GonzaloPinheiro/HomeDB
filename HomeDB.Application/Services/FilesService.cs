using HomeDB.Application.DTOs.Files;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Exceptions;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Domain.Interfaces.Services;

namespace HomeDB.Application.Services
{
    public class FilesService
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IFileItemRepository _fileItemRepository;

        public FilesService(IFileStorageService fileStorageService, IFileItemRepository fileItemRepository)
        {
            _fileStorageService = fileStorageService;
            _fileItemRepository = fileItemRepository;
        }

        public async Task<UploadFileResponseDto> UploadFileAsync(UploadFileRequestDto request,
                                                                    int ownerId,
                                                                    CancellationToken cToken)
        {
            //Generar el guid único para el archivo con el mismo formato de extensión del archivo original
            string extension = Path.GetExtension(request.FileName); // → ".jpg"
            string storedName = Guid.NewGuid().ToString() + extension; // → "a1b2c3d4-....jpg"

            try
            {
                //Guardar el archivo en el disco del servicor
                await _fileStorageService.SaveAsync(request.FileStream, storedName, cToken);

                //Crear el registro en la base de datos
                FileItem fileItem = new FileItem
                {
                    FileName = request.FileName,
                    StoredName = storedName,
                    SizeBytes = request.SizeBytes,
                    ContentType = request.ContentType,
                    FolderId = request.FolderId,
                    OwnerId = ownerId,
                    UploadedAt = DateTime.UtcNow
                };

                //Guardar el registro en la base de datos
                await _fileItemRepository.AddAsync(fileItem, cToken);

                //Guardar los cambios en la base de datos del registro del objeto FileItem
                await _fileItemRepository.SaveChangesAsync(cToken);

                //Mapear el FileItem a UploadFileResponseDto
                UploadFileResponseDto response = new UploadFileResponseDto(
                    fileItem.Id,
                    fileItem.FileName,
                    fileItem.SizeBytes,
                    fileItem.ContentType,
                    fileItem.FolderId,
                    fileItem.OwnerId,
                    fileItem.UploadedAt
                );

                //Todo Ok
                return response;
            }
            catch (Exception)
            {
                //Si algo falla, eliminar el archivo del disco para evitar archivos huérfanos
                await _fileStorageService.DeleteAsync(storedName, cToken);
                throw;
            }

        }

        public async Task<DownloadFileResponseDto> DownloadFileAsync(int fileId,int userId, CancellationToken cToken)
        {
            // Buscar el FileItem por su Id
            FileItem? fileItem = await _fileItemRepository.GetByIdAsync(fileId, cToken);

            //Si no existe el archivo, lanzar una excepción
            if (fileItem == null)
                throw new FileNotFoundException("Archivo no encontrado en la base de datos.");

            //Verificar que el archivo pertenece al usuario que lo solicita.
            if(fileItem.OwnerId != userId)
                throw new UnauthorizedAccessException("No tienes permiso para acceder a este archivo.");

            //Verificar que el archivo existe en el disco.
            if (!_fileStorageService.Exists(fileItem.StoredName))
                throw new FileNotFoundException("Archivo no encontrado en el almacenamiento.");

            //Obtener la ruta del archivo en el disco usando el StoredName
            string filePath = _fileStorageService.GetFilePath(fileItem.StoredName);

            //Todo Ok
            return new DownloadFileResponseDto(filePath, fileItem.FileName, fileItem.ContentType);
        }

        public async Task<DeleteFileResponseDto> DeleteFileAsync(int fileId, int userId, CancellationToken cToken)
        {
            //Buscar si existe el archivo recibido por su id en DB
            FileItem? fileItem = await _fileItemRepository.GetByIdAsync(fileId, cToken);

            //Si no existe el archivo, lanzar excepción
            if(fileItem == null)
                throw new FileItemNotFoundException(fileId);

            //Verificar que el archivo pertenece al usuario que lo solicita.
            if (fileItem.OwnerId != userId)
                throw new UnauthorizedException(userId);

            //Eliminar el archivo del disco usando el StoredName
            await _fileStorageService.DeleteAsync(fileItem.StoredName, cToken);

            //Eliminar el registro del archivo en la base de datos
            _fileItemRepository.DeleteFile(fileItem);

            //Persistir los cambios en la base de datos
            await _fileItemRepository.SaveChangesAsync(cToken);

            //Todo Ok
            return new DeleteFileResponseDto(fileId, fileItem.FileName);
        }
    }
}
