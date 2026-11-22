
namespace HomeDB.Domain.Common
{
    /// <summary>
    /// Límites fijos de la subida directa (un archivo en una sola petición). Son constantes de compilación y no
    /// configuración porque los atributos [RequestSizeLimit] y [RequestFormLimits] del endpoint solo admiten
    /// constantes, y porque el tope de 20 MiB es obligatorio: no debe poder subirse editando un appsettings.
    /// Lo que supere este tamaño debe subirse por chunks (api/files/upload/init).
    /// </summary>
    public static class UploadLimits
    {
        /// <summary>Tamaño máximo del archivo en una subida directa: 20 MiB (20 971 520 bytes).</summary>
        public const long SingleUploadMaxFileBytes = 20L * 1024 * 1024;

        /// <summary>
        /// Tamaño máximo de la petición completa de una subida directa: el archivo más 1 MiB de margen para los
        /// delimitadores multipart y los demás campos del formulario.
        /// </summary>
        public const long SingleUploadMaxRequestBytes = SingleUploadMaxFileBytes + 1024 * 1024;
    }
}
