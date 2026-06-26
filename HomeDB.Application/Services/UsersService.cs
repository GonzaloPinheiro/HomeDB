using HomeDB.Application.DTOs;
using HomeDB.Application.DTOs.Auth;
using HomeDB.Domain.Common;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Exceptions;
using HomeDB.Domain.Interfaces.Repositories;

namespace HomeDB.Application.Services
{
    public class UsersService
    {
        //Variables y objetos globales
        private readonly IUserRepository _userRepository;
        private readonly AuditService _auditService;

        //Constructores
        public UsersService(IUserRepository userRepository, AuditService auditService)
        {
            _userRepository = userRepository;
            _auditService = auditService;
        }

        //Obtener una lista de usuarios paginada y filtrada según los parámetros de consulta
        public async Task<GetUsersResponseDto> GetUsersAsync(GetUsersRequestDto dto, CancellationToken cToken)
        {
            //Obtener la lista de usuarios
            (IEnumerable<User> users, int totalCount) = await _userRepository.GetUsersAsync(
                page: dto.Page,
                pageSize: dto.PageSize,
                userId: dto.userId,
                userName: dto.UserName,
                email: dto.Email,
                from: dto.From,
                to: dto.To,
                roleId: dto.roleId,
                roleName: dto.RoleName,
                cToken: cToken);

            //Mapear los usuarios a DTO de respuesta
            IEnumerable<UserSummaryDto> userDtos = users.Select(u => new UserSummaryDto(
                u.Id,
                u.Username,
                u.Email,
                u.CreatedAt,
                u.UserRoles.Select(ur => ur.Role.RoleName)));

            //Devolver los usuarios
            return new GetUsersResponseDto
            {
                Users = userDtos,
                TotalCount = totalCount,
                Page = dto.Page,
                PageSize = dto.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / dto.PageSize)
            };
        }

        //Obtener un usuario por su ID
        public async Task<UserSummaryDto?> GetUserByIdAsync(int userId, CancellationToken cToken)
        {
            //Obtener el usuario con sus roles de la base de datos
            User? user = await _userRepository.GetUserByIdWithRolesAsync(userId, cToken);

            //Si el usuario no existe
            if (user == null)
                throw new UserNotFoundException(nameof(user));

            //Devolver el usuario mapeado a DTO de respuesta
            return new UserSummaryDto(
                user.Id,
                user.Username,
                user.Email,
                user.CreatedAt,
                user.UserRoles.Select(ur => ur.Role.RoleName));
        }
        
        //Actualiza el perfil del usuario con los campos recibidos en el Dto
        public async Task<UpdateProfileResponseDto> UpdateProfileAsync(int userId, UpdateProfileRequestDto dto, CancellationToken cToken)
        {
            //Filtros
            if(dto == null)
                throw new ArgumentNullException(nameof(dto));

            //Obtener el usuario
            User? user = await _userRepository.GetUserByIdAsync(userId, cToken, false);

            //Si el usuario no existe
            if (user == null)
                throw new UserNotFoundException(userId);

            //Comprobar si ya existe un usuario con ese nombre
            if(dto.Username != null && await _userRepository.UsernameExistsAsync(dto.Username, cToken))
                throw new UserAlreadyExistsException(dto.Username);

            //Comprobar si ya existe una cuenta con ese correo
            if(dto.Email != null && await _userRepository.EmailExistsAsync(dto.Email, cToken))
                throw new EmailAlreadyExistsException(dto.Email);

            //Modificar los campos recibidos
            User updatedUser = await _userRepository.UpdateProfileAsync(userId, dto.Username, dto.Email, cToken);

            //Persistir los cambios en la base de datos
            await _userRepository.SaveChangesAsync(cToken);

            //Audit log
            await _auditService.LogAsync(AuditLogActions.UpdateProfile, nameof(User), userId, null, cToken);

            //Devolver Dto
            return new UpdateProfileResponseDto
            (
               userId,
               updatedUser.Username,
               updatedUser.Email
            );
        }

        //Borra el usuario indicado por parámetros (a no ser que sea administrador)
        public async Task<DeleteUserResponseDto> DeleteUserAsync(int targetUserId, int requestingUserId, CancellationToken cToken)
        {
            //Obtener el usuario con sus roles (tracking activo para poder eliminarlo)
            User? user = await _userRepository.GetUserByIdWithRolesAsync(targetUserId, cToken, asNoTracking: false);

            //Si el usuario no existe
            if (user == null)
                throw new UserNotFoundException(targetUserId);

            //Un admin solo puede ser eliminado por sí mismo
            bool targetIsAdmin = user.UserRoles.Any(ur => ur.RoleId == (int)RolesList.Admin);
            if (targetIsAdmin && targetUserId != requestingUserId)
                throw new UnauthorizedException(requestingUserId, targetUserId);

            //Eliminar el usuario de la base de datos
            _userRepository.DeleteUser(user);

            //Persistir los cambios en la base de datos
            await _userRepository.SaveChangesAsync(cToken);

            //Audit log
            await _auditService.LogAsync(AuditLogActions.DeleteUser, nameof(User), user.Id, user.Username, cToken);

            //Devolver datos del usuario eliminado
            return new DeleteUserResponseDto(user.Id, user.Username);
        }
    }
}