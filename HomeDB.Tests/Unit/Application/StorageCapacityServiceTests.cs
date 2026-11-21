using HomeDB.Application.Options;
using HomeDB.Application.Services;
using HomeDB.Domain.Common;
using HomeDB.Domain.Common.RecordsInfrastructure;
using HomeDB.Domain.Entities;
using HomeDB.Domain.Exceptions;
using HomeDB.Domain.Interfaces.Repositories;
using HomeDB.Domain.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace HomeDB.Tests.Unit.Application
{
    //Tests unitarios de StorageCapacityService. La capacidad del disco y el uso registrado se controlan con dobles,
    //para poder probar los límites exactos (justo en el borde) sin depender del espacio real de la máquina.
    public sealed class StorageCapacityServiceTests
    {
        private const long Total = 1000;

        //Variables y objetos
        private readonly FakeAvailabilityService _availability = new FakeAvailabilityService();
        private readonly FakeFileItemRepository _repository = new FakeFileItemRepository();

        [Fact]
        public async Task EnsureFitsAsync_WithRoomAndNoLimits_DoesNotThrow()
        {
            _availability.Capacity = new DiskCapacity(Total, FreeBytes: 500);
            StorageCapacityService service = CreateService(minFreeBytes: 0, maxUsedBytes: null);

            await service.EnsureFitsAsync("main", 500, CancellationToken.None);
        }

        [Fact]
        public async Task EnsureFitsAsync_LeavingExactlyTheMinimumFree_IsAllowed()
        {
            _availability.Capacity = new DiskCapacity(Total, FreeBytes: 500);
            StorageCapacityService service = CreateService(minFreeBytes: 100, maxUsedBytes: null);

            //Quedarían justo 100 libres: el mínimo no se rebasa
            await service.EnsureFitsAsync("main", 400, CancellationToken.None);
        }

        [Fact]
        public async Task EnsureFitsAsync_GoingOneByteBelowTheMinimumFree_ThrowsFullWithFreeSpaceReason()
        {
            _availability.Capacity = new DiskCapacity(Total, FreeBytes: 500);
            StorageCapacityService service = CreateService(minFreeBytes: 100, maxUsedBytes: null);

            StorageLocationFullException exception = await Assert.ThrowsAsync<StorageLocationFullException>(
                () => service.EnsureFitsAsync("main", 401, CancellationToken.None));

            Assert.Equal("main", exception.LocationKey);
            Assert.Equal("not enough free space", exception.Reason);
        }

        [Fact]
        public async Task EnsureFitsAsync_ReachingExactlyTheUsageLimit_IsAllowed()
        {
            _availability.Capacity = new DiskCapacity(Total, FreeBytes: 900);
            _repository.UsedBytes = 300;
            StorageCapacityService service = CreateService(minFreeBytes: 0, maxUsedBytes: 400);

            await service.EnsureFitsAsync("main", 100, CancellationToken.None);
        }

        [Fact]
        public async Task EnsureFitsAsync_ExceedingTheUsageLimitByOneByte_ThrowsFullWithUsageReason()
        {
            _availability.Capacity = new DiskCapacity(Total, FreeBytes: 900);
            _repository.UsedBytes = 300;
            StorageCapacityService service = CreateService(minFreeBytes: 0, maxUsedBytes: 400);

            StorageLocationFullException exception = await Assert.ThrowsAsync<StorageLocationFullException>(
                () => service.EnsureFitsAsync("main", 101, CancellationToken.None));

            Assert.Equal("usage limit reached", exception.Reason);
        }

        [Fact]
        public async Task EnsureFitsAsync_WithoutUsageLimit_DoesNotQueryTheDatabase()
        {
            _availability.Capacity = new DiskCapacity(Total, FreeBytes: 900);
            StorageCapacityService service = CreateService(minFreeBytes: 0, maxUsedBytes: null);

            await service.EnsureFitsAsync("main", 100, CancellationToken.None);

            Assert.Equal(0, _repository.UsedBytesQueries);
        }

        [Fact]
        public async Task EnsureFitsAsync_WhenFreeSpaceIsInsufficient_DoesNotQueryTheDatabase()
        {
            _availability.Capacity = new DiskCapacity(Total, FreeBytes: 50);
            StorageCapacityService service = CreateService(minFreeBytes: 0, maxUsedBytes: 400);

            await Assert.ThrowsAsync<StorageLocationFullException>(() => service.EnsureFitsAsync("main", 100, CancellationToken.None));

            Assert.Equal(0, _repository.UsedBytesQueries);
        }

        [Fact]
        public async Task EnsureFitsAsync_WhenCapacityCannotBeRead_FailsClosedAsUnavailable()
        {
            _availability.Capacity = null;
            StorageCapacityService service = CreateService(minFreeBytes: 0, maxUsedBytes: null);

            StorageLocationUnavailableException exception = await Assert.ThrowsAsync<StorageLocationUnavailableException>(
                () => service.EnsureFitsAsync("main", 1, CancellationToken.None));

            Assert.Equal("m2-1", exception.DiskKey);
            Assert.Equal("capacity could not be read", exception.Reason);
        }

        [Fact]
        public async Task EnsureFitsAsync_UnknownLocationKey_ThrowsInvalidOperation()
        {
            StorageCapacityService service = CreateService(minFreeBytes: 0, maxUsedBytes: null);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.EnsureFitsAsync("nope", 1, CancellationToken.None));
        }

        //Helpers
        private StorageCapacityService CreateService(long minFreeBytes, long? maxUsedBytes)
        {
            DiskOptions disk = new DiskOptions
            {
                Key = "m2-1",
                Path = "/storage",
                StartupPolicy = DiskStartupPolicy.Required,
                MinFreeBytes = minFreeBytes,
                Locations = new List<StorageLocationOptions>
                {
                    new StorageLocationOptions { Key = "main", Usage = LocationUsage.Personal, IsDefault = true, SubPath = "files", MaxUsedBytes = maxUsedBytes }
                }
            };

            return new StorageCapacityService(
                Options.Create(new StorageOptions { Disks = new List<DiskOptions> { disk } }), _availability, _repository);
        }

        //Servicio de disponibilidad falso: solo importa la capacidad que devuelve
        private sealed class FakeAvailabilityService : IStorageAvailabilityService
        {
            public DiskCapacity? Capacity { get; set; }

            public DiskCapacity? GetCapacity(string diskKey) => Capacity;

            public void EnsureAvailable(string diskKey, bool forWrite) => throw new NotSupportedException();
            public DiskStatus CheckDisk(string diskKey) => throw new NotSupportedException();
            public IReadOnlyList<DiskStatus> CheckAllDisks() => throw new NotSupportedException();
            public DiskStatus? GetCachedStatus(string diskKey) => throw new NotSupportedException();
            public IReadOnlyList<DiskStatus> GetCachedStatuses() => throw new NotSupportedException();
        }

        //Repositorio falso: solo importa el uso registrado por ubicación y cuántas veces se consulta
        private sealed class FakeFileItemRepository : IFileItemRepository
        {
            public long UsedBytes { get; set; }
            public int UsedBytesQueries { get; private set; }

            public Task<long> GetUsedBytesByLocationAsync(string locationKey, CancellationToken cToken)
            {
                UsedBytesQueries++;
                return Task.FromResult(UsedBytes);
            }

            public Task AddAsync(FileItem fileItem, CancellationToken cToken) => throw new NotSupportedException();
            public Task<FileItem?> GetByIdAsync(int id, CancellationToken cToken, bool asNoTracking = true) => throw new NotSupportedException();
            public Task<(IEnumerable<FileItem> Items, int TotalCount)> SearchFileAsync(string? fileName, int ownerId, int? folderId, string? contentType,
                long? minSizeBytes, long? maxSizeBytes, DateTime? uploadedFrom, DateTime? uploadedTo, int pageNumber, int pageSize, CancellationToken cToken)
                => throw new NotSupportedException();
            public Task<IEnumerable<FileItem>> GetByOwnerAndFolderAsync(int ownerId, int? folderId, CancellationToken cToken) => throw new NotSupportedException();
            public Task<(int TotalFiles, long TotalSizeBytes, int TotalFolders)> GetUserStatsAsync(int ownerId, CancellationToken cToken) => throw new NotSupportedException();
            public void DeleteFile(FileItem fileItem) => throw new NotSupportedException();
            public Task<bool> UserHasFilesAsync(int ownerId, CancellationToken cToken) => throw new NotSupportedException();
            public Task SaveChangesAsync(CancellationToken cToken) => throw new NotSupportedException();
        }
    }
}
