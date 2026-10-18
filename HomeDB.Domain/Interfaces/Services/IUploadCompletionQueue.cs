
namespace HomeDB.Domain.Interfaces.Services
{
    //Interfaz que expone la operación de encolar la finalización de una subida para que se procese en background.
    public interface IUploadCompletionQueue
    {
        //Encola la finalización de la sesión indicada; no espera a que se procese.
        Task QueueCompletionAsync(Guid sessionId, int ownerId);
    }
}
