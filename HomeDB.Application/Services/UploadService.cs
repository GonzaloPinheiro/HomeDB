using HomeDB.Application.DTOs;
using HomeDB.Application.DTOs.Files;
using HomeDB.Application.Options;
using HomeDB.Domain.Common;
using HomeDB.Domain.Common.Enums;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Exceptions;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Domain.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace HomeDB.Application.Services
{
    public class UploadService
    {
        //Número de chunk reservado para el lock de finalización de sesión.
        private const int CompletionLockChunkNumber = 0;

        //Variables y objetos globales
        private readonly IUploadSessionRepository _uploadSessionRepository;
        private readonly IFolderRepository _folderRepository;
        private readonly IFileItemRepository _fileItemRepository;
        private readonly IUploadChunkRepository _uploadChunkRepository;
        private readonly IUploadChunkLockProvider _lockProvider;
        private readonly IFileStorageService _fileStorageService;
        private readonly IFileTypeValidator _fileTypeValidator;
        private readonly IContentTypeResolver _contentTypeResolver;
        private readonly AuditService _auditService;
        private readonly UserAdminSettingsService _userAdminSettingsService;
        private readonly IUploadCompletionQueue _completionQueue;
        private readonly StorageOptions _storageOptions;

        //Constructores
        public UploadService(IUploadSessionRepository uploadSessionRepository, IFolderRepository folderRepository,
                             IFileItemRepository fileItemRepository, IUploadChunkLockProvider lockProvider, IUploadChunkRepository uploadChunkRepository,
                             IFileStorageService fileStorageService, IFileTypeValidator fileTypeValidator, IContentTypeResolver contentTypeResolver,
                             AuditService auditService, UserAdminSettingsService userAdminSettingsService, IUploadCompletionQueue completionQueue,
                             IOptions<StorageOptions> storageOptions)
        {
            _uploadSessionRepository = uploadSessionRepository;
            _folderRepository = folderRepository;
            _fileItemRepository = fileItemRepository;
            _lockProvider = lockProvider;
            _uploadChunkRepository = uploadChunkRepository;
            _fileStorageService = fileStorageService;
            _fileTypeValidator = fileTypeValidator;
            _contentTypeResolver = contentTypeResolver;
            _auditService = auditService;
            _userAdminSettingsService = userAdminSettingsService;
            _completionQueue = completionQueue;
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

            //Revisar que la sesión no esté completada ni en proceso: un chunk tardío no debe pisar archivos
            //que el worker de finalización ya puede estar leyendo para ensamblar.
            if (session.Status != UploadSessionStatus.InProgress)
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

                        //Estado terminal ya persistido: liberar los locks de la sesión para no acumularlos indefinidamente.
                        _lockProvider.ReleaseSessionLocks(session.SessionId);

                        throw new FileTooLargeException(session.ReceivedSizeBytes + writtenBytes, session.MaxFileSizeBytes);
                    }
                }

                //Guardar registro de última actividad
                session.LastActivityAt = DateTimeOffset.UtcNow;
                await _uploadSessionRepository.SaveChangesAsync(cToken);
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

            //Si la sesión ya se completó, incluir los datos del archivo resultante para que el cliente no tenga
            //que llamar a otro endpoint para saber qué se creó.
            UploadFileResponseDto? file = null;
            if (session.Status == UploadSessionStatus.Completed && session.ResultFileItemId.HasValue)
            {
                FileItem? fileItem = await _fileItemRepository.GetByIdAsync(session.ResultFileItemId.Value, cToken);
                if (fileItem != null)
                {
                    file = new UploadFileResponseDto(fileItem.Id, fileItem.FileName, fileItem.SizeBytes,
                        fileItem.ContentType, fileItem.FolderId, fileItem.OwnerId, fileItem.UploadedAt);
                }
            }

            //Devolver el estado de la sesión de carga con el ID de sesión, total de chunks, los chunks recibidos,
            //el estado actual y, según corresponda, el archivo resultante o el motivo del error.
            return new UploadStatusResponseDto(session.SessionId, session.TotalChunks, receivedChunks,
                session.Status, file, session.ErrorCode);
        }

        /// <summary>
        /// Valida que la sesión esté lista para finalizar (todos los chunks recibidos) y encola el ensamblado pesado
        /// para que se procese en background. Devuelve de inmediato: no ensambla nada en esta llamada.
        /// </summary>
        public async Task RequestCompletionAsync(Guid sessionId, int ownerId, CancellationToken cToken)
        {
            //Obtener la sesión de subida
            UploadSession? session = await _uploadSessionRepository.GetBySessionIdAsync(sessionId, cToken, asNoTracking: false);

            //Revisar que el session exista y que pertenezca al usuario
            if (session == null || session.OwnerId != ownerId)
                throw new UploadSessionNotFoundException(sessionId.ToString());

            //Revisar que la sesión esté activa (no cancelada)
            if (session.Status == UploadSessionStatus.Cancelled)
                throw new UploadSessionNotActiveException(sessionId.ToString());

            //Llamada repetida a /complete mientras ya está completada o en proceso: no hay nada más que hacer,
            //el cliente debe consultar el estado con GetUploadStatusAsync.
            if (session.Status == UploadSessionStatus.Completed || session.Status == UploadSessionStatus.Processing)
                return;

            //Lock de finalización a nivel de sesión (chunk reservado 0): evita que dos llamadas a complete concurrentes
            //encolen la misma sesión dos veces. No compite con los locks por chunk de ReceiveChunkAsync.
            SemaphoreSlim completionLock = _lockProvider.GetLock(session.SessionId, CompletionLockChunkNumber);
            await completionLock.WaitAsync(cToken);

            try
            {
                //Releer el estado con el lock ya tomado.
                session = await _uploadSessionRepository.GetBySessionIdAsync(sessionId, cToken, asNoTracking: false);
                if (session == null)
                    throw new UploadSessionNotFoundException(sessionId.ToString());

                //Comprobar si la sesión sigue activa.
                if (session.Status == UploadSessionStatus.Cancelled)
                    throw new UploadSessionNotActiveException(sessionId.ToString());

                if (session.Status == UploadSessionStatus.Completed || session.Status == UploadSessionStatus.Processing)
                    return;

                //Obtener la lista de chunks recibidos
                List<int> receivedChunks = await _uploadChunkRepository.GetReceivedChunkNumbersAsync(session.Id, cToken);

                //Comprobar que llegaron todos los chunks
                bool allChunksRecived = Enumerable.Range(1, session.TotalChunks)
                    .All(n => receivedChunks.Contains(n));

                //Verificar que llegaron todos los chunks
                if (!allChunksRecived)
                    throw new IncompleteUploadException(session.SessionId, receivedChunks.Count, session.TotalChunks);

                //Marcar la sesión como "en proceso": a partir de aquí el ensamblado real ocurre en background,
                //y ninguna otra llamada a /complete puede volver a encolarla (cae en el return de arriba).
                session.Status = UploadSessionStatus.Processing;
                await _uploadSessionRepository.SaveChangesAsync(cToken);

                //Encolar el ensamblado pesado para que lo procese el worker, fuera de esta request.
                await _completionQueue.QueueCompletionAsync(session.SessionId, ownerId);
            }
            finally
            {
                completionLock.Release();
            }
        }

        /// <summary>
        /// Ensambla los chunks recibidos en un único archivo y lo guarda en su ubicación final. Llamado únicamente
        /// por el worker en background (nunca directamente desde un controller) una vez que RequestCompletionAsync
        /// ya marcó la sesión como "Processing". Cambia el estado final de la sesión a "Completed" o "Cancelled".
        /// </summary>
        public async Task ProcessCompletionAsync(Guid sessionId, int ownerId, CancellationToken cToken)
        {
            //Obtener la sesión de subida; RequestCompletionAsync ya garantizó que existe y está en Processing.
            UploadSession? session = await _uploadSessionRepository.GetBySessionIdAsync(sessionId, cToken, asNoTracking: false);
            if (session == null)
                throw new UploadSessionNotFoundException(sessionId.ToString());

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
            catch (Exception ex)
            {
                //El ensamblado o su validación posterior fallaron.
                //Cancelar la sesión, registrar el motivo y limpiar los artefactos temporales para no dejar huérfanos.
                session.Status = UploadSessionStatus.Cancelled;
                session.ErrorCode = MapExceptionToErrorCode(ex);
                await _uploadSessionRepository.SaveChangesAsync(CancellationToken.None);

                //Estado terminal ya persistido: liberar los locks de la sesión para no acumularlos en memoria.
                _lockProvider.ReleaseSessionLocks(session.SessionId);

                //Borrar el directorio
                if (Directory.Exists(sessionFolderPath))
                    Directory.Delete(sessionFolderPath, recursive: true);

                throw;
            }

            //Generar un nombre único para el archivo final a partir de un GUID y la extensión original del archivo
            string storedName = Guid.NewGuid().ToString() + extension;

            //Objeto a insertar en base de datos
            FileItem fileItem;

            try
            {
                //Mover el archivo ya ensamblado a su ubicación final, sin volver a copiarlo entero.
                await _fileStorageService.SaveFromPathAsync(assembledTempPath, storedName, cToken);

                if (!_contentTypeResolver.TryGetContentType(session.FileName, out string? contentType))
                    contentType = "application/octet-stream"; // fallback si la extensión no está en su diccionario interno

                //Crear un nuevo FileItem para la base de datos con la información del archivo subido
                fileItem = new FileItem
                {
                    FileName = session.FileName,
                    StoredName = storedName,
                    SizeBytes = assembledSizeBytes,
                    ContentType = contentType!,
                    FolderId = session.FolderId,
                    OwnerId = ownerId,
                    UploadedAt = DateTime.UtcNow
                };

                //Guardar el FileItem en la base de datos. Se persiste antes de tocar la sesión porque recién
                //después de este SaveChanges existe fileItem.Id (identity) para guardarlo en ResultFileItemId.
                await _fileItemRepository.AddAsync(fileItem, cToken);
                await _fileItemRepository.SaveChangesAsync(cToken);
            }
            catch (Exception)
            {
                //Si algo falla antes de comprometer el FileItem en la base de datos, eliminar el archivo final para evitar huérfanos.
                //Nota: la sesión sigue en Processing en la base de datos, así que no se liberan los locks:
                //el cliente puede reintentar el complete con los mismos chunks.
                if (_fileStorageService.Exists(storedName))
                    await _fileStorageService.DeleteAsync(storedName, CancellationToken.None);
                throw;
            }

            //Marcar la sesión como completada y guardar el archivo resultante.
            session.Status = UploadSessionStatus.Completed;
            session.ResultFileItemId = fileItem.Id;
            await _uploadSessionRepository.SaveChangesAsync(cToken);

            //Registrar la acción de subida de archivo en el log de auditoría (sin ICurrentUserService: no hay request).
            await _auditService.LogAsync(AuditLogActions.UploadFile, ownerId, nameof(FileItem), fileItem.Id, fileItem.FileName, cToken);

            //Eliminar la carpeta temporal de la sesión y todos sus chunks
            if (Directory.Exists(sessionFolderPath))
                Directory.Delete(sessionFolderPath, recursive: true);

            //Estado terminal ya persistido: liberar los locks de la sesión para no acumularlos indefinidamente.
            _lockProvider.ReleaseSessionLocks(session.SessionId);
        }

        /// <summary>
        /// Mapea una excepción de ensamblado/validación al código de error que debe quedar en UploadSession.ErrorCode,
        /// para que el cliente que hace polling a /status sepa por qué falló sin exponer el mensaje interno.
        /// </summary>
        private static ApiErrorCodes MapExceptionToErrorCode(Exception ex) => ex switch
        {
            AssembledFileSizeMismatchException => ApiErrorCodes.AssembledFileSizeMismatch,
            InvalidFileExtensionException => ApiErrorCodes.InvalidFileExtension,
            _ => ApiErrorCodes.InternalError
        };

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

                //Red de seguridad: la sesión ya está en estado terminal, así que sus locks ya no deberían usarse.
                _lockProvider.ReleaseSessionLocks(session.SessionId);

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