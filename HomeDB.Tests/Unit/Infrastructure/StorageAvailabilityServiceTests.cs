using HomeDB.Application.Options;
using HomeDB.Domain.Common;
using HomeDB.Domain.Common.RecordsInfrastructure;
using HomeDB.Domain.Exceptions;
using HomeDB.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace HomeDB.Tests.Unit.Infrastructure
{
    //Tests unitarios de StorageAvailabilityService sobre directorios temporales reales: el marcador y la
    //sonda de escritura son operaciones de fichero, y simularlas con dobles no probaría nada útil.
    //No se cubre el fallo de la sonda de escritura (disco en solo lectura): no hay forma portable entre
    //Windows y Linux de provocarlo en un directorio temporal.
    public sealed class StorageAvailabilityServiceTests : IDisposable
    {
        private const string DiskKey = "hdd1";

        //Variables y objetos
        private readonly string _rootPath = Directory.CreateTempSubdirectory("HomeDB.Tests.Availability").FullName;
        private readonly string _diskPath;

        //Constructores
        public StorageAvailabilityServiceTests()
        {
            _diskPath = Path.Combine(_rootPath, "hdd1");
            Directory.CreateDirectory(_diskPath);
        }

        //Limpia el directorio temporal usado por el test
        public void Dispose()
        {
            if (Directory.Exists(_rootPath))
                Directory.Delete(_rootPath, recursive: true);
        }

        [Fact]
        public void EnsureAvailable_WithMatchingMarker_DoesNotThrow()
        {
            WriteMarker(DiskKey);
            StorageAvailabilityService service = CreateService();

            service.EnsureAvailable(DiskKey, forWrite: true);
        }

        [Fact]
        public void EnsureAvailable_MarkerWithTrailingNewline_DoesNotThrow()
        {
            //"echo hdd1 > fichero" deja un salto de línea al final
            WriteMarker(DiskKey + Environment.NewLine);
            StorageAvailabilityService service = CreateService();

            service.EnsureAvailable(DiskKey, forWrite: false);
        }

        [Fact]
        public void EnsureAvailable_MissingMarker_ThrowsWithGenericReason()
        {
            StorageAvailabilityService service = CreateService();

            StorageLocationUnavailableException exception = Assert.Throws<StorageLocationUnavailableException>(
                () => service.EnsureAvailable(DiskKey, forWrite: false));

            Assert.Equal(DiskKey, exception.DiskKey);
            Assert.Equal("marker file is missing", exception.Reason);
        }

        [Fact]
        public void EnsureAvailable_MarkerOfAnotherDisk_Throws()
        {
            WriteMarker("hdd2");
            StorageAvailabilityService service = CreateService();

            StorageLocationUnavailableException exception = Assert.Throws<StorageLocationUnavailableException>(
                () => service.EnsureAvailable(DiskKey, forWrite: false));

            Assert.Equal("marker file does not match the disk", exception.Reason);
        }

        [Fact]
        public void EnsureAvailable_DiskPathDoesNotExist_Throws()
        {
            WriteMarker(DiskKey);
            Directory.Delete(_diskPath, recursive: true);
            StorageAvailabilityService service = CreateService();

            StorageLocationUnavailableException exception = Assert.Throws<StorageLocationUnavailableException>(
                () => service.EnsureAvailable(DiskKey, forWrite: false));

            Assert.Equal("disk path is not accessible", exception.Reason);
        }

        [Fact]
        public void EnsureAvailable_Failure_DoesNotLeakHostPaths()
        {
            StorageAvailabilityService service = CreateService();

            StorageLocationUnavailableException exception = Assert.Throws<StorageLocationUnavailableException>(
                () => service.EnsureAvailable(DiskKey, forWrite: true));

            //El mensaje llega tal cual al cliente en la respuesta 503
            Assert.DoesNotContain(_rootPath, exception.Message);
        }

        [Fact]
        public void EnsureAvailable_DiskRemovedAfterCheck_DetectsItLive()
        {
            //El marcador se lee en cada operación: un disco que se desmonta se detecta sin esperar al vigilante
            WriteMarker(DiskKey);
            StorageAvailabilityService service = CreateService();
            service.CheckDisk(DiskKey);

            File.Delete(Path.Combine(_diskPath, StorageAvailabilityService.MarkerFileName));

            Assert.Throws<StorageLocationUnavailableException>(() => service.EnsureAvailable(DiskKey, forWrite: false));
        }

        [Fact]
        public void EnsureAvailable_UnknownDiskKey_ThrowsInvalidOperation()
        {
            StorageAvailabilityService service = CreateService();

            Assert.Throws<InvalidOperationException>(() => service.EnsureAvailable("nope", forWrite: false));
        }

        [Fact]
        public void CheckDisk_HealthyDisk_IsAvailableAndWritable_AndLeavesNoProbeFile()
        {
            WriteMarker(DiskKey);
            StorageAvailabilityService service = CreateService();

            DiskStatus status = service.CheckDisk(DiskKey);

            Assert.True(status.IsAvailable);
            Assert.True(status.IsWritable);
            Assert.Null(status.Reason);
            Assert.Equal(new[] { StorageAvailabilityService.MarkerFileName },
                Directory.GetFiles(_diskPath).Select(Path.GetFileName).ToArray());
        }

        [Fact]
        public void CheckDisk_MissingMarker_IsNotAvailable_AndWritesNothing()
        {
            StorageAvailabilityService service = CreateService();

            DiskStatus status = service.CheckDisk(DiskKey);

            Assert.False(status.IsAvailable);
            Assert.False(status.IsWritable);
            Assert.Equal("marker file is missing", status.Reason);
            //Sin marcador válido no se debe escribir ni la sonda
            Assert.Empty(Directory.GetFiles(_diskPath));
        }

        [Fact]
        public void GetCachedStatus_BeforeAndAfterCheck()
        {
            WriteMarker(DiskKey);
            StorageAvailabilityService service = CreateService();

            Assert.Null(service.GetCachedStatus(DiskKey));
            Assert.Empty(service.GetCachedStatuses());

            service.CheckDisk(DiskKey);

            Assert.NotNull(service.GetCachedStatus(DiskKey));
            Assert.Single(service.GetCachedStatuses());
        }

        [Fact]
        public void CheckAllDisks_ReportsEveryConfiguredDisk()
        {
            WriteMarker(DiskKey);
            string otherDiskPath = Path.Combine(_rootPath, "hdd2");
            Directory.CreateDirectory(otherDiskPath);
            StorageAvailabilityService service = CreateService(
                new DiskOptions { Key = DiskKey, Path = _diskPath },
                new DiskOptions { Key = "hdd2", Path = otherDiskPath });

            IReadOnlyList<DiskStatus> statuses = service.CheckAllDisks();

            Assert.Equal(2, statuses.Count);
            Assert.True(statuses.Single(s => s.DiskKey == DiskKey).IsAvailable);
            Assert.False(statuses.Single(s => s.DiskKey == "hdd2").IsAvailable);
        }

        [Fact]
        public void GetCapacity_ConfirmedDisk_ReturnsConsistentFigures()
        {
            WriteMarker(DiskKey);
            StorageAvailabilityService service = CreateService();

            DiskCapacity? capacity = service.GetCapacity(DiskKey);

            Assert.NotNull(capacity);
            Assert.True(capacity!.TotalBytes > 0);
            Assert.InRange(capacity.FreeBytes, 0, capacity.TotalBytes);
            Assert.Equal(capacity.TotalBytes - capacity.FreeBytes, capacity.UsedBytes);
        }

        [Fact]
        public void GetCapacity_DiskWithoutMarker_ReturnsNull_InsteadOfTheContainingDisksCapacity()
        {
            //La carpeta existe y vive en un disco real con capacidad, pero sin marcador no es el disco esperado
            StorageAvailabilityService service = CreateService();

            Assert.Null(service.GetCapacity(DiskKey));
        }

        [Fact]
        public void GetCapacity_UnknownDiskKey_ThrowsInvalidOperation()
        {
            StorageAvailabilityService service = CreateService();

            Assert.Throws<InvalidOperationException>(() => service.GetCapacity("nope"));
        }

        //Helpers
        private void WriteMarker(string content)
        {
            File.WriteAllText(Path.Combine(_diskPath, StorageAvailabilityService.MarkerFileName), content);
        }

        private StorageAvailabilityService CreateService()
        {
            return CreateService(new DiskOptions { Key = DiskKey, Path = _diskPath });
        }

        private static StorageAvailabilityService CreateService(params DiskOptions[] disks)
        {
            foreach (DiskOptions disk in disks)
                disk.StartupPolicy = DiskStartupPolicy.Optional;

            StorageOptions options = new StorageOptions { Disks = disks.ToList() };
            return new StorageAvailabilityService(Options.Create(options));
        }
    }
}
