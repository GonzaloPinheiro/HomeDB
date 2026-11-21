using HomeDB.Application.Options;
using HomeDB.Domain.Common;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Infrastructure.Observability;
using HomeDB.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace HomeDB.Tests.Unit.Infrastructure
{
    //Tests unitarios de la política de arranque de discos (Required/Optional). Usan directorios temporales
    //reales para el marcador y un Logger real con una cola falsa que captura los avisos registrados.
    public sealed class StorageStartupValidatorTests : IDisposable
    {
        //Variables y objetos
        private readonly string _rootPath = Directory.CreateTempSubdirectory("HomeDB.Tests.Startup").FullName;
        private readonly FakeLogQueue _logQueue = new FakeLogQueue();

        //Limpia el directorio temporal usado por el test
        public void Dispose()
        {
            if (Directory.Exists(_rootPath))
                Directory.Delete(_rootPath, recursive: true);
        }

        [Fact]
        public async Task ValidateAsync_AllDisksAvailable_DoesNotThrowNorLog()
        {
            DiskOptions required = CreateDisk("m2-1", DiskStartupPolicy.Required, withMarker: true);
            DiskOptions optional = CreateDisk("hdd1", DiskStartupPolicy.Optional, withMarker: true);

            await CreateValidator(required, optional).ValidateAsync();

            Assert.Empty(_logQueue.Entries);
        }

        [Fact]
        public async Task ValidateAsync_RequiredDiskWithoutMarker_ThrowsAndNamesTheDisk()
        {
            DiskOptions required = CreateDisk("m2-1", DiskStartupPolicy.Required, withMarker: false);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateValidator(required).ValidateAsync());

            Assert.Contains("m2-1", exception.Message);
            Assert.Contains("marker file is missing", exception.Message);
        }

        [Fact]
        public async Task ValidateAsync_OptionalDiskWithoutMarker_DoesNotThrow_AndLogsCritical()
        {
            DiskOptions required = CreateDisk("m2-1", DiskStartupPolicy.Required, withMarker: true);
            DiskOptions optional = CreateDisk("hdd1", DiskStartupPolicy.Optional, withMarker: false);

            await CreateValidator(required, optional).ValidateAsync();

            LogEntry entry = Assert.Single(_logQueue.Entries);
            Assert.Equal("Critical", entry.Level);
            Assert.Contains("hdd1", entry.Message);
            Assert.Contains("marker file is missing", entry.Message);
        }

        [Fact]
        public async Task ValidateAsync_RequiredFailsAndOptionalFails_ThrowsOnlyAboutRequired()
        {
            DiskOptions required = CreateDisk("m2-1", DiskStartupPolicy.Required, withMarker: false);
            DiskOptions optional = CreateDisk("hdd1", DiskStartupPolicy.Optional, withMarker: false);

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateValidator(required, optional).ValidateAsync());

            Assert.Contains("m2-1", exception.Message);
            Assert.DoesNotContain("hdd1", exception.Message);
        }

        [Fact]
        public async Task ValidateAsync_DiskMarkerOfAnotherDisk_IsTreatedAsUnavailable()
        {
            //Caso real: el HDD equivocado montado en el punto de montaje de otro
            DiskOptions required = CreateDisk("m2-1", DiskStartupPolicy.Required, withMarker: false);
            File.WriteAllText(Path.Combine(required.Path, StorageAvailabilityService.MarkerFileName), "hdd1");

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateValidator(required).ValidateAsync());

            Assert.Contains("marker file does not match the disk", exception.Message);
        }

        //Helpers
        private DiskOptions CreateDisk(string key, DiskStartupPolicy policy, bool withMarker)
        {
            string path = Path.Combine(_rootPath, key);
            Directory.CreateDirectory(path);

            if (withMarker)
                File.WriteAllText(Path.Combine(path, StorageAvailabilityService.MarkerFileName), key);

            return new DiskOptions { Key = key, Path = path, StartupPolicy = policy };
        }

        private StorageStartupValidator CreateValidator(params DiskOptions[] disks)
        {
            IOptions<StorageOptions> options = Options.Create(new StorageOptions { Disks = disks.ToList() });
            StorageAvailabilityService availability = new StorageAvailabilityService(options);

            Logger logger = _logQueue.CreateLogger(Path.Combine(_rootPath, "failed-logs"));

            return new StorageStartupValidator(availability, options, logger);
        }
    }
}
