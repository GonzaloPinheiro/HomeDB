
namespace HomeDB.Application.DTOs.Files
{
    //Se usa para recibir la información del archivo que se va a subir desde el cliente.
    public record UploadFileRequestDto(Stream FileStream, string FileName, long SizeBytes,
                                    string ContentType, int? FolderId);

    //Se usa para proporcionar información detallada del archivo al cliente después de subirlo.
    public record UploadFileResponseDto(int Id, string FileName, long SizeBytes, 
                                        string ContentType, int? FolderId, int OwnerId, DateTime UploadedAt);

    public record DownloadFileResponseDto(string FilePath, string FileName, string ContentType);

    //Se usa para proporcionar información básica del archivo al cliente después de eliminarlo.
    public record DeleteFileResponseDto(int FileId, string FileName);

    //Se usa para listar los archivos de una carpeta, sin incluir el OwnerId porque no es necesario para el cliente
    public record GetFileItemDto(int Id, string FileName, long SizeBytes, string ContentType, 
                                    int? FolderId, DateTime UploadedAt);
}