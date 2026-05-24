
namespace HomeDB.Domain.Interfaces
{
    public interface ICurrentUserService
    {
        int UserId { get; } //Id del usuario actual (0 si no autenticado)
        string Username { get; } //Nombre de usuario actual (string vacío si no autenticado)
        string? IpAddress { get; } //Dirección IP del cliente (string vacío o null si no disponible)
    }
}