using HomeDB.Application.Options;
using HomeDB.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HomeDB.Tests.Unit.Application
{
    //Reproduce el registro de StorageOptions de ServicesExtensions (Configure + ValidateDataAnnotations) para comprobar
    //la regla que impide que una ubicación de backups entre en el backup.
    public sealed class StorageOptionsValidationTests
    {
        [Fact]
        public void StorageOptions_BackupsLocationIncludedInBackup_ThrowsOnResolve()
        {
            ServiceCollection services = BuildServices(includeBackupsLocationInBackup: true);
            using ServiceProvider provider = services.BuildServiceProvider();

            OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
                () => provider.GetRequiredService<IOptions<StorageOptions>>().Value);

            Assert.Contains("Backups no puede tener IncludeInBackup", exception.Message);
        }

        [Fact]
        public void StorageOptions_BackupsLocationNotIncludedInBackup_ResolvesWithoutThrowing()
        {
            ServiceCollection services = BuildServices(includeBackupsLocationInBackup: false);
            using ServiceProvider provider = services.BuildServiceProvider();

            StorageOptions resolved = provider.GetRequiredService<IOptions<StorageOptions>>().Value;

            Assert.Equal(2, resolved.Disks.Count);
        }

        [Theory]
        [InlineData(LocationUsage.Media)]
        [InlineData(LocationUsage.Temp)]
        [InlineData(LocationUsage.Backups)]
        public void StorageOptions_MaxUsedBytesOutsidePersonalLocation_ThrowsOnResolve(LocationUsage usage)
        {
            ServiceCollection services = BuildServicesWithExtraLocation(usage, maxUsedBytes: 1000);
            using ServiceProvider provider = services.BuildServiceProvider();

            OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
                () => provider.GetRequiredService<IOptions<StorageOptions>>().Value);

            Assert.Contains("MaxUsedBytes solo se admite en ubicaciones de uso Personal", exception.Message);
        }

        [Fact]
        public void StorageOptions_MaxUsedBytesInPersonalLocation_ResolvesWithoutThrowing()
        {
            ServiceCollection services = BuildServicesWithExtraLocation(LocationUsage.Personal, maxUsedBytes: 1000);
            using ServiceProvider provider = services.BuildServiceProvider();

            Assert.Equal(1000, provider.GetRequiredService<IOptions<StorageOptions>>().Value.Disks[1].Locations[0].MaxUsedBytes);
        }

        [Theory]
        [InlineData(1L)]
        [InlineData(1048576L)]  //1 MiB
        [InlineData(20971520L)] //20 MiB: el tope
        public void StorageOptions_SingleUploadMaxWithinRange_ResolvesAndAddsMarginForRequest(long maxFileBytes)
        {
            ServiceCollection services = BuildServicesWithSingleUploadMax(maxFileBytes);
            using ServiceProvider provider = services.BuildServiceProvider();

            StorageOptions resolved = provider.GetRequiredService<IOptions<StorageOptions>>().Value;

            Assert.Equal(maxFileBytes, resolved.SingleUploadMaxFileBytes);
            Assert.Equal(maxFileBytes + 1024 * 1024, resolved.SingleUploadMaxRequestBytes);
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(-5L)]
        [InlineData(20971521L)]     //1 byte por encima del tope
        [InlineData(524288000L)]    //500 MB: el límite global del chunked no vale aquí
        [InlineData(10737418240L)]  //10 GB: más que int.MaxValue
        public void StorageOptions_SingleUploadMaxOutOfRange_ThrowsOnResolve(long maxFileBytes)
        {
            ServiceCollection services = BuildServicesWithSingleUploadMax(maxFileBytes);
            using ServiceProvider provider = services.BuildServiceProvider();

            OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
                () => provider.GetRequiredService<IOptions<StorageOptions>>().Value);

            Assert.Contains("entre 1 y 20971520 bytes", exception.Message);
        }

        [Fact]
        public void StorageOptions_SingleUploadMaxNotConfigured_DefaultsToTwentyMebibytes()
        {
            Assert.Equal(20L * 1024 * 1024, new StorageOptions().SingleUploadMaxFileBytes);
        }

        //Helpers
        private static ServiceCollection BuildServicesWithSingleUploadMax(long maxFileBytes)
        {
            ServiceCollection services = new ServiceCollection();
            services.AddOptions<StorageOptions>()
                    .Configure(options =>
                    {
                        options.MaxFileSizeBytes = 1;
                        options.SingleUploadMaxFileBytes = maxFileBytes;
                        options.Disks = new List<DiskOptions>
                        {
                            new DiskOptions
                            {
                                Key = "disk",
                                Path = "/tmp",
                                StartupPolicy = DiskStartupPolicy.Optional,
                                Locations = new List<StorageLocationOptions>
                                {
                                    new StorageLocationOptions { Key = "main", Usage = LocationUsage.Personal, IsDefault = true, SubPath = "files" }
                                }
                            }
                        };
                    })
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
            return services;
        }

        private static ServiceCollection BuildServicesWithExtraLocation(LocationUsage usage, long? maxUsedBytes)
        {
            ServiceCollection services = new ServiceCollection();
            services.AddOptions<StorageOptions>()
                    .Configure(options =>
                    {
                        options.MaxFileSizeBytes = 1;
                        options.Disks = new List<DiskOptions>
                        {
                            new DiskOptions
                            {
                                Key = "m2-1", Path = "/storage", StartupPolicy = DiskStartupPolicy.Required,
                                Locations = new List<StorageLocationOptions>
                                {
                                    new StorageLocationOptions { Key = "main", Usage = LocationUsage.Personal, IsDefault = true, SubPath = "files" }
                                }
                            },
                            new DiskOptions
                            {
                                Key = "extra", Path = "/extra", StartupPolicy = DiskStartupPolicy.Optional,
                                Locations = new List<StorageLocationOptions>
                                {
                                    new StorageLocationOptions { Key = "extra-loc", Usage = usage, IsDefault = usage != LocationUsage.Personal, SubPath = "x", MaxUsedBytes = maxUsedBytes } //Solo una ubicación por defecto por uso: "main" ya es la Personal
                                }
                            }
                        };
                    })
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

            return services;
        }

        private static ServiceCollection BuildServices(bool includeBackupsLocationInBackup)
        {
            ServiceCollection services = new ServiceCollection();
            services.AddOptions<StorageOptions>()
                    .Configure(options =>
                    {
                        options.MaxFileSizeBytes = 1;
                        options.Disks = new List<DiskOptions>
                        {
                            new DiskOptions
                            {
                                Key = "m2-1", Path = "/storage", StartupPolicy = DiskStartupPolicy.Required,
                                Locations = new List<StorageLocationOptions>
                                {
                                    new StorageLocationOptions { Key = "main", Usage = LocationUsage.Personal, IsDefault = true, SubPath = "files", IncludeInBackup = true }
                                }
                            },
                            new DiskOptions
                            {
                                Key = "hdd2", Path = "/das/hdd2", StartupPolicy = DiskStartupPolicy.Optional,
                                Locations = new List<StorageLocationOptions>
                                {
                                    new StorageLocationOptions { Key = "backups", Usage = LocationUsage.Backups, IsDefault = true, SubPath = "backups", IncludeInBackup = includeBackupsLocationInBackup }
                                }
                            }
                        };
                    })
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

            return services;
        }
    }
}
