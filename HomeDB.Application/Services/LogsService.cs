using HomeDB.Application.DTOs;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces.Repositories;

namespace HomeDB.Application.Services
{
    public class LogsService
    {
        //Variables y objetos globales
        private readonly ILogEntryRepository _logEntryRepository;

        //Constructores
        public LogsService(ILogEntryRepository logEntryRepository)
        {
            _logEntryRepository = logEntryRepository
                ?? throw new ArgumentNullException(nameof(logEntryRepository));
        }


        //Devuelve una lista de logs paginada y filtrada según los parámetros de consulta
        public async Task<GetLogsResponseDto> GetLogsAsync(GetLogsRequestDto query,CancellationToken cToken)
        {
            //Obtener los logs de la DB aplicando los filtros y la paginación
            (IEnumerable<LogEntry> items, int totalCount) = await _logEntryRepository.GetLogsAsync(
                query.Level,
                query.Operation,
                query.From,
                query.To,
                query.CorrelationId,
                query.Page,
                query.PageSize,
                cToken);

            //Mapear las entidades LogEntry a DTOs LogEntryDto y construir la respuesta con la paginación
            return new GetLogsResponseDto
            {
                Items = items.Select(MapToDto),
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
            };
        }

        //Devuelve un resumen de la salud del sistema basado en los logs de la última hora y las últimas 24 horas
        public async Task<LogHealthResponseDto> GetHealthAsync(CancellationToken cToken)
        {
            //Intervalos de tiempo
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset oneHourAgo = now.AddHours(-1);
            DateTimeOffset oneDayAgo = now.AddHours(-24);

            //Obtener los logs de las últimas 24 horas para calcular los conteos
            IEnumerable<(string Level, DateTimeOffset TimeStamp)> entries =
                await _logEntryRepository.GetHealthAsync(cToken);

            //Calcular los conteos de errores y advertencias en los intervalos de tiempo especificados
            return new LogHealthResponseDto
            {
                ErrorsLastHour = entries.Count(l => l.Level == "Error" && l.TimeStamp >= oneHourAgo),
                ErrorsLast24h = entries.Count(l => l.Level == "Error"),
                WarningsLast24h = entries.Count(l => l.Level == "Warning")
            };
        }

        //Devuelve un resumen de los errores agrupados por operación en las últimas horas
        public async Task<IEnumerable<LogErrorSummaryItemDto>> GetErrorSummaryAsync(int hours, CancellationToken cToken)
        {
            //Obtener últimos errores en base a las horas recibidas
            IEnumerable<(string Operation, int Count)> results =
                await _logEntryRepository.GetErrorSummaryAsync(hours, cToken);

            //Devolver la lista de errores
            return results.Select(r => new LogErrorSummaryItemDto
            {
                Operation = r.Operation,
                Count = r.Count
            });
        }

        //Devuelve una lista de operaciones lentas que superan un umbral de duración especificado
        public async Task<IEnumerable<LogSlowOperationDto>> GetSlowOperationsAsync(long thresholdMs, CancellationToken cToken)
        {
            //Devuelve una lista de operaciones que superaron el tiempo recibido
            IEnumerable<(string Operation, long DurationMs, DateTimeOffset TimeStamp)> results =
                await _logEntryRepository.GetSlowOperationsAsync(thresholdMs, cToken);

            //Devolver la lista de operaciones
            return results.Select(r => new LogSlowOperationDto
            {
                Operation = r.Operation,
                DurationMs = r.DurationMs,
                TimeStamp = r.TimeStamp
            });
        }

        #region Funciones auxiliares privadas
        //Mapea una entidad LogEntry a un DTO LogEntryDto
        private static LogEntryDto MapToDto(LogEntry entry)
        {
            return new LogEntryDto
            {
                Id = entry.Id,
                TimeStamp = entry.TimeStamp,
                Level = entry.Level,
                Source = entry.Source,
                Operation = entry.Operation,
                Message = entry.Message,
                Exception = string.IsNullOrEmpty(entry.Exception) ? null : entry.Exception,
                UserId = entry.UserId,
                CorrelationId = entry.CorrelationId,
                DurationMs = entry.DurationMs
            };
        }
        #endregion
    }
}