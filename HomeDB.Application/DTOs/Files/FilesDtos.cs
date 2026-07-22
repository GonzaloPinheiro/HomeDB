
using System.ComponentModel.DataAnnotations;

namespace HomeDB.Application.DTOs.Files
{
    //Se usa para recibir la información del archivo que se va a subir desde el cliente.
    public record UploadFileRequestDto(Stream FileStream, string FileName, long SizeBytes,
                                    string ContentType, int? FolderId);

    //Se usa para proporcionar información detallada del archivo al cliente después de subirlo.
    public record UploadFileResponseDto(int Id, string FileName, long SizeBytes,
                                        string ContentType, int? FolderId, int OwnerId, DateTime UploadedAt);

    public record DownloadFileResponseDto(string FilePath, string FileName, string ContentType);

    //Se usa para actualizar los campos de un archivo, como el nombre o la carpeta a la que pertenece.
    public class UpdateFileRequestDto
    {
        public string? NewFileName { get; set; } //Nuevo nombre
        public int? NewFolderId { get; set; } //Carpeta, OJO si es null es = a carpeta raiz
    }

    //Se usa para proporcionar información detallada del archivo al cliente después de actualizarlo.
    public record UpdateFileResponseDto(int Id, string FileName, long SizeBytes,
                                        string ContentType, int? FolderId, int OwnerId, DateTime UploadedAt);

    //Se usa para proporcionar información básica del archivo al cliente después de eliminarlo.
    public record DeleteFileResponseDto(int FileId, string FileName);

    //Se usa para aplicar los filtros al buscar entre los archivos
    public class SearchFileRequestDto
    {
        public string? FileName { get; set; }
        public int? FolderId { get; set; }
        public string? ContentType { get; set; }
        public long? MinSizeBytes { get; set; }
        public long? MaxSizeBytes { get; set; }
        public DateTime? UploadedFrom { get; set; }
        public DateTime? UploadedTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    //Se usa para la respuesta de buscar file items
    public record SearchFilesResponseDto
    (
        IEnumerable<GetFileItemDto> Items,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages
    );



    //Se usa para listar los archivos de una carpeta, sin incluir el OwnerId porque no es necesario para el cliente
    public record GetFileItemDto(int Id, string FileName, long SizeBytes, string ContentType,
                                    int? FolderId, DateTime UploadedAt);
}