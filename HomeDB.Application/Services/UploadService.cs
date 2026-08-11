using HomeDB.Application.DTOs;
using HomeDB.Application.DTOs.Files;
using HomeDB.Application.Options;
using HomeDB.Domain.Common;
using HomeDB.Domain.Common.Enums;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Exceptions;
using HomeDB.Domain.Interfaces;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Domain.Interfaces.Services;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

namespace HomeDB.Application.Services
{
    public class UploadService
    {
        //Variables y objetos globales
        private readonly IUploadSessionRepository _uploadSessionRepository;
        private readonly IFolderRepository _folderRepository;
        private readonly IFileItemRepository _fileItemRepository;
        private readonly IUploadChunkRepository _uploadChunkRepository;
        private readonly IUploadChunkLockProvider _lockProvider;
        private readonly IFileStorageService _fileStorageService;
        private readonly IFileTypeValidator _fileTypeValidator;
        private readonly AuditService _auditService;
        private readonly UserAdminSettingsService _userAdminSettingsService;
        private readonly StorageOptions _storageOptions;

        //Constructores
        public UploadService(IUploadSessionRepository uploadSessionRepository, IFolderRepository folderRepository,
                             IFileItemRepository fileItemRepository, IUploadChunkLockProvider lockProvider, IUploadChunkRepository uploadChunkRepository,
                             IFileStorageService fileStorageService, IFileTypeValidator fileTypeValidator, AuditService auditService,
                             UserAdminSettingsService userAdminSettingsService, IOptions<StorageOptions> storageOptions)
        {
            _uploadSessionRepository = uploadSessionRepository;
            _folderRepository = folderRepository;
            _fileItemRepository = fileItemRepository;
            _lockProvider = lockProvider;
            _uploadChunkRepository = uploadChunkRepository;
            _fileStorageService = fileStorageService;
            _fileTypeValidator = fileTypeValidator;
            _auditService = auditService;
            _userAdminSettingsService = userAdminSettingsService;
            _storageOptions = storageOptions.Value;
        }

        /// <summary>
        /// Crea una nueva sesión de carga de archivos y devuelve el ID de la sesión por el DTO.
        /// </summary>
        public async Task<UploadInitResponseDto> InitUploadAsync(UploadInitRequestDto request, int ownerId, CancellationToken cToken)
        {
            //Comprobar que el total de chunks sea válido
            if (request.TotalChunks < 1)
                throw new InvalidChunkNumberException(1, request.TotalChunks);

            //Comprobar que el tamaño total del archivo sea válido
            if (request.TotalSizeBytes <= 0)
                throw new InvalidUploadRequestException($"TotalSizeBytes debe ser mayor que 0. Valor recibido: {request.TotalSizeBytes}.");

            //Comprobar si la carpeta padre existe y pertenece al usuario
            if (request.FolderId.HasValue)
            {
                FolderItem? folder = await _folderRepository.GetByIdAsync(request.FolderId.Value, cToken);
                if (folder is null || folder.OwnerId != ownerId)
                {
                    throw new ParentFolderNotFoundException(request.FolderId.Value);
                }
            }

            //Obtener la configuración de administración del usuario para verificar el límite de tamaño de archivo
            UserAdminSettings effectiveSettings = await _userAdminSettingsService.GetEffectiveSettingsAsync(ownerId, cToken);

            //Comprobar el límite de tamaño de archivo efectivo para el usuario
            if (effectiveSettings.MaxFileSizeBytes.HasValue && request.TotalSizeBytes > effectiveSettings.MaxFileSizeBytes.Value)
                throw new FileTooLargeException(request.TotalSizeBytes, effectiveSettings.MaxFileSizeBytes.Value);

            //Comprobar la cuota de almacenamiento total del usuario (solo si tiene límite configurado)
            if (effectiveSettings.StorageLimitBytes.HasValue)
            {
                (int _, long totalSizeBytes, int _) = await _fileItemRepository.GetUserStatsAsync(ownerId, cToken);
                if (totalSizeBytes + request.TotalSizeBytes > effectiveSettings.StorageLimitBytes.Value)
                    throw new StorageLimitExceededException(totalSizeBytes + request.TotalSizeBytes, effectiveSettings.StorageLimitBytes.Value);
            }

            //Crear una nueva sesión de carga
            UploadSession session = new UploadSession
            {
                FileName = request.FileName,
                TotalSizeBytes = request.TotalSizeBytes,
                MaxFileSizeBytes = effectiveSettings.MaxFileSizeBytes ?? _storageOptions.MaxFileSizeBytes,
                TotalChunks = request.TotalChunks,
                OwnerId = ownerId,
                FolderId = request.FolderId
            };

            //Guardar la sesión en la base de datos
            await _uploadSessionRepository.AddAsync(session, cToken);

            //Persistir los cambios en la base de datos
            await _uploadSessionRepository.SaveChangesAsync(cToken);

            //Devolver el ID de la sesión
            return new UploadInitResponseDto(session.SessionId);
        }

        /// <summary>
        /// Recibe un chunk de archivo y lo guarda temporalmente en el servidor. Se asegura de que la sesión de carga esté activa y que el chunk sea válido.
        /// </summary>
        public async Task ReceiveChunkAsync(UploadChunkRequestDto request, int ownerId, CancellationToken cToken)
        {
            //Obtener la sesión de carga por el ID de sesión
            UploadSession? session = await _uploadSessionRepository.GetBySessionIdAsync(request.SessionId, cToken, asNoTracking: false);

            //Revisar que el session exista y que pertenezca al usuario
            if (session == null || session.OwnerId != ownerId)
                throw new UploadSessionNotFoundException(request.SessionId.ToString());

            //Revisar que la sesión esté activa (no cancelada ni completada)
            if (session.Status == UploadSessionStatus.Cancelled)
                throw new UploadSessionNotActiveException(request.SessionId.ToString());

            //Revisar que la sesión no esté completada, si ya está completada no hace falta reescribir nada
            if (session.Status == UploadSessionStatus.Completed)
                return;

            //Comprobar que el número de chunk sea válido (mayor que 0 y menor o igual al total de chunks)
            if (request.ChunkNumber <= 0 || request.ChunkNumber > session.TotalChunks)
                throw new InvalidChunkNumberException(request.ChunkNumber, session.TotalChunks);

            //Obtener un lock para el chunk específico de la sesión para evitar condiciones de carrera
            SemaphoreSlim chunkLock = _lockProvider.GetLock(session.SessionId, request.ChunkNumber);

            //Esperar a adquirir el lock antes de procesar el chunk
            await chunkLock.WaitAsync(cToken);

            //Obtener la ruta de la carpeta temporal para almacenar los chunks de la sesión
            string sessionFolderPath = Path.Combine(_storageOptions.TempUploadsPath, session.SessionId.ToString());

            //Crear la ruta completa del archivo temporal para el chunk específico
            string chunkPath = Path.Combine(sessionFolderPath, $"chunk_{request.ChunkNumber}");

            //Crear la ruta temporal para el chunk antes de moverlo a su ubicación final
            string chunkTempPath = chunkPath + ".tmp";

            try
            {
                //Comprobar si el chunk ya fue recibido previamente para evitar duplicados
                bool alreadyReceived = await _uploadChunkRepository.ExistsAsync(session.Id, request.ChunkNumber, cToken);

                //Crear la carpeta temporal para almacenar los chunks de la sesión si no existe
                Directory.CreateDirectory(sessionFolderPath);

                //Guardar el chunk en un archivo temporal en el servidor
                await _fileStorageService.SaveAsync(request.ChunkStream, chunkTempPath, cToken);

                //Leer el tamaño del archivo escrito
                long writtenBytes = new FileInfo(chunkTempPath).Length;

                //Verificar que el tamaño del chunk escrito coincida con el tamaño del chunk recibido
                if (writtenBytes != request.ChunkStream.Length)
                    throw new InvalidChunkSizeException(request.ChunkNumber, request.ChunkStream.Length, writtenBytes);

                //Mover el archivo temporal a la ubicación final del chunk, sobrescribiendo si ya existe
                File.Move(chunkTempPath, chunkPath, overwrite: true);

                //Si el chunk no fue recibido previamente, registrar la recepción del chunk en la base de datos
                if (!alreadyReceived)
                {
                    //Crear un nuevo registro de UploadChunk para la base de datos
                    UploadChunk chunk = new UploadChunk
                    {
                        UploadSessionId = session.Id,
                        ChunkNumber = request.ChunkNumber,
                        SizeBytes = writtenBytes,
                        ReceivedAt = DateTimeOffset.UtcNow
                    };

                    //Guardar el registro del chunk en la base de datos
                    await _uploadChunkRepository.AddAsync(chunk, cToken);

                    //Persistir los cambios en la base de datos
                    await _uploadChunkRepository.SaveChangesAsync(cToken);

                    //Actualizar el tamaño total del archivo
                    bool withinLimit = await _uploadSessionRepository
                        .TryIncrementReceivedSizeBytesAsync(session.Id, writtenBytes, session.MaxFileSizeBytes, cToken);

                    //Comprobar que fue posible aumentarlo.(Si supera el límite máximo es false)
                    if (!withinLimit)
                    {
                        session.Status = UploadSessionStatus.Cancelled;
                        await _uploadSessionRepository.SaveChangesAsync(cToken);
                        throw new FileTooLargeException(session.ReceivedSizeBytes + writtenBytes, session.MaxFileSizeBytes);
                    }

                    //Guardar registro de última actividad
                    session.LastActivityAt = DateTimeOffset.UtcNow;
                    await _uploadSessionRepository.SaveChangesAsync(cToken);
                }
            }
            catch (Exception)
            {
                //Borrar el archivo temporal del chunk si existe
                if (File.Exists(chunkTempPath))
                {
                    File.Delete(chunkTempPath);
                }

                //Re-lanzar la excepción para que sea manejada por el llamador
                throw;
            }
            finally
            {
                chunkLock.Release();
            }
        }

        /// <summary>
        /// Obtiene el estado de la sesión de carga, incluyendo los chunks recibidos hasta el momento.
        /// </summary>
        public async Task<UploadStatusResponseDto> GetUploadStatusAsync(Guid sessionId, int ownerId, CancellationToken cToken)
        {
            //Obtener la sesión de carga por el ID de sesión
            UploadSession? session = await _uploadSessionRepository.GetBySessionIdAsync(sessionId, cToken);

            //Revisar que el session exista y que pertenezca al usuario
            if (session == null || session.OwnerId != ownerId)
                throw new UploadSessionNotFoundException(sessionId.ToString());

            //Obtener la lista de números de chunks recibidos hasta el momento para la sesión
            List<int> receivedChunks = await _uploadChunkRepository.GetReceivedChunkNumbersAsync(session.Id, cToken);

            //Devolver el estado de la sesión de carga con el ID de sesión, total de chunks y los chunks recibidos
            return new UploadStatusResponseDto(session.SessionId, session.TotalChunks, receivedChunks);
        }

        /// <summary>
        /// Completa la sesión de carga ensamblando los chunks recibidos en un único archivo y guardándolo en su ubicación final. Cambia el estado de la sesión a "Completed" y elimina los archivos temporales.
        /// </summary>
        public async Task<UploadFileResponseDto?> CompleteUploadAsync(Guid sessionId, int ownerId, CancellationToken cToken)
        {
            //Obtener la sesión de subida
            UploadSession? session = await _uploadSessionRepository.GetBySessionIdAsync(sessionId, cToken, asNoTracking: false);

            //Revisar que el session exista y que pertenezca al usuario
            if (session == null || session.OwnerId != ownerId)
                throw new UploadSessionNotFoundException(sessionId.ToString());

            //Revisar que la sesión esté activa (no cancelada ni completada)
            if (session.Status == UploadSessionStatus.Cancelled)
                throw new UploadSessionNotActiveException(sessionId.ToString());

            //TODO darle una vuelta 
            if (session.Status == UploadSessionStatus.Completed)
                return null;

            //Obtener la lista de chunks recibidos
            List<int> receivedChunks = await _uploadChunkRepository.GetReceivedChunkNumbersAsync(session.Id, cToken);

            //Comprobar que llegaron todos los chunks
            bool allChunksRecived = Enumerable.Range(1, session.TotalChunks)
                .All(n => receivedChunks.Contains(n));

            //Verificar que llegaron todos los chunks
            if (!allChunksRecived)
                throw new IncompleteUploadException(session.SessionId, receivedChunks.Count, session.TotalChunks);

            //Crear la ruta de la carpeta temporal para almacenar los chunks de la sesión
            string sessionFolderPath = Path.Combine(_storageOptions.TempUploadsPath, session.SessionId.ToString());

            //Crear la ruta completa del archivo ensamblado temporal
            string assembledTempPath = Path.Combine(sessionFolderPath, "assembled.tmp");

            long assembledSizeBytes;
            string extension;

            try
            {
                //Concatenar los chunks en orden numérico hacia un único archivo temporal
                await using (FileStream outputStream = new FileStream(assembledTempPath, FileMode.Create, FileAccess.Write))
                {
                    //Iterar sobre los chunks en orden y copiarlos al archivo ensamblado
                    for (int chunkNumber = 1; chunkNumber <= session.TotalChunks; chunkNumber++)
                    {
                        //Crear la ruta completa del chunk específico
                        string chunkPath = Path.Combine(sessionFolderPath, $"chunk_{chunkNumber}");

                        //Verificar que el chunk exista antes de intentar leerlo
                        await using (FileStream chunkStream = new FileStream(chunkPath, FileMode.Open, FileAccess.Read))
                        {
                            await chunkStream.CopyToAsync(outputStream, cToken);
                        }
                    }
                }

                //Verificar que el tamaño del archivo ensamblado coincida con el tamaño total esperado
                assembledSizeBytes = new FileInfo(assembledTempPath).Length;

                //Si el tamaño del archivo ensamblado no coincide con el tamaño total esperado, lanzar una excepción
                if (assembledSizeBytes != session.TotalSizeBytes)
                    throw new AssembledFileSizeMismatchException(session.SessionId, session.TotalSizeBytes, assembledSizeBytes);

                //Validar que la extensión del archivo está en la whitelist
                extension = Path.GetExtension(session.FileName);
                if (!AllowedExtensions.Whitelist.Contains(extension))
                    throw new InvalidFileExtensionException(extension);

                //Leer los bytes iniciales del archivo ensamblado para validar el tipo real
                byte[] headerBytes = new byte[16];
                await using (FileStream finalStream = new FileStream(assembledTempPath, FileMode.Open, FileAccess.Read))
                {
                    await finalStream.ReadAsync(headerBytes, cToken);
                }

                //Validar que el contenido del archivo coincide con la extensión declarada
                if (!_fileTypeValidator.IsValid(session.FileName, headerBytes))
                    throw new InvalidFileExtensionException(extension);
            }
            catch (Exception)
            {
                //El ensamblado o su validación posterior fallaron.
                //Cancelar la sesión y limpiar los artefactos temporales para no dejar huérfanos.
                session.Status = UploadSessionStatus.Cancelled;
                await _uploadSessionRepository.SaveChangesAsync(CancellationToken.None);

                //Borrar el directorio
                if (Directory.Exists(sessionFolderPath))
                    Directory.Delete(sessionFolderPath, recursive: true);

                throw;
            }

            //Generar un nombre único para el archivo final a partir de un GUID y la extensión original del archivo
            string storedName = Guid.NewGuid().ToString() + extension;

            try
            {
                //Guardar el archivo ensamblado en su ubicación final
                await using (FileStream assembledReadStream = new FileStream(assembledTempPath, FileMode.Open, FileAccess.Read))
                {
                    await _fileStorageService.SaveAsync(assembledReadStream, storedName, cToken);
                }

                FileExtensionContentTypeProvider contentTypeProvider = new FileExtensionContentTypeProvider();
                if (!contentTypeProvider.TryGetContentType(session.FileName, out string? contentType))
                    contentType = "application/octet-stream"; // fallback si la extensión no está en su diccionario interno

                //Crear un nuevo FileItem para la base de datos con la información del archivo subido
                FileItem fileItem = new FileItem
                {
                    FileName = session.FileName,
                    StoredName = storedName,
                    SizeBytes = assembledSizeBytes,
                    ContentType = contentType!,
                    FolderId = session.FolderId,
                    OwnerId = ownerId,
                    UploadedAt = DateTime.UtcNow
                };

                //Guardar el FileItem en la base de datos
                await _fileItemRepository.AddAsync(fileItem, cToken);

                //Marcar la sesión como completada
                session.Status = UploadSessionStatus.Completed;

                //Persistir los cambios en la base de datos
                await _fileItemRepository.SaveChangesAsync(cToken);

                //Registrar la acción de subida de archivo en el log de auditoría
                await _auditService.LogAsync(AuditLogActions.UploadFile, nameof(FileItem), fileItem.Id, fileItem.FileName, cToken);

                //Eliminar la carpeta temporal de la sesión y todos sus chunks
                Directory.Delete(sessionFolderPath, recursive: true);

                //Devolver la información del archivo subido en un DTO de respuesta
                return new UploadFileResponseDto(
                    fileItem.Id,
                    fileItem.FileName,
                    fileItem.SizeBytes,
                    fileItem.ContentType,
                    fileItem.FolderId,
                    fileItem.OwnerId,
                    fileItem.UploadedAt
                );
            }
            catch (Exception)
            {
                //Si algo falla tras guardar en disco, eliminar el archivo final para evitar huérfanos.
                await _fileStorageService.DeleteAsync(storedName, CancellationToken.None);
                throw;
            }
        }

        /// <summary>
        /// Elimina las sesiones de carga que han sido completadas o canceladas, junto con sus archivos temporales.
        /// </summary>
        public async Task CleanupFinishedSessionsAsync(CancellationToken cToken)
        {
            //Obtener todas las sesiones de carga que han sido completadas o canceladas
            List<UploadSession> finishedSessions = await _uploadSessionRepository.GetFinishedSessionsAsync(cToken);

            //Si no hay sesiones finalizadas, no hacer nada
            if (finishedSessions.Count == 0)
                return;

            //Lista para almacenar las sesiones que se eliminarán de la base de datos
            List<UploadSession> sessionsToRemove = new List<UploadSession>();

            //Iterar sobre cada sesión finalizada para eliminar sus archivos temporales y marcarla para eliminación de la base de datos
            foreach (UploadSession session in finishedSessions)
            {
                //Crear la ruta de la carpeta temporal para la sesión específica
                string sessionFolderPath = Path.Combine(_storageOptions.TempUploadsPath, session.SessionId.ToString());

                try
                {
                    //Puede no existir ya (complete() borra su propia carpeta al terminar con éxito)
                    if (Directory.Exists(sessionFolderPath))
                        Directory.Delete(sessionFolderPath, recursive: true);

                    sessionsToRemove.Add(session);
                }
                catch (Exception ex)
                {
                    //TODO Cambiarlo por un log normal
                    Console.WriteLine($"[UploadCleanupService] No se pudo borrar la carpeta de la sesión {session.SessionId}: {ex.Message}");
                    //No se añade a sessionsToRemove: se reintenta la próxima noche, no se pierde el registro
                }
            }

            _uploadSessionRepository.RemoveRange(sessionsToRemove);
            await _uploadSessionRepository.SaveChangesAsync(cToken);
        }
    }
}