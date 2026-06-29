using HomeDB.Application.DTOs;
using HomeDB.Domain.Common;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Exceptions;
using HomeDB.Domain.Interfaces.Repositories;

namespace HomeDB.Application.Services
{
    public class FoldersService
    {
        //Variables y objetos globales
        private readonly IFolderRepository _folderRepository;
        private readonly AuditService _auditService;

        //Constructores
        public FoldersService(IFolderRepository folderRepository, AuditService auditService)
        {
            _folderRepository = folderRepository;
            _auditService = auditService;
        }

        //Crea una carpeta para un usuario específico.
        public async Task<CreateFolderResponseDto> CreateFolderAsync(CreateFolderRequestDto request, int ownerId, CancellationToken cToken)
        {
            //Verifica si el parent folder existe y pertenece al usuario
            if (request.ParentFolderId.HasValue) //No es null(carpeta raiz), entonces verifica
            {
                //Obtiene el folder padre
                FolderItem? parentFolder = await _folderRepository.GetByIdAsync(request.ParentFolderId.Value, cToken);

                //Comprueba si el foler padre existe y pertenece al usuario
                if (parentFolder == null || parentFolder.OwnerId != ownerId)
                    throw new ParentFolderNotFoundException(request.ParentFolderId.Value);

            }

            //Crear objeto FolderItem a partir del request
            FolderItem newFolder = new FolderItem
            {
                Name = request.Name,
                ParentFolderId = request.ParentFolderId,
                OwnerId = ownerId,
                CreatedAt = DateTime.UtcNow
            };

            //Crear el nuevo folder en la base de datos
            await _folderRepository.CreateAsync(newFolder, cToken);

            //Persistir los cambios en la base de datos
            await _folderRepository.SaveChangesAsync(cToken);

            //AuditLog
            await _auditService.LogAsync(AuditLogActions.CreateFolder, nameof(FolderItem), newFolder.Id, newFolder.Name, cToken);

            //Retornar el nuevo folder creado como respuesta
            return new CreateFolderResponseDto
            (
                newFolder.Id,
                newFolder.Name,
                newFolder.ParentFolderId,
                newFolder.OwnerId,
                newFolder.CreatedAt
            );
        }

        //Obtiene las subcarpetas de una carpeta específica de un usuario.
        public async Task<IEnumerable<GetFolderResponseDto>> GetFoldersAsync(int ownerId, int? parentFolderId, CancellationToken cToken)
        {
            //Comprobar que el parten folderId es válido y pertenece al usuario
            if (parentFolderId.HasValue)
            {
                //Obtener el folder padre
                FolderItem? parentFolder = await _folderRepository.GetByIdAsync(parentFolderId.Value, cToken);

                //Comprobar si el folder padre existe y pertenece al usuario
                if (parentFolder == null || parentFolder.OwnerId != ownerId)
                    throw new ParentFolderNotFoundException(parentFolderId.Value);
            }

            //Obtener las subcarpetas
            IEnumerable<FolderItem> folders = await _folderRepository.GetByParentAsync(ownerId, parentFolderId, cToken);

            //Devolver las subcarpetas como respuesta
            return folders.Select(f => new GetFolderResponseDto(f.Id, f.Name, f.ParentFolderId, f.OwnerId, f.CreatedAt)).ToList();
        }

        //Actualiza los campos recibidos por parámetro
        public async Task<GetFolderResponseDto> UpdateFolderAsync(int folderId, int ownerId, UpdateFolderRequestDto dto, CancellationToken cToken)
        {
            //Obtener el folder
            FolderItem? folder = await _folderRepository.GetByIdAsync(folderId, cToken, asNoTracking:false);

            //Comprobar que el folder existe y pertenece al usuario
            if (folder == null || folder.OwnerId != ownerId)
                throw new FolderNotFoundException(folderId);

            //Filtros
            if (dto.NewParentFolderId.HasValue)
            {
                //Verificar que el nuevo parent foler existe y pertenece al usuario
                FolderItem? newParentFolder = await _folderRepository.GetByIdAsync(dto.NewParentFolderId.Value, cToken);
                if (newParentFolder == null || newParentFolder.OwnerId != ownerId)
                    throw new ParentFolderNotFoundException(dto.NewParentFolderId.Value);

                //Comprobar que no se intente meter la carpeta dentro de si misma o dentro de una de sus carpetas hijas
                if(await _folderRepository.IsDescendantAsync2(folder.Id, dto.NewParentFolderId.Value, cToken))
                    throw new FolderCyclicReferenceException(dto.NewParentFolderId.Value);
            }

            //Si se recibe un nuevo nombre de carpeta cambiarlo
            if (!string.IsNullOrEmpty(dto.NewFolderName))
            {
                //Cambiar el nombre
                folder.Name = dto.NewFolderName;

                //AuditLog
                await _auditService.LogAsync(AuditLogActions.ChangeFolderName, nameof(FolderItem), folderId, dto.NewFolderName, cToken);
            }

            //Si se recibe un nuevo parentFolder cambiarlo
            if (dto.NewParentFolderId.HasValue)
            {
                //Cambiar el folder padre
                if (dto.NewParentFolderId.Value == 0) //Si es 0 significa que se mueve a la carpeta raiz
                {
                    folder.ParentFolderId = null;
                }
                else
                {
                    folder.ParentFolderId = dto.NewParentFolderId.Value;
                }

                //AuditLog
                await _auditService.LogAsync(AuditLogActions.ChangeParentFolder, nameof(FolderItem), dto.NewParentFolderId, null, cToken);
            }

            //Persistir los cambios en la base de datos
            await _folderRepository.SaveChangesAsync(cToken);

            //Devolver resultado
            return new GetFolderResponseDto(
                folder.Id,
                folder.Name,
                folder.ParentFolderId,
                folder.OwnerId,
                folder.CreatedAt
                );
        }

        //Elimina una carpeta específica de un usuario.
        public async Task<DeleteFolderResponseDto> DeleteFolderAsync(int folderId, int ownerId, CancellationToken cToken)
        {
            //Obtener el folder a eliminar
            FolderItem? folderItem = await _folderRepository.GetByIdAsync(folderId, cToken);

            //Comprobar que pertenece al usuario
            if (folderItem == null || folderItem.OwnerId != ownerId)
                throw new FolderNotFoundException(folderId);

            //Comrpobar que esté vacío
            if (await _folderRepository.HasFilesAsync(folderId, cToken) ||
                await _folderRepository.HasSubfoldersAsync(folderId, cToken))
            {
                throw new FolderNotEmptyException(folderId);
            }

            //Eliminar el folder de la base de datos
            await _folderRepository.DeleteAsync(folderItem, cToken);

            //Persistir los cambios en la base de datos
            await _folderRepository.SaveChangesAsync(cToken);

            //AuditLog
            await _auditService.LogAsync(AuditLogActions.DeleteFolder, nameof(FolderItem), folderItem.Id, folderItem.Name, cToken);

            //Devolver el folder eliminado como respuesta
            return new DeleteFolderResponseDto(folderItem.Id, folderItem.Name);
        }
    }
}