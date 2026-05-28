
namespace HomeDB.Application.DTOs
{
    //DTO para la creación de una carpeta
    public record CreateFolderRequestDto(string Name, int? ParentFolderId);

    //DTO para la respuesta de la creación de una carpeta
    public record CreateFolderResponseDto(int Id, string Name, int? ParentFolderId, int OwnerId, DateTime CreatedAt);

    //DTO para la respuesta de obtener una carpeta
    public record GetFolderResponseDto(int Id, string Name, int? ParentFolderId, int OwnerId, DateTime CreatedAt);

    //DTO para la respuesta de la eliminación de una carpeta
    public record DeleteFolderResponseDto(int FolderId, string Name);
}