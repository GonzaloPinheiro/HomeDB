using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces.Repositories
{
    public interface IUserRepository
    {
        /// <summary>
        /// Comrpueba si existe un usuario con el nombre indicado
        /// </summary>
        Task<bool> UserExistsAsync(string username, CancellationToken cToken);

        /// <summary>
        /// Comrpueba si existe un usuario con el ID indicado
        /// </summary>
        Task<bool> UserExistsAsync(int userId, CancellationToken cToken);

        /// <summary>
        /// Comrpueba si ya existe una cuenta con el email indicado
        /// </summary>
        Task<bool> EmailExistsAsync(string email, CancellationToken cToken);

        /// <summary>
        /// Devuelve un usuario buscando por el userId
        /// </summary>
        Task<User?> GetUserByIdAsync(int userId, CancellationToken cToken, bool asNoTracking = true);

        /// <summary>
        /// Devuelve un usuario con sus roles buscando por el userId
        /// </summary>
        Task<User?> GetUserByIdWithRolesAsync(int userId, CancellationToken cToken, bool asNoTracking = true);

        /// <summary>
        /// Devuelve el usuario junto con sus roles asignados
        /// </summary>
        Task<User?> GetByUsernameWithRolesAsync(string username, CancellationToken cToken, bool asNoTracking = true);

        /// <summary>
        /// Devuelve el usuario junto con sus roles asignados
        /// </summary>
        Task<(IEnumerable<User> Users, int TotalCount)> GetUsersAsync(int page, int pageSize,
                                              int? userId, string? userName, string? email,
                                              DateTimeOffset? from, DateTimeOffset? to,
                                              int? roleId, string? roleName,
                                              CancellationToken cToken);

        /// <summary>
        /// Agrega un nuevo usuario
        /// </summary>
        Task AddUserAsync(User user, CancellationToken cToken);

        /// <summary>
        /// Elimina un usuario
        /// </summary>
        void DeleteUser(User user);

        /// <summary>
        /// Confirma todos los cambios en la base de datos
        /// </summary>
        Task SaveChangesAsync(CancellationToken cToken);
    }
}