namespace HomeDB.Infrastructure.Observability
{
    //Tipos de fallo del pipeline de logs que puede volcar LogFailureFileSink, cada uno representando un subdirectorio diferente bajo BasePath.
    public enum LogFailureType
    {
        InsertFailure = 1
    }
}