using HomeDB.Application.DTOs;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Exceptions;
using HomeDB.Domain.Interfaces.Repositories;

namespace HomeDB.Application.Services
{
    public class RolesService
    {
        //Variables y objetos globales
        private readonly IRolesRepository _rolesRepository;

        //Constructores
        public RolesService(IRolesRepository rolesRepository)
        {
            _rolesRepository = rolesRepository;
        }

        /// <summary>
        /// Devuelve el rol solicitado
        /// </summary>
        public async Task<RoleResponseDto> GetRoleAsync(int roleId, CancellationToken cToken)
        {
            //Obtener el rol
            Role? result = await _rolesRepository.GetRoleAsync(roleId, cToken);

            //Si el rol no existe
            if (result is null)
                throw new RoleNotFoundException(roleId);

            //Devlver rol encontrado
            return new RoleResponseDto(result.Id, result.RoleName, result.RoleDescription);
        }

        /// <summary>
        /// Devuelve la lista de roles existentes
        /// </summary>
        public async Task<IEnumerable<RoleResponseDto>> GetRolesAsync(CancellationToken cToken)
        {
            //Obtener la lista de roles
            IEnumerable<Role> result = await _rolesRepository.GetRolesAsync(cToken, false);

            //Devolver el resultado
            return result.Select(r => new RoleResponseDto(r.Id, r.RoleName, r.RoleDescription));
        }

        /// <summary>
        /// Actualiza la descripción de un rol existente
        /// </summary>
        public async Task<RoleResponseDto> UpdateDescriptionAsync(int roleId, string newDescription, CancellationToken cToken)
        {
            //Obtener el rol
            Role? role = await _rolesRepository.GetRoleAsync(roleId, cToken, false);

            //Si el rol no existe
            if (role is null)
                throw new RoleNotFoundException(roleId);

            if(string.IsNullOrWhiteSpace(newDescription))
                throw new ArgumentNullException("La descripción no puede estar vacía.", nameof(newDescription));

            //Actualizar la descripción
            role.RoleDescription = newDescription;
            //await _rolesRepository.UpdateRoleAsync(role, cToken);

            //Persistir los cambios en la base de datos
            await _rolesRepository.SaveChangesAsync(cToken);

            //Devolver el rol actualizado
            return new RoleResponseDto(role.Id, role.RoleName, role.RoleDescription);
        }
    }
}