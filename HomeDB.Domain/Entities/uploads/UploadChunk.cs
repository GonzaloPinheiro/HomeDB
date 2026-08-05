
namespace HomeDB.Domain.Entities
{
    //Representa un fragmento de un archivo cargado en una sesión de carga.
    public class UploadChunk
    {
        public int Id { get; set; } //PK
        public int UploadSessionId { get; set; } //Id de la sesión de carga a la que pertenece este fragmento.
        public int ChunkNumber { get; set; } //Número del fragmento dentro de la sesión de carga.
        public long SizeBytes { get; set; } //Tamaño en bytes del fragmento escrito en disco.
        public DateTimeOffset ReceivedAt { get; set; } //Fecha y hora en que se recibió este fragmento.

        //Navegation property hacia la sesión de carga a la que pertenece este fragmento.
        public UploadSession UploadSession { get; set; } = null!;
    }   
}