
namespace HomeDB.Application.DTOs.Files
{

    public record UploadFileRequestDto(Stream FileStream, string FileName, long SizeBytes,
                                    string ContentType, int? FolderId);

    public record UploadFileResponseDto(int Id, String FileName, long SizeBytes, 
                                        string ContentType, int? FolderId, int OwnerId, DateTime UploadedAt);

    public record DownloadFileResponseDto(string FilePath, string FileName, string ContentType);

}