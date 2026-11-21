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

        //Helpers
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
