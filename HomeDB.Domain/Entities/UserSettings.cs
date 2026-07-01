
namespace HomeDB.Domain.Entities
{
    public class UserSettings
    {
        public int Id { get; set; } // Primary key
        public int UserId { get; set; } //Clave foranea a la tabla User
        public User User { get; set; } = null!; //Navigation property hacia la entidad User

        public string Language { get; set; } = "es"; //Idioma preferido del usuario, por defecto español
        public string Timezone { get; set; } = "UTC"; //Zona horaria preferida del usuario, por defecto UTC
    }
}