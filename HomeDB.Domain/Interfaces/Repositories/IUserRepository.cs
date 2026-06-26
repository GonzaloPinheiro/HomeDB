using HomeDB.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace HomeDB.Domain.Interfaces.Repositories
{
    public interface IUserRepository
    {
        //Comrpueba si ya existe un usuario con el nombre indicado
        Task<bool> UsernameExistsAsync(string username, CancellationToken cToken);
        //Comrpueba si ya existe una cuenta con el email indicado
        Task<bool> EmailExistsAsync(string email, CancellationToken cToken);
        //Devuelve un usuario buscando por el userId
        Task<User?> GetUserByIdAsync(int userId, CancellationToken cToken, bool asNoTracking = true);
        //Devuelve un usuario con sus roles buscando por el userId
        Task<User?> GetUserByIdWithRolesAsync(int userId, CancellationToken cToken, bool asNoTracking = true);
        //Devuelve el usuario junto con sus roles asignados
        Task<User?> GetByUsernameWithRolesAsync(string username, CancellationToken cToken, bool asNoTracking = true);
        Task<(IEnumerable<User> Users, int TotalCount)> GetUsersAsync(int page, int pageSize,
                                              int? userId, string? userName, string? email,
                                              DateTimeOffset? from, DateTimeOffset? to,
                                              int? roleId, string? roleName,
                                              CancellationToken cToken);

        //Agrega un nuevo usuario
        Task AddUserAsync(User user, CancellationToken cToken);

        //Actualiza los parámetros del usuario
        Task<User> UpdateProfileAsync(int userId, string? username, string? email, CancellationToken cToken);

        //Elimina un usuario
        void DeleteUser(User user);

        //Confirma todos los cambios en la base de datos
        Task SaveChangesAsync(CancellationToken cToken);
    }
}