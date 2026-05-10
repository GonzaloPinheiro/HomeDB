using HomeDB.Application.DTOs.Files;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces;

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
    }
}
