using HomeDB.Application.DTOs.Files;
using HomeDB.Domain.Common;
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
        private readonly IFolderRepository _folderRepository;
        private readonly AuditService _auditService;
        private readonly UserAdminSettingsService _userAdminSettingsService;

        public FilesService(IFileStorageService fileStorageService, IFileItemRepository fileItemRepository, IFolderRepository folderRepository, AuditService auditService, UserAdminSettingsService userAdminSettingsService)
        {
            _fileStorageService = fileStorageService;
            _fileItemRepository = fileItemRepository;
            _folderRepository = folderRepository;
            _auditService = auditService;
            _userAdminSettingsService = userAdminSettingsService;
        }

        /// <summary>
        /// Guarda un archivo en el disco del servidor y crea un registro en la base de datos con su metadata. Devuelve un DTO con la información del archivo subido.
        /// </summary>
        public async Task<UploadFileResponseDto> UploadFileAsync(UploadFileRequestDto request, int ownerId, CancellationToken cToken)
        {
            //Generar el guid único para el archivo con el mismo formato de extensión del archivo original
            string extension = Path.GetExtension(request.FileName);
            string storedName = Guid.NewGuid().ToString() + extension;

            //Obtener la configuración de administración del usuario para verificar el límite de tamaño de archivo
            UserAdminSettings effectiveSettings = await _userAdminSettingsService.GetEffectiveSettingsAsync(ownerId, cToken);

            //Comprobar el límite de tamaño de archivo efectivo para el usuario
            if (effectiveSettings.MaxFileSizeBytes.HasValue && request.SizeBytes > effectiveSettings.MaxFileSizeBytes.Value)
                throw new FileTooLargeException(request.SizeBytes, effectiveSettings.MaxFileSizeBytes.Value);

            //Comprobar la cuota de almacenamiento total del usuario (solo si tiene límite configurado)
            if (effectiveSettings.StorageLimitBytes.HasValue)
            {
                (int _, long totalSizeBytes, int _) = await _fileItemRepository.GetUserStatsAsync(ownerId, cToken);
                if (totalSizeBytes + request.SizeBytes > effectiveSettings.StorageLimitBytes.Value)
                    throw new StorageLimitExceededException(totalSizeBytes + request.SizeBytes, effectiveSettings.StorageLimitBytes.Value);
            }

            //Si se especificó un FolderId, verificar que exista y que pertenezca al usuario que sube el archivo
            if (request.FolderId.HasValue)
            {
                //Buscar el folder indicado por su id en la base de datos
                FolderItem? folder = await _folderRepository.GetByIdAsync(request.FolderId.Value, cToken);

                //Si no existe el folder o no pertenece al usuario, lanzar una excepción
                if (folder == null || folder.OwnerId != ownerId)
                    throw new ParentFolderNotFoundException(request.FolderId.Value);
            }

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

                //AuditLog
                await _auditService.LogAsync(AuditLogActions.UploadFile, nameof(FileItem), fileItem.Id, fileItem.FileName, cToken);

                //Todo Ok
                return response;
            }
            catch (Exception)
            {
                //Si algo falla, eliminar el archivo del disco para evitar archivos huérfanos
                await _fileStorageService.DeleteAsync(storedName, CancellationToken.None);
                throw;
            }

        }

        /// <summary>
        /// Descarga un archivo del disco del servidor verificando que el usuario tenga permiso para acceder a él. 
        /// Devuelve un DTO con la ruta del archivo en el disco, su nombre original y su tipo de contenido.
        /// </summary>
        public async Task<DownloadFileResponseDto> DownloadFileAsync(int fileId, int userId, CancellationToken cToken)
        {
            // Buscar el FileItem por su Id
            FileItem? fileItem = await _fileItemRepository.GetByIdAsync(fileId, cToken);

            //Si no existe el archivo, lanzar una excepción
            if (fileItem == null)
                throw new FileItemNotFoundException(fileId);

            //Verificar que el archivo pertenece al usuario que lo solicita.
            if (fileItem.OwnerId != userId)
                throw new FileItemNotFoundException(fileId);

            //Verificar que el archivo existe en el disco.
            if (!_fileStorageService.Exists(fileItem.StoredName))
                throw new InvalidOperationException($"FileItem {fileId} registrado en BD pero el archivo físico no existe."); //TODO Crear una excepción personalizada para este caso, que borre el registro de la base de datos?.

            //Obtener la ruta del archivo en el disco usando el StoredName
            string filePath = _fileStorageService.GetFilePath(fileItem.StoredName);

            //AuditLog
            await _auditService.LogAsync(AuditLogActions.DownloadFile, nameof(FileItem), fileItem.Id, fileItem.FileName, cToken);

            //Todo Ok
            return new DownloadFileResponseDto(filePath, fileItem.FileName, fileItem.ContentType);
        }

        /// <summary>
        /// Elimina un archivo del disco del servidor y su registro en la base de datos.
        /// </summary>
        public async Task<DeleteFileResponseDto> DeleteFileAsync(int fileId, int userId, CancellationToken cToken)
        {
            //Buscar si existe el archivo recibido por su id en DB
            FileItem? fileItem = await _fileItemRepository.GetByIdAsync(fileId, cToken, false);

            //Si no existe el archivo, lanzar excepción
            if (fileItem == null)
                throw new FileItemNotFoundException(fileId);

            //Verificar que el archivo pertenece al usuario que lo solicita.
            if (fileItem.OwnerId != userId)
                throw new FileItemNotFoundException(fileId);

            //Eliminar el archivo del disco usando el StoredName
            await _fileStorageService.DeleteAsync(fileItem.StoredName, cToken);

            //Eliminar el registro del archivo en la base de datos
            _fileItemRepository.DeleteFile(fileItem);

            //Persistir los cambios en la base de datos
            await _fileItemRepository.SaveChangesAsync(cToken);

            //AuditLog
            await _auditService.LogAsync(AuditLogActions.DeleteFile, nameof(FileItem), fileItem.Id, fileItem.FileName, cToken);

            //Todo Ok
            return new DeleteFileResponseDto(fileId, fileItem.FileName);
        }

        /// <summary>
        /// Obtiene una lista de archivos del disco del servidor verificando que el usuario tenga permiso para acceder a ellos.
        /// Si se especifica el folderId, solo se listarán los archivos que pertenecen a esa carpeta. Si no se especifica, se listarán todos los archivos de la carpeta raiz.
        /// </summary>
        public async Task<IEnumerable<GetFileItemDto>> ListFilesAsync(int ownerId, int? folderId, CancellationToken cToken)
        {
            //Obtener la lista de archivos del usuario y carpeta especificados
            IEnumerable<FileItem> files = await _fileItemRepository.GetByOwnerAndFolderAsync(ownerId, folderId, cToken);

            //Mapear la lista de FileItem a GetFileItemDto
            return files.Select(f => new GetFileItemDto(
                f.Id,
                f.FileName,
                f.SizeBytes,
                f.ContentType,
                f.FolderId,
                f.UploadedAt
            ));
        }
    }
}
