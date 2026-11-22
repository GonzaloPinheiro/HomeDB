using HomeDB.Application.Options;
using HomeDB.Application.Services;
using HomeDB.Domain.Common;
using HomeDB.Domain.Common.Enums;
using HomeDB.Domain.Common.RecordsInfrastructure;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Exceptions;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Domain.Interfaces.Services;
using HomeDB.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace HomeDB.Tests.Unit.Application
{
    //Tests unitarios de BackupService: rotación de directorios en disco, uso de link-dest de rsync,
    //propagación de fallos rsync/pg_dump, actualización de la auditoría y comprobación de los discos.
    //No hay rsync ni pg_dump reales de por medio: se usan fakes en memoria para el repositorio y el proceso
    //de backup, y directorios temporales reales en disco (con su fichero marcador, como un disco real) para
    //poder comprobar la rotación y la disponibilidad tal cual las hace el código.
    public sealed class BackupServiceTests : IDisposable
    {
        //Variables y objetos
        private readonly string _rootPath = Directory.CreateTempSubdirectory("HomeDB.Tests.Backup").FullName;
        private readonly string _m2Path;
        private readonly string _hdd2Path;
        private readonly string _sourceDirectory;
        private readonly string _dailyDirectory;
        private readonly List<DiskOptions> _disks = new List<DiskOptions>();
        private readonly FakeBackupAuditRepository _repository = new FakeBackupAuditRepository();
        private readonly FakeBackupProcessService _processService = new FakeBackupProcessService();

        //Constructores
        public BackupServiceTests()
        {
            //Disco principal con la ubicación "main" (se copia) y disco de backups con la ubicación "backups"
            _m2Path = Path.Combine(_rootPath, "m2-1");
            _hdd2Path = Path.Combine(_rootPath, "hdd2");
            _sourceDirectory = Path.Combine(_m2Path, "files");
            _dailyDirectory = Path.Combine(_hdd2Path, "backups", "daily");
            Directory.CreateDirectory(_sourceDirectory);
            Directory.CreateDirectory(_dailyDirectory);

            AddDisk("m2-1", _m2Path, new StorageLocationOptions
            {
                Key = "main", Usage = LocationUsage.Personal, IsDefault = true, SubPath = "files", IncludeInBackup = true
            });
            AddDisk("hdd2", _hdd2Path, new StorageLocationOptions
            {
                Key = "backups", Usage = LocationUsage.Backups, IsDefault = true, SubPath = "backups"
            });
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

            //El origen lleva barra final (rsync copia su contenido) y el destino es una subcarpeta con la clave de la ubicación
            Assert.Equal(_sourceDirectory + Path.DirectorySeparatorChar, _processService.LastRsyncSourcePath);
            Assert.Equal(Path.Combine(currentPath, "main"), _processService.LastRsyncDestinationPath);

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
            Assert.Equal(Path.Combine(previousPath, "main"), _processService.LastRsyncLinkDestPath);

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
            Assert.Equal("[main] rsync error", entry.ErrorMessage); //Indica qué ubicación falló
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
        public async Task RunDailyBackupAsync_WhenBackupsDiskUnavailable_ThrowsAndTouchesNothing()
        {
            BackupAuditEntry existingEntry = SeedExistingBackup();
            File.Delete(Path.Combine(_hdd2Path, StorageAvailabilityService.MarkerFileName));
            BackupService service = CreateService();

            StorageLocationUnavailableException exception = await Assert.ThrowsAsync<StorageLocationUnavailableException>(
                () => service.RunDailyBackupAsync(CancellationToken.None));

            Assert.Equal("hdd2", exception.DiskKey);
            AssertNothingWasTouched(existingEntry);
        }

        [Fact]
        public async Task RunDailyBackupAsync_WhenSourceDiskUnavailable_ThrowsBeforeRotatingTheLastGoodBackup()
        {
            //Caso que justifica la comprobación: sin ella rsync copiaría una carpeta vacía y la rotación
            //descartaría la última copia buena
            BackupAuditEntry existingEntry = SeedExistingBackup();
            File.Delete(Path.Combine(_m2Path, StorageAvailabilityService.MarkerFileName));
            BackupService service = CreateService();

            StorageLocationUnavailableException exception = await Assert.ThrowsAsync<StorageLocationUnavailableException>(
                () => service.RunDailyBackupAsync(CancellationToken.None));

            Assert.Equal("m2-1", exception.DiskKey);
            AssertNothingWasTouched(existingEntry);
        }

        [Fact]
        public async Task RunDailyBackupAsync_WithSeveralLocations_CopiesEachIntoItsOwnSubfolderAndSumsSizes()
        {
            string otherDiskPath = Path.Combine(_rootPath, "hdd3");
            Directory.CreateDirectory(Path.Combine(otherDiskPath, "files"));
            AddDisk("hdd3", otherDiskPath, new StorageLocationOptions
            {
                Key = "main-2", Usage = LocationUsage.Personal, IsDefault = false, SubPath = "files", IncludeInBackup = true
            });
            BackupService service = CreateService();

            await service.RunDailyBackupAsync(CancellationToken.None);

            string currentPath = Path.Combine(_dailyDirectory, "backup_actual");
            Assert.Equal(
                new[] { Path.Combine(currentPath, "main"), Path.Combine(currentPath, "main-2") },
                _processService.RsyncCalls.Select(call => call.Destination).ToArray());

            BackupAuditEntry entry = Assert.Single(_repository.Entries);
            Assert.Equal(BackupStatus.Success, entry.Status);
            Assert.Equal(2 * _processService.RsyncResult.OutputSizeBytes, entry.FilesBackedUpSizeBytes);
        }

        [Fact]
        public async Task RunDailyBackupAsync_WhenFirstLocationFails_DoesNotCopyTheRestNorDumpTheDatabase()
        {
            string otherDiskPath = Path.Combine(_rootPath, "hdd3");
            Directory.CreateDirectory(Path.Combine(otherDiskPath, "files"));
            AddDisk("hdd3", otherDiskPath, new StorageLocationOptions
            {
                Key = "main-2", Usage = LocationUsage.Personal, IsDefault = false, SubPath = "files", IncludeInBackup = true
            });
            _processService.RsyncResult = new BackupProcessResult(false, 23, 0, "partial transfer");
            BackupService service = CreateService();

            await service.RunDailyBackupAsync(CancellationToken.None);

            Assert.Single(_processService.RsyncCalls);
            Assert.False(_processService.PgDumpWasCalled);
            Assert.Equal(BackupStatus.Failed, Assert.Single(_repository.Entries).Status);
        }

        [Fact]
        public async Task RunDailyBackupAsync_WhenLocationFolderDoesNotExistYet_SkipsItAndStillDumpsTheDatabase()
        {
            //Una ubicación sin ningún archivo subido todavía no tiene carpeta: no hay nada que copiar
            Directory.Delete(_sourceDirectory);
            BackupService service = CreateService();

            await service.RunDailyBackupAsync(CancellationToken.None);

            Assert.False(_processService.RsyncWasCalled);
            Assert.True(_processService.PgDumpWasCalled);
            Assert.Equal(BackupStatus.Success, Assert.Single(_repository.Entries).Status);
        }

        //Declara un disco con su marcador (clave del disco como contenido) y las ubicaciones indicadas.
        private void AddDisk(string key, string path, params StorageLocationOptions[] locations)
        {
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, StorageAvailabilityService.MarkerFileName), key);

            _disks.Add(new DiskOptions
            {
                Key = key, Path = path, StartupPolicy = DiskStartupPolicy.Optional, Locations = locations.ToList()
            });
        }

        //Deja un backup anterior con contenido y su registro de auditoría activo, como el de una ejecución previa.
        private BackupAuditEntry SeedExistingBackup()
        {
            string currentPath = Path.Combine(_dailyDirectory, "backup_actual");
            Directory.CreateDirectory(currentPath);
            File.WriteAllText(Path.Combine(currentPath, "good-backup.txt"), "content");

            BackupAuditEntry existingEntry = new BackupAuditEntry
            {
                Level = BackupLevel.Daily,
                Status = BackupStatus.Success,
                StartedAt = DateTime.UtcNow.AddDays(-1),
                CompletedAt = DateTime.UtcNow.AddDays(-1),
                BackupPath = currentPath
            };
            _repository.Entries.Add(existingEntry);

            return existingEntry;
        }

        //Comprueba que un backup abortado no rotó nada, no ejecutó procesos ni tocó el registro de auditoría.
        private void AssertNothingWasTouched(BackupAuditEntry existingEntry)
        {
            Assert.True(File.Exists(Path.Combine(_dailyDirectory, "backup_actual", "good-backup.txt")));
            Assert.False(Directory.Exists(Path.Combine(_dailyDirectory, "backup_anterior")));
            Assert.False(_processService.RsyncWasCalled);
            Assert.False(_processService.PgDumpWasCalled);
            Assert.Single(_repository.Entries);
            Assert.Null(existingEntry.DeletedAt);
            Assert.NotNull(existingEntry.BackupPath);
        }

        //Crea un BackupService con los fakes del test y la configuración indicada. Los discos y ubicaciones
        //se leen de _disks, que los tests pueden ampliar antes de llamar a este método.
        private BackupService CreateService(string connectionString = "Host=localhost;Database=homedb;Username=u;Password=p")
        {
            IOptions<DatabaseOptions> databaseOptions = Options.Create(new DatabaseOptions
            {
                PostgreSQL_HomeDB = connectionString
            });

            IOptions<StorageOptions> storageOptions = Options.Create(new StorageOptions { Disks = _disks });
            StorageLocationResolver locationResolver = new StorageLocationResolver(storageOptions);
            StorageAvailabilityService availabilityService = new StorageAvailabilityService(storageOptions);

            return new BackupService(_repository, _processService, databaseOptions, locationResolver, availabilityService);
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
            public List<(string Source, string Destination, string? LinkDest)> RsyncCalls { get; } = new List<(string, string, string?)>();
            public string? LastRsyncSourcePath { get; private set; }
            public string? LastRsyncDestinationPath { get; private set; }
            public string? LastRsyncLinkDestPath { get; private set; }

            public bool PgDumpWasCalled { get; private set; }
            public string? LastPgDumpConnectionString { get; private set; }
            public string? LastPgDumpOutputFilePath { get; private set; }

            public Task<BackupProcessResult> RunRsyncAsync(string sourcePath, string destinationPath, string? linkDestPath, CancellationToken cToken)
            {
                RsyncWasCalled = true;
                RsyncCalls.Add((sourcePath, destinationPath, linkDestPath));
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
