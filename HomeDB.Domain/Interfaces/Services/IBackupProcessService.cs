using HomeDB.Domain.Common.RecordsInfrastructure;

namespace HomeDB.Domain.Interfaces.Services
{
    public interface IBackupProcessService
    {
        /// <summary>
        /// Lanza el comando rsync para realizar una copia de seguridad de los archivos desde la ruta de origen a la ruta de destino, con la opción de especificar un enlace de destino.
        /// </summary>
        Task<BackupProcessResult> RunRsyncAsync(string sourcePath, string destinationPath, string? linkDestPath, CancellationToken cToken);

        /// <summary>
        /// Lanza el comando pg_dump para realizar una copia de seguridad de la base de datos PostgreSQL indicada por la cadena de conexión.
        /// </summary>
        Task<BackupProcessResult> RunPgDumpAsync(string connectionString, string outputFilePath, CancellationToken cToken);
    }
}