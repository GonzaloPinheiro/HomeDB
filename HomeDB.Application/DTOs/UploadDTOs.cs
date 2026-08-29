
//TODO Unificar todos los dto de subida de archivos en solo este archivo (HomeDB.Application.DTOs)(HomeDB.Application.DTOs.Files)
namespace HomeDB.Application.DTOs
{
    //DTO usado para inicializar una sesión de carga de archivos.
    public class UploadInitRequestDto
    {
        public string FileName { get; set; } = string.Empty;
        public long TotalSizeBytes { get; set; }
        public int TotalChunks { get; set; }
        public int? FolderId { get; set; } //Null = carpeta raíz
    }

    //DTO usado para responder al cliente con el SessionId de la sesión de carga inicializada.
    public record UploadInitResponseDto(
        Guid SessionId
    );

    public class UploadChunkRequestDto
    {
        public Guid SessionId { get; set; }
        public int ChunkNumber { get; set; }
        public Stream ChunkStream { get; set; } = null!;
    }

    //DTO usado para responder al cliente con el estado de la sesión de subida indicada.
    public record UploadStatusResponseDto(
        Guid SessionId,
        int TotalChunks,
        List<int> ReceivedChunks
    );
}