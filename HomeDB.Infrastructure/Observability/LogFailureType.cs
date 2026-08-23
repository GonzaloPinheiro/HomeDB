namespace HomeDB.Infrastructure.Observability
{
    //Tipos de fallo del pipeline de logs que puede volcar LogFailureFileSink, cada uno representando un subdirectorio diferente bajo BasePath.
    public enum LogFailureType
    {
        InsertFailure = 1,
        QueueFull = 2,
        NullEntry = 3
    }
}