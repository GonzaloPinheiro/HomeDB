using HomeDB.Application.Options;
using HomeDB.Domain.Common;
using HomeDB.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace HomeDB.Tests.Unit.Infrastructure
{
    //Tests unitarios de StorageLocationResolver.GetBackupLocationKeys: qué ubicaciones entran en el backup.
    public sealed class StorageLocationResolverTests
    {
        [Fact]
        public void GetBackupLocationKeys_ReturnsOnlyMarkedLocations_AcrossDisks_InConfigurationOrder()
        {
            StorageLocationResolver resolver = CreateResolver(
                CreateDisk("m2-1",
                    CreateLocation("main", LocationUsage.Personal, includeInBackup: true),
                    CreateLocation("temp", LocationUsage.Temp, includeInBackup: false)),
                CreateDisk("hdd1",
                    CreateLocation("media", LocationUsage.Media, includeInBackup: false)),
                CreateDisk("hdd3",
                    CreateLocation("main-2", LocationUsage.Personal, includeInBackup: true)));

            IReadOnlyCollection<string> keys = resolver.GetBackupLocationKeys();

            Assert.Equal(new[] { "main", "main-2" }, keys.ToArray());
        }

        [Fact]
        public void GetBackupLocationKeys_WhenNoneIsMarked_ReturnsEmpty()
        {
            StorageLocationResolver resolver = CreateResolver(
                CreateDisk("m2-1", CreateLocation("main", LocationUsage.Personal, includeInBackup: false)));

            Assert.Empty(resolver.GetBackupLocationKeys());
        }

        //Helpers
        private static StorageLocationOptions CreateLocation(string key, LocationUsage usage, bool includeInBackup)
        {
            return new StorageLocationOptions { Key = key, Usage = usage, IsDefault = true, SubPath = key, IncludeInBackup = includeInBackup };
        }

        private static DiskOptions CreateDisk(string key, params StorageLocationOptions[] locations)
        {
            return new DiskOptions { Key = key, Path = "/" + key, StartupPolicy = DiskStartupPolicy.Optional, Locations = locations.ToList() };
        }

        private static StorageLocationResolver CreateResolver(params DiskOptions[] disks)
        {
            return new StorageLocationResolver(Options.Create(new StorageOptions { Disks = disks.ToList() }));
        }
    }
}
