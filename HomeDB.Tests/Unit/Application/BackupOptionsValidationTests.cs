using HomeDB.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HomeDB.Tests.Unit.Application
{
    //Reproduce el mismo registro de BackupOptions que ServicesExtensions (Bind + ValidateDataAnnotations + ValidateOnStart)
    //para comprobar que las validaciones anidadas de BackupLevelOptions.Daily se aplican realmente al resolver las opciones.
    public sealed class BackupOptionsValidationTests
    {
        [Fact]
        public void BackupOptions_WhenDailyDirectoryMissing_ThrowsOnResolve()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddOptions<BackupOptions>()
                    .Configure(options =>
                    {
                        options.SourceDirectory = "/source";
                        options.Daily.Directory = string.Empty; //Directorio vacío: debe fallar por [Required]/[MinLength] en BackupLevelOptions
                    })
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

            using ServiceProvider provider = services.BuildServiceProvider();

            Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<BackupOptions>>().Value);
        }

        [Fact]
        public void BackupOptions_WhenValid_ResolvesWithoutThrowing()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddOptions<BackupOptions>()
                    .Configure(options =>
                    {
                        options.SourceDirectory = "/source";
                        options.Daily.Directory = "/backups/daily";
                    })
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

            using ServiceProvider provider = services.BuildServiceProvider();

            BackupOptions resolved = provider.GetRequiredService<IOptions<BackupOptions>>().Value;
            Assert.Equal("/backups/daily", resolved.Daily.Directory);
        }
    }
}
