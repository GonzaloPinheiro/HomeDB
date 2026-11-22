using HomeDB.Application.DTOs;
using HomeDB.Application.Options;
using HomeDB.Application.Services;
using HomeDB.Domain.Common;
using HomeDB.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace HomeDB.Tests.Unit.Application
{
    //Tests unitarios de StorageStatusService con el servicio de disponibilidad real sobre directorios temporales:
    //lo que se comprueba es justamente cómo se combinan configuración, marcador y capacidad.
    public sealed class StorageStatusServiceTests : IDisposable
    {
        //Variables y objetos
        private readonly string _rootPath = Directory.CreateTempSubdirectory("HomeDB.Tests.Status").FullName;

        //Limpia el directorio temporal usado por el test
        public void Dispose()
        {
            if (Directory.Exists(_rootPath))
                Directory.Delete(_rootPath, recursive: true);
        }

        [Fact]
        public void GetLocationsStatus_AvailableDisk_ReturnsOneRowPerLocationWithCapacityAndConfig()
        {
            DiskOptions disk = CreateDisk("m2-1", withMarker: true, minFreeBytes: 1000,
                new StorageLocationOptions { Key = "main", Usage = LocationUsage.Personal, IsDefault = true, SubPath = "files", MaxUsedBytes = 5000 },
                new StorageLocationOptions { Key = "temp", Usage = LocationUsage.Temp, IsDefault = true, SubPath = "temp-uploads" });
            StorageStatusService service = CreateService(disk);

            IReadOnlyList<StorageLocationStatusDto> rows = service.GetLocationsStatus();

            Assert.Equal(2, rows.Count);

            StorageLocationStatusDto main = rows[0];
            Assert.Equal("main", main.LocationKey);
            Assert.Equal("m2-1", main.DiskKey);
            Assert.Equal(LocationUsage.Personal, main.Usage);
            Assert.True(main.IsDefault);
            Assert.True(main.IsAvailable);
            Assert.True(main.IsWritable);
            Assert.Null(main.Reason);
            Assert.NotNull(main.TotalBytes);
            Assert.Equal(main.TotalBytes - main.FreeBytes, main.UsedBytes);
            Assert.Equal(1000, main.MinFreeBytes);
            Assert.Equal(5000, main.MaxUsedBytes);

            //El límite de uso es de la ubicación; la disponibilidad y la capacidad son las del disco compartido
            StorageLocationStatusDto temp = rows[1];
            Assert.Equal("temp", temp.LocationKey);
            Assert.Null(temp.MaxUsedBytes);
            Assert.Equal(main.TotalBytes, temp.TotalBytes);
        }

        [Fact]
        public void GetLocationsStatus_DiskWithoutMarker_IsUnavailableWithReasonAndNoCapacity()
        {
            //Estado guardado tras una comprobación completa, como la que hace el vigilante
            DiskOptions disk = CreateDisk("hdd1", withMarker: false, minFreeBytes: 0,
                new StorageLocationOptions { Key = "media", Usage = LocationUsage.Media, IsDefault = true, SubPath = "media" });
            StorageAvailabilityService availability = CreateAvailability(disk);
            availability.CheckAllDisks();
            StorageStatusService service = CreateService(availability, disk);

            StorageLocationStatusDto row = Assert.Single(service.GetLocationsStatus());

            Assert.False(row.IsAvailable);
            Assert.False(row.IsWritable);
            Assert.Equal("marker file is missing", row.Reason);
            Assert.Null(row.TotalBytes);
            Assert.Null(row.UsedBytes);
            Assert.Null(row.FreeBytes);
        }

        [Fact]
        public void GetLocationsStatus_WithoutAnyPreviousCheck_ChecksTheDiskInsteadOfFailing()
        {
            DiskOptions disk = CreateDisk("m2-1", withMarker: true, minFreeBytes: 0,
                new StorageLocationOptions { Key = "main", Usage = LocationUsage.Personal, IsDefault = true, SubPath = "files" });
            StorageStatusService service = CreateService(disk);

            StorageLocationStatusDto row = Assert.Single(service.GetLocationsStatus());

            Assert.True(row.IsAvailable);
            Assert.True(row.IsWritable);
        }

        [Fact]
        public void GetLocationsStatus_SeveralDisks_KeepsConfigurationOrder()
        {
            DiskOptions first = CreateDisk("m2-1", withMarker: true, minFreeBytes: 0,
                new StorageLocationOptions { Key = "main", Usage = LocationUsage.Personal, IsDefault = true, SubPath = "files" });
            DiskOptions second = CreateDisk("hdd1", withMarker: false, minFreeBytes: 0,
                new StorageLocationOptions { Key = "media", Usage = LocationUsage.Media, IsDefault = true, SubPath = "media" });
            StorageStatusService service = CreateService(first, second);

            IReadOnlyList<StorageLocationStatusDto> rows = service.GetLocationsStatus();

            Assert.Equal(new[] { "main", "media" }, rows.Select(r => r.LocationKey).ToArray());
            Assert.True(rows[0].IsAvailable);
            Assert.False(rows[1].IsAvailable);
        }

        //Helpers
        private DiskOptions CreateDisk(string key, bool withMarker, long minFreeBytes, params StorageLocationOptions[] locations)
        {
            string path = Path.Combine(_rootPath, key);
            Directory.CreateDirectory(path);

            if (withMarker)
                File.WriteAllText(Path.Combine(path, StorageAvailabilityService.MarkerFileName), key);

            return new DiskOptions
            {
                Key = key,
                Path = path,
                StartupPolicy = DiskStartupPolicy.Optional,
                MinFreeBytes = minFreeBytes,
                Locations = locations.ToList()
            };
        }

        private static StorageAvailabilityService CreateAvailability(params DiskOptions[] disks)
        {
            return new StorageAvailabilityService(Options.Create(new StorageOptions { Disks = disks.ToList() }));
        }

        private static StorageStatusService CreateService(params DiskOptions[] disks)
        {
            return CreateService(CreateAvailability(disks), disks);
        }

        private static StorageStatusService CreateService(StorageAvailabilityService availability, params DiskOptions[] disks)
        {
            return new StorageStatusService(Options.Create(new StorageOptions { Disks = disks.ToList() }), availability);
        }
    }
}
