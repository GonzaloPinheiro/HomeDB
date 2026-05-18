
namespace HomeDB.Application.DTOs
{
    public record CreateFolderRequestDto(string Name, int? ParentFolderId);

    public record CreateFolderResponseDto(int Id, string Name, int? ParentFolderId, int OwnerId, DateTime CreatedAt);

    public record GetFolderResponseDto(int Id, string Name, int? ParentFolderId, int OwnerId, DateTime CreatedAt);

    public record DeleteFolderResponseDto(int FolderId, string Name);
}