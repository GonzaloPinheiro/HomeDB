
using HomeDB.Domain.Common.Enums;

namespace HomeDB.Domain.Entities
{
    public class UploadSession
    {
        public int Id { get; set; } //PK
        public Guid SessionId { get; set; } = Guid.NewGuid(); //Identificador único de la sesión de carga enviado al cliente.
        public string FileName { get; set; } = string.Empty; //Nombre del archivo que se está cargando.
        public long TotalSizeBytes { get; set; } //Tamaño total del archivo en bytes declarado por el cliente.
        public long ReceivedSizeBytes { get; set; } = 0; //Tamaño acumulado de los chunks recibidos hasta el momento.
        public long MaxFileSizeBytes { get; set; } //Límite de tamaño snapshoteado en el momento del initiate.
        public int TotalChunks { get; set; } //Número total de fragmentos en los que se dividirá el archivo.
        public int OwnerId { get; set; } //Id del usuario que inició la sesión de carga.
        public int? FolderId { get; set; } //Id de la carpeta donde se almacenará el archivo cargado (opcional).
        public UploadSessionStatus Status { get; set; } = UploadSessionStatus.InProgress; //Estado actual de la sesión de carga.
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow; //Fecha y hora en que se creó la sesión de carga.
        public DateTimeOffset? LastActivityAt { get; set; } = null; //Fecha y hora de la última actividad de la sesión. Usado para detectar sesiones huérfanas.


        //Navegation property hacia el usuario que inició la sesión de carga.
        public ICollection<UploadChunk> Chunks { get; set; } = new List<UploadChunk>();
    }
}
