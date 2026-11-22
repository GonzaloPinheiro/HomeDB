
namespace HomeDB.Domain.Common
{
    /// <summary>
    /// Lectura de la cabecera de un archivo para validar su tipo real (magic numbers). Compartida por la subida
    /// por chunks y la subida directa, para que ambas validen exactamente los mismos bytes.
    /// </summary>
    public static class FileHeader
    {
        /// <summary>
        /// Cantidad máxima de bytes iniciales que se inspeccionan. Algunos formatos tienen su firma desplazada
        /// (p. ej. "ftyp" de MP4 en el offset 4, "WEBP" y "WAVE" en el offset 8), por eso no basta con 4 u 8 bytes.
        /// </summary>
        public const int Length = 4096;

        /// <summary>
        /// Lee hasta <see cref="Length"/> bytes desde la posición actual del stream. Devuelve un array del tamaño
        /// exacto de lo leído (sin relleno de ceros: un archivo corto no debe parecer binario por ello).
        /// </summary>
        public static async Task<byte[]> ReadAsync(Stream stream, CancellationToken cToken)
        {
            byte[] buffer = new byte[Length];
            int totalRead = 0;

            //Un único ReadAsync puede devolver menos bytes de los pedidos, así que se repite hasta llenar el
            //buffer o llegar al final del stream.
            while (totalRead < Length)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(totalRead, Length - totalRead), cToken);
                if (read == 0)
                    break;

                totalRead += read;
            }

            return buffer.AsSpan(0, totalRead).ToArray();
        }
    }
}
