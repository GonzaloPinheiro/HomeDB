using HomeDB.Domain.Interfaces.Services;
using HomeDB.Infrastructure.Data;
using HomeDB.Infrastructure.Storage;

namespace HomeDB.DependencyInjection
{
    /// <summary>
    /// Comprobaciones de arranque del almacenamiento multi-ubicación. Se ejecutan sobre la aplicación ya construida
    /// y antes de empezar a atender peticiones.
    /// </summary>
    public static class StorageStartupExtensions
    {
        /// <summary>
        /// Aplica la política de arranque de cada disco declarado en Storage:Disks. Debe llamarse antes de migrar:
        /// si falta un disco Required la API no arranca y no se toca la base de datos. Un disco Optional ausente solo
        /// deja un aviso Critical, que se persiste cuando arranca el host.
        /// </summary>
        /// <param name="app">Aplicación web ya construida.</param>
        /// <exception cref="InvalidOperationException">Algún disco Required no está disponible o no es escribible.</exception>
        public static async Task ValidateStorageDisksAsync(this WebApplication app)
        {
            //El validador es singleton; el scope solo es necesario para resolverlo del proveedor de la aplicación.
            using IServiceScope scope = app.Services.CreateScope();

            StorageStartupValidator validator = scope.ServiceProvider.GetRequiredService<StorageStartupValidator>();
            await validator.ValidateAsync();
        }

        /// <summary>
        /// Comprueba que toda clave de ubicación guardada en FileItems exista en la configuración. Debe llamarse
        /// después de migrar, porque lee de la base de datos. Las claves de ubicación no se renombran nunca (ver módulo
        /// de almacenamiento multi-ubicación), así que una clave guardada que ya no exista en la config es una
        /// inconsistencia real -no un disco temporalmente desconectado-, y se detiene el arranque en vez de dejar
        /// archivos irresolubles.
        /// </summary>
        /// <param name="app">Aplicación web ya construida.</param>
        /// <exception cref="InvalidOperationException">Hay archivos con claves de ubicación que no están en la configuración.</exception>
        public static void ValidateStoredLocationKeys(this WebApplication app)
        {
            using IServiceScope scope = app.Services.CreateScope();

            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            IStorageLocationResolver locationResolver = scope.ServiceProvider.GetRequiredService<IStorageLocationResolver>();

            HashSet<string> configuredLocationKeys = locationResolver.GetAllLocationKeys().ToHashSet(StringComparer.OrdinalIgnoreCase);
            List<string> missingLocationKeys = db.FileItems
                .Select(f => f.LocationKey)
                .Distinct()
                .AsEnumerable()
                .Where(key => !configuredLocationKeys.Contains(key))
                .ToList();

            if (missingLocationKeys.Count > 0)
                throw new InvalidOperationException(
                    $"Hay archivos en base de datos con claves de ubicación que no existen en Storage:Disks: {string.Join(", ", missingLocationKeys)}.");
        }
    }
}
