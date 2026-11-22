using HomeDB.Application.Options;
using HomeDB.Domain.Common;
using HomeDB.Domain.Entities;
using HomeDB.Infrastructure.Observability;
using HomeDB.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace HomeDB.Tests.Unit.Infrastructure
{
    //Tests unitarios de StorageAvailabilityMonitor. El tiempo se pasa como parámetro, así que no hay esperas ni
    //temporizadores: se simula que pasan horas indicando la hora de cada comprobación.
    public sealed class StorageAvailabilityMonitorTests : IDisposable
    {
        private const string DiskKey = "hdd1";
        private static readonly DateTime StartTime = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

        //Variables y objetos
        private readonly string _rootPath = Directory.CreateTempSubdirectory("HomeDB.Tests.Monitor").FullName;
        private readonly string _markerPath;
        private readonly FakeLogQueue _logQueue = new FakeLogQueue();
        private readonly StorageAvailabilityMonitor _monitor;

        //Constructores
        public StorageAvailabilityMonitorTests()
        {
            string diskPath = Path.Combine(_rootPath, DiskKey);
            Directory.CreateDirectory(diskPath);
            _markerPath = Path.Combine(diskPath, StorageAvailabilityService.MarkerFileName);
            File.WriteAllText(_markerPath, DiskKey);

            IOptions<StorageOptions> options = Options.Create(new StorageOptions
            {
                Disks = new List<DiskOptions> { new DiskOptions { Key = DiskKey, Path = diskPath, StartupPolicy = DiskStartupPolicy.Optional } }
            });
            StorageAvailabilityService availability = new StorageAvailabilityService(options);
            Logger logger = _logQueue.CreateLogger(Path.Combine(_rootPath, "failed-logs"));

            //El validador de arranque deja el estado guardado; aquí se hace con una comprobación directa
            availability.CheckAllDisks();
            _monitor = new StorageAvailabilityMonitor(availability, logger);
            _monitor.SeedFromCachedStatuses(StartTime);
        }

        //Limpia el directorio temporal usado por el test
        public void Dispose()
        {
            if (Directory.Exists(_rootPath))
                Directory.Delete(_rootPath, recursive: true);
        }

        [Fact]
        public async Task CheckAsync_DiskStaysHealthy_LogsNothing()
        {
            await _monitor.CheckAsync(StartTime.AddMinutes(1));
            await _monitor.CheckAsync(StartTime.AddMinutes(2));

            Assert.Empty(_logQueue.Entries);
        }

        [Fact]
        public async Task CheckAsync_DiskGoesDown_LogsCriticalOnce_NotOnEveryCheck()
        {
            File.Delete(_markerPath);

            await _monitor.CheckAsync(StartTime.AddMinutes(1));
            await _monitor.CheckAsync(StartTime.AddMinutes(2));
            await _monitor.CheckAsync(StartTime.AddMinutes(3));

            LogEntry entry = Assert.Single(_logQueue.Entries);
            Assert.Equal("Critical", entry.Level);
            Assert.Contains(DiskKey, entry.Message);
            Assert.Contains("marker file is missing", entry.Message);
        }

        [Fact]
        public async Task CheckAsync_DiskStillDownAfterAWholeDay_LogsOneReminder()
        {
            File.Delete(_markerPath);
            await _monitor.CheckAsync(StartTime.AddMinutes(1));

            //Justo antes de cumplirse las 24 h desde el primer aviso: nada nuevo
            await _monitor.CheckAsync(StartTime.AddMinutes(1) + StorageAvailabilityMonitor.ReminderInterval - TimeSpan.FromSeconds(1));
            Assert.Single(_logQueue.Entries);

            //Pasadas las 24 h: un recordatorio
            DateTime reminderTime = StartTime.AddMinutes(1) + StorageAvailabilityMonitor.ReminderInterval;
            await _monitor.CheckAsync(reminderTime);
            Assert.Equal(2, _logQueue.Entries.Count);
            Assert.Equal("Critical", _logQueue.Entries[1].Level);
            Assert.StartsWith("Recordatorio", _logQueue.Entries[1].Message);

            //Y el siguiente recordatorio vuelve a esperar 24 h
            await _monitor.CheckAsync(reminderTime.AddHours(1));
            Assert.Equal(2, _logQueue.Entries.Count);
        }

        [Fact]
        public async Task CheckAsync_DiskRecovers_LogsInformation_AndAlertsAgainIfItFallsAgain()
        {
            File.Delete(_markerPath);
            await _monitor.CheckAsync(StartTime.AddMinutes(1));

            File.WriteAllText(_markerPath, DiskKey);
            await _monitor.CheckAsync(StartTime.AddMinutes(2));

            Assert.Equal(2, _logQueue.Entries.Count);
            Assert.Equal("Information", _logQueue.Entries[1].Level);
            Assert.Contains("vuelve a estar disponible", _logQueue.Entries[1].Message);

            //Una caída nueva avisa de inmediato, sin esperar al intervalo de recordatorio
            File.Delete(_markerPath);
            await _monitor.CheckAsync(StartTime.AddMinutes(3));

            Assert.Equal(3, _logQueue.Entries.Count);
            Assert.Equal("Critical", _logQueue.Entries[2].Level);
        }

        [Fact]
        public async Task CheckAsync_DiskAlreadyDownAtStartup_DoesNotAlertAgainOnFirstCheck()
        {
            //El validador de arranque ya registró este disco como caído: el vigilante parte de ese estado
            File.Delete(_markerPath);
            IOptions<StorageOptions> options = Options.Create(new StorageOptions
            {
                Disks = new List<DiskOptions> { new DiskOptions { Key = DiskKey, Path = Path.GetDirectoryName(_markerPath)!, StartupPolicy = DiskStartupPolicy.Optional } }
            });
            StorageAvailabilityService availability = new StorageAvailabilityService(options);
            availability.CheckAllDisks();
            StorageAvailabilityMonitor monitor = new StorageAvailabilityMonitor(availability, _logQueue.CreateLogger(Path.Combine(_rootPath, "failed-logs")));
            monitor.SeedFromCachedStatuses(StartTime);

            await monitor.CheckAsync(StartTime.AddMinutes(1));

            Assert.Empty(_logQueue.Entries);
        }

        [Fact]
        public async Task CheckAsync_WithoutSeed_FirstObservationOfADownDisk_LogsCritical()
        {
            File.Delete(_markerPath);
            IOptions<StorageOptions> options = Options.Create(new StorageOptions
            {
                Disks = new List<DiskOptions> { new DiskOptions { Key = DiskKey, Path = Path.GetDirectoryName(_markerPath)!, StartupPolicy = DiskStartupPolicy.Optional } }
            });
            StorageAvailabilityMonitor monitor = new StorageAvailabilityMonitor(
                new StorageAvailabilityService(options), _logQueue.CreateLogger(Path.Combine(_rootPath, "failed-logs")));

            await monitor.CheckAsync(StartTime);

            Assert.Equal("Critical", Assert.Single(_logQueue.Entries).Level);
        }
    }
}
