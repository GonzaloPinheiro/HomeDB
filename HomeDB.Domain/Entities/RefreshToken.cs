
namespace HomeDB.Domain.Entities
{
    public class RefreshToken
    {
        
        public int Id { get; set; } //PK (identity)
        public string Token { get; set; } = string.Empty; //El token de refresco
        public int UserId { get; set; } //Id del usuario al que pertenece el token
        public DateTime ExpiresAt { get; set; } //Fecha de expiración del token
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; //Fecha de creación del token
        public bool IsRevoked { get; set; } = false; //Indica si el token ha sido revocado
        public string? ReplacedByToken { get; set; } //Token por el que ha sido reempazado

        //Naviation propertys
        public User? User { get; set; } //Referencia al usuario (navegation property)
    }
}
