using System.Text.Json;
using HomeDB.Application.Options;
using HomeDB.Domain.Entities;
using Microsoft.Extensions.Options;

namespace HomeDB.Infrastructure.Observability
{
    /// <summary>
    /// Último recurso cuando algo falla en el pipeline de logs (insert a BD, cola llena, etc.):
    /// vuelca el LogEntry afectado a un fichero .jsonl en disco para no perder la información.
    /// </summary>
    public class LogFailureFileSink
    {
        private readonly string _basePath;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);

        public LogFailureFileSink(IOptions<LogFallbackOptions> options)
        {
            _basePath = options.Value.BasePath;
        }

        /// <summary>
        /// Vuelca un fallo del pipeline de logs (insert a BD, cola llena, etc.), junto con el
        /// LogEntry afectado, como una línea JSON en el fichero del día correspondiente.
        /// </summary>
        public async Task WriteAsync(LogFailureType failureType, LogEntry entry, Exception failure, CancellationToken cToken)
        {
            try
            {
                //Asegurarse de que el directorio del tipo de fallo existe.
                string directory = Path.Combine(_basePath, failureType.ToString());
                Directory.CreateDirectory(directory);

                string fileName = $"{DateTimeOffset.UtcNow:yyyy-MM-dd}.jsonl";
                string filePath = Path.Combine(directory, fileName);

                //Crear un objeto con los datos del LogEntry y el motivo del fallo.
                var record = new
                {
                    failedAtUtc = DateTimeOffset.UtcNow,
                    failureType = failureType.ToString(),
                    failureMessage = failure?.Message,
                    failureExceptionType = failure?.GetType().FullName,
                    originalTimeStamp = entry?.TimeStamp,
                    level = entry?.Level,
                    source = entry?.Source,
                    operation = entry?.Operation,
                    message = entry?.Message,
                    exception = string.IsNullOrEmpty(entry?.Exception) ? null : entry.Exception,
                    userId = entry?.UserId,
                    correlationId = entry?.CorrelationId,
                    durationMs = entry?.DurationMs,
                    metadataJson = entry?.MetadataJson
                };

                //Serializar el objeto a JSON y escribirlo en el fichero.
                string line = JsonSerializer.Serialize(record);

                //Asegurarse de que solo un hilo a la vez pueda escribir en el fichero.
                await _writeLock.WaitAsync(cToken).ConfigureAwait(false);

                //Escribir la línea en el fichero, añadiendo un salto de línea al final.
                try
                {
                    await File.AppendAllTextAsync(filePath, line + Environment.NewLine, cToken).ConfigureAwait(false);
                }
                finally
                {
                    //Liberar el semáforo para permitir que otros hilos escriban.
                    _writeLock.Release();
                }
            }
            catch (Exception ex)
            {
                // No hay más red de seguridad que la consola si ni el fallback a fichero funciona.
                Console.WriteLine($"No se pudo escribir el fallback de log a fichero ({failureType}): {ex}");
            }
        }
    }
}
