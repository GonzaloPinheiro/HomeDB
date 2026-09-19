using HomeDB.Application.Options;
using HomeDB.Application.Services;
using HomeDB.Domain.Common.Enums;
using HomeDB.Domain.Common.RecordsInfrastructure;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace HomeDB.Tests.Unit.Application
{
    //Tests unitarios de BackupService: rotación de directorios en disco, uso de link-dest de rsync,
    //propagación de fallos rsync/pg_dump y actualización de la auditoría. No hay rsync ni pg_dump
    //reales de por medio: se usan fakes en memoria para el repositorio y el proceso de backup, y
    //directorios temporales reales en disco para poder comprobar la rotación tal cual la hace el código.
    public sealed class BackupServiceTests : IDisposable
    {
        //Variables y objetos
        private readonly string _rootPath = Directory.CreateTempSubdirectory("HomeDB.Tests.Backup").FullName;
        private readonly string _sourceDirectory;
        private readonly string _dailyDirectory;
        private readonly FakeBackupAuditRepository _repository = new FakeBackupAuditRepository();
        private readonly FakeBackupProcessService _processService = new FakeBackupProcessService();

        //Constructores
        public BackupServiceTests()
        {
            _sourceDirectory = Path.Combine(_rootPath, "source");
            _dailyDirectory = Path.Combine(_rootPath, "daily");
            Directory.CreateDirectory(_sourceDirectory);
            Directory.CreateDirectory(_dailyDirectory);
        }

        //Limpia el directorio temporal usado por el test
        public void Dispose()
        {
            if (Directory.Exists(_rootPath))
                Directory.Delete(_rootPath, recursive: true);
        }

        [Fact]
        public async Task RunDailyBackupAsync_FirstRun_CreatesCurrentBackupAndSuccessEntry()
        {
            BackupService service = CreateService();

            await service.RunDailyBackupAsync(CancellationToken.None);

            //Se crea el directorio actual con el contenido simulado por el fake de rsync
            string currentPath = Path.Combine(_dailyDirectory, "backup_actual");
            Assert.True(Directory.Exists(currentPath));

            //No había backup anterior, así que rsync se llama sin link-dest
            Assert.True(_processService.RsyncWasCalled);
            Assert.Null(_processService.LastRsyncLinkDestPath);
            Assert.Equal(_sourceDirectory, _processService.LastRsyncSourcePath);
            Assert.Equal(currentPath, _processService.LastRsyncDestinationPath);

            //Se registra un único entry en éxito con los tamaños devueltos por rsync/pg_dump
            BackupAuditEntry entry = Assert.Single(_repository.Entries);
            Assert.Equal(BackupLevel.Daily, entry.Level);
            Assert.Equal(BackupStatus.Success, entry.Status);
            Assert.NotNull(entry.CompletedAt);
            Assert.Equal(currentPath, entry.BackupPath);
            Assert.Equal(_processService.RsyncResult.OutputSizeBytes, entry.FilesBackedUpSizeBytes);
            Assert.Equal(_processService.PgDumpResult.OutputSizeBytes, entry.DatabaseDumpSizeBytes);
            Assert.Null(entry.ErrorMessage);
        }

        [Fact]
        public async Task RunDailyBackupAsync_WhenPreviousBackupExists_RotatesDirectoriesAndUsesLinkDest()
        {
            BackupService service = CreateService();

            //Primer backup: crea backup_actual
            await service.RunDailyBackupAsync(CancellationToken.None);

            //Segundo backup: backup_actual debe pasar a ser backup_anterior y reutilizarse como link-dest
            await service.RunDailyBackupAsync(CancellationToken.None);

            string currentPath = Path.Combine(_dailyDirectory, "backup_actual");
            string previousPath = Path.Combine(_dailyDirectory, "backup_anterior");

            Assert.True(Directory.Exists(currentPath));
            Assert.True(Directory.Exists(previousPath));
            Assert.Equal(previousPath, _processService.LastRsyncLinkDestPath);

            //Dos ejecuciones, dos entries de auditoría
            Assert.Equal(2, _repository.Entries.Count);
        }

        [Fact]
        public async Task RunDailyBackupAsync_MarksOldestActiveEntryAsDeletedBeforeRotating()
        {
            //Entry preexistente "activo" simulando un backup anterior ya registrado en la base de datos
            BackupAuditEntry existingEntry = new BackupAuditEntry
            {
                Level = BackupLevel.Daily,
                Status = BackupStatus.Success,
                StartedAt = DateTime.UtcNow.AddDays(-1),
                CompletedAt = DateTime.UtcNow.AddDays(-1),
                BackupPath = Path.Combine(_dailyDirectory, "backup_actual")
            };
            _repository.Entries.Add(existingEntry);

            BackupService service = CreateService();
            await service.RunDailyBackupAsync(CancellationToken.None);

            //El entry antiguo debe quedar marcado como eliminado y sin ruta de backup asociada,
            //porque su backup físico va a ser (o ya fue) reemplazado por la rotación
            Assert.NotNull(existingEntry.DeletedAt);
            Assert.Null(existingEntry.BackupPath);
        }

        [Fact]
        public async Task RunDailyBackupAsync_WhenRsyncFails_SkipsPgDumpAndMarksEntryFailed()
        {
            _processService.RsyncResult = new BackupProcessResult(false, 1, 0, "rsync error");

            BackupService service = CreateService();
            await service.RunDailyBackupAsync(CancellationToken.None);

            //Si rsync falla, no tiene sentido volcar la base de datos
            Assert.False(_processService.PgDumpWasCalled);

            BackupAuditEntry entry = Assert.Single(_repository.Entries);
            Assert.Equal(BackupStatus.Failed, entry.Status);
            Assert.Equal("rsync error", entry.ErrorMessage);
            Assert.Equal(0, entry.DatabaseDumpSizeBytes);
        }

        [Fact]
        public async Task RunDailyBackupAsync_WhenPgDumpFails_MarksEntryFailedWithPgDumpError()
        {
            _processService.PgDumpResult = new BackupProcessResult(false, 1, 0, "pg_dump error");

            BackupService service = CreateService();
            await service.RunDailyBackupAsync(CancellationToken.None);

            Assert.True(_processService.PgDumpWasCalled);

            BackupAuditEntry entry = Assert.Single(_repository.Entries);
            Assert.Equal(BackupStatus.Failed, entry.Status);
            Assert.Equal("pg_dump error", entry.ErrorMessage);
        }

        [Fact]
        public async Task RunDailyBackupAsync_UsesConfiguredConnectionStringAndOutputPathForPgDump()
        {
            const string connectionString = "Host=myhost;Port=5432;Database=homedb;Username=myuser;Password=mypass";
            BackupService service = CreateService(connectionString);

            await service.RunDailyBackupAsync(CancellationToken.None);

            Assert.Equal(connectionString, _processService.LastPgDumpConnectionString);
            Assert.Equal(
                Path.Combine(_dailyDirectory, "backup_actual", "database.dump"),
                _processService.LastPgDumpOutputFilePath);
        }

        [Fact]
        public async Task RunDailyBackupAsync_WhenConnectionStringMissing_ThrowsAndMarksEntryFailed()
        {
            BackupService service = CreateService(connectionString: null);

            InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.RunDailyBackupAsync(CancellationToken.None));

            //El registro de auditoría ya se había guardado como "en curso" antes de intentar el pg_dump;
            //al lanzar la excepción debe quedar marcado como fallido en vez de "en curso" para siempre
            BackupAuditEntry entry = Assert.Single(_repository.Entries);
            Assert.Equal(BackupStatus.Failed, entry.Status);
            Assert.NotNull(entry.CompletedAt);
            Assert.Equal(thrown.Message, entry.ErrorMessage);
        }

        //Crea un BackupService con los fakes del test y la configuración indicada
        private BackupService CreateService(string? connectionString = "Host=localhost;Database=homedb;Username=u;Password=p")
        {
            Dictionary<string, string?> configValues = new Dictionary<string, string?>();
            if (connectionString is not null)
                configValues["ConnectionStrings:PostgreSQL_HomeDB"] = connectionString;

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues)
                .Build();

            IOptions<BackupOptions> options = Options.Create(new BackupOptions
            {
                SourceDirectory = _sourceDirectory,
                Daily = new BackupLevelOptions { Directory = _dailyDirectory }
            });

            return new BackupService(_repository, _processService, configuration, options);
        }

        #region Fakes
        //Repositorio de auditoría en memoria: refleja el mismo comportamiento que la implementación
        //real sobre EF Core (mismos filtros/orden), pero sin tocar una base de datos.
        private sealed class FakeBackupAuditRepository : IBackupAuditRepository
        {
            public List<BackupAuditEntry> Entries { get; } = new List<BackupAuditEntry>();

            public Task<BackupAuditEntry?> GetOldestActiveAsync(BackupLevel level, CancellationToken cToken, bool asNoTracking = true)
            {
                BackupAuditEntry? oldest = Entries
                    .Where(entry => entry.Level == level && entry.DeletedAt == null)
                    .OrderBy(entry => entry.StartedAt)
                    .FirstOrDefault();

                return Task.FromResult(oldest);
            }

            public Task<DateTime?> GetLastSuccessfulCompletedAtAsync(BackupLevel level, CancellationToken cToken)
            {
                DateTime? lastCompletedAt = Entries
                    .Where(entry => entry.Level == level && entry.Status == BackupStatus.Success)
                    .OrderByDescending(entry => entry.CompletedAt)
                    .Select(entry => entry.CompletedAt)
                    .FirstOrDefault();

                return Task.FromResult(lastCompletedAt);
            }

            public Task<BackupAuditEntry?> GetLatestActiveAsync(BackupLevel level, CancellationToken cToken, bool asNoTracking = true)
            {
                BackupAuditEntry? latest = Entries
                    .Where(entry => entry.Level == level && entry.DeletedAt == null)
                    .OrderByDescending(entry => entry.StartedAt)
                    .FirstOrDefault();

                return Task.FromResult(latest);
            }

            public Task<(IEnumerable<BackupAuditEntry> Items, int TotalCount)> GetHistoryAsync(BackupLevel? level, int pageNumber, int pageSize, CancellationToken cToken)
            {
                IEnumerable<BackupAuditEntry> filtered = level.HasValue
                    ? Entries.Where(entry => entry.Level == level.Value)
                    : Entries;

                List<BackupAuditEntry> items = filtered
                    .OrderByDescending(entry => entry.StartedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Task.FromResult<(IEnumerable<BackupAuditEntry>, int)>((items, filtered.Count()));
            }

            public Task AddAsync(BackupAuditEntry entry, CancellationToken cToken)
            {
                Entries.Add(entry);
                return Task.CompletedTask;
            }

            public Task SaveChangesAsync(CancellationToken cToken)
            {
                return Task.CompletedTask;
            }
        }

        //Servicio de proceso de backup en memoria: no ejecuta rsync/pg_dump reales, solo simula su
        //resultado y registra con qué argumentos fue invocado para poder comprobarlos en los tests.
        private sealed class FakeBackupProcessService : IBackupProcessService
        {
            public BackupProcessResult RsyncResult { get; set; } = new BackupProcessResult(true, 0, 1000, null);
            public BackupProcessResult PgDumpResult { get; set; } = new BackupProcessResult(true, 0, 500, null);

            public bool RsyncWasCalled { get; private set; }
            public string? LastRsyncSourcePath { get; private set; }
            public string? LastRsyncDestinationPath { get; private set; }
            public string? LastRsyncLinkDestPath { get; private set; }

            public bool PgDumpWasCalled { get; private set; }
            public string? LastPgDumpConnectionString { get; private set; }
            public string? LastPgDumpOutputFilePath { get; private set; }

            public Task<BackupProcessResult> RunRsyncAsync(string sourcePath, string destinationPath, string? linkDestPath, CancellationToken cToken)
            {
                RsyncWasCalled = true;
                LastRsyncSourcePath = sourcePath;
                LastRsyncDestinationPath = destinationPath;
                LastRsyncLinkDestPath = linkDestPath;

                //Simula el efecto de rsync sobre el disco para que los tests de rotación tengan contenido real que comprobar
                Directory.CreateDirectory(destinationPath);
                File.WriteAllText(Path.Combine(destinationPath, "marker.txt"), "content");

                return Task.FromResult(RsyncResult);
            }

            public Task<BackupProcessResult> RunPgDumpAsync(string connectionString, string outputFilePath, CancellationToken cToken)
            {
                PgDumpWasCalled = true;
                LastPgDumpConnectionString = connectionString;
                LastPgDumpOutputFilePath = outputFilePath;

                return Task.FromResult(PgDumpResult);
            }
        }
        #endregion
    }
}
