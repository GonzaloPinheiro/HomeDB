namespace HomeDB.Domain.Common.RecordsInfrastructure
{
    /// <summary>
    /// Capacidad del sistema de ficheros que respalda un disco. FreeBytes es el espacio disponible para la API y
    /// UsedBytes se calcula como Total - Free, igual que el monitor de sistema: en ext4 el espacio reservado
    /// para root cuenta como usado.
    /// </summary>
    public record DiskCapacity(long TotalBytes, long FreeBytes)
    {
        public long UsedBytes => TotalBytes - FreeBytes;
    }
}
