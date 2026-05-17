using HomeDB.Domain.Entities;

namespace HomeDB.Domain.Interfaces.Repositories
{
    public interface IUserRepository
    {
        //Comrpueba si ya existe un usuario con el nombre indicado
        Task<bool> UsernameExistsAsync(string username, CancellationToken cToken);
        //Devuelve el usuario junto con sus roles asignados
        Task<User?> GetByUsernameWithRolesAsync(string username, CancellationToken cToken);
        //Agrega un nuevo usuario
        Task AddUserAsync(User user, CancellationToken cToken);
        //Confirma todos los cambios en la base de datos
        Task SaveChangesAsync(CancellationToken cToken);
    }
}
