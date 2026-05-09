
namespace HomeDB.Domain.Interfaces
{
    internal interface IFileStorageService
    {
        Task SaveAsync(Stream stream, string storedName, CancellationToken cancellationToken);
        Task DeleteAsync(string storedName, CancellationToken cancellationToken);
        bool Exists(string storedName);
    }
}
