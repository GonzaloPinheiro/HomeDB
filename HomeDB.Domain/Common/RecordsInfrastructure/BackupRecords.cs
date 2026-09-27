using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeDB.Domain.Common.RecordsInfrastructure
{
    /// <summary>
    /// Representa el resultado de un proceso de backup, incluyendo información sobre el éxito, código de salida, tamaño de salida y mensaje de error.
    /// </summary>
    public record BackupProcessResult(bool Success, int ExitCode, long OutputSizeBytes, string? ErrorMessage);
}
