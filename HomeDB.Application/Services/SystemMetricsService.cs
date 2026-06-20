using HomeDB.Application.DTOs;
using HomeDB.Domain.Common.Snapshots;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Exceptions;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Domain.Interfaces.Services;

namespace HomeDB.Application.Services
{
    public class SystemMetricsService
    {
        //Variables y objetos globales
        private readonly ISystemMetricsReaderService _reader;
        private readonly ISystemMetricsRepository _repository;

        //Constructores
        public SystemMetricsService(ISystemMetricsReaderService reader, ISystemMetricsRepository repository)
        {
            _reader = reader;
            _repository = repository;
        }

        /// <summary>
        /// Lee todas las métricas actuales del sistema y las persiste como una única entrada
        /// de histórico. Pensado para ser llamado periódicamente desde un IHostedService.
        /// </summary>
        public async Task CaptureAndStoreAsync(CancellationToken cToken)
        {
            //Leer métricas del sistema
            CpuSnapshot? cpu = await _reader.ReadCpuAsync(cToken);
            MemorySnapshot? memory = await _reader.ReadMemoryAsync(cToken);
            DiskSnapshot? disk = await _reader.ReadDiskAsync(cToken);
            TemperatureSnapshot? temperature = await _reader.ReadTemperatureAsync(cToken);

            //Crear objeto a insertar en DB
            SystemMetricsEntry entry = new SystemMetricsEntry(
                cpu,
                memory,
                disk,
                temperature);

            //Guardar las métricas en la DB
            await _repository.InsertAsync(entry, cToken);
        }

        /// <summary>
        /// Devuelve el último registro de las métricas registrado en la DB
        /// </summary>
        public async Task<SystemMetricsResponseDto> GetLastMetricAsync(CancellationToken cToken) //TODO Cambiar la lógica para forzar una lectura del sistema y leer ese registro?
        {
            //Obtener la métrica más reciente
            SystemMetricsEntry? lastEntrie = await _repository.GetLastAsync(cToken);

            //Si no hay último registro no devuelve nada
            if (lastEntrie is null)
                throw new MetricNotFoundException();

            //Devolver el último registro
            return new SystemMetricsResponseDto(
                lastEntrie.Timestamp,
                lastEntrie.CpuUsagePercent,
                lastEntrie.MemoryTotalBytes,
                lastEntrie.MemoryUsedBytes,
                lastEntrie.MemoryUsagePercent,
                lastEntrie.DiskTotalBytes,
                lastEntrie.DiskUsedBytes,
                lastEntrie.DiskUsagePercent,
                lastEntrie.TemperatureCelsius);
        }

        /// <summary>
        /// Devuelve el histórico de métricas dentro de un rango de fechas, para que el
        /// controller de admin lo exponga paginado o tal cual al frontend.
        /// </summary>
        public async Task<IEnumerable<SystemMetricsResponseDto>> GetHistoryAsync(
            DateTimeOffset from,
            DateTimeOffset to,
            CancellationToken cToken)
        {
            //Obtener el histórico de las métricas
            IEnumerable<SystemMetricsEntry> entries = await _repository.GetRangeAsync(from, to, cToken);

            //Devolver resultado
            return entries.Select(e => new SystemMetricsResponseDto(
                e.Timestamp,
                e.CpuUsagePercent,
                e.MemoryTotalBytes,
                e.MemoryUsedBytes,
                e.MemoryUsagePercent,
                e.DiskTotalBytes,
                e.DiskUsedBytes,
                e.DiskUsagePercent,
                e.TemperatureCelsius));
        }

        /// <summary>
        /// Borra del histórico todo lo anterior al punto de corte calculado a partir
        /// de la retención configurada. Pensado para ser llamado periódicamente,
        /// separado de la captura para no acoplar ambas responsabilidades.
        /// </summary>
        public async Task PurgeOldEntriesAsync(TimeSpan retention, CancellationToken cToken)
        {
            DateTimeOffset cutoff = DateTimeOffset.UtcNow.Subtract(retention);
            await _repository.DeleteOlderThanAsync(cutoff, cToken);
        }
    }
}