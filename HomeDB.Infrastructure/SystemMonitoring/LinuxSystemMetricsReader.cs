
using HomeDB.Domain.Common.Snapshots;
using HomeDB.Domain.Interfaces.Services;

namespace HomeDB.Infrastructure.SystemMonitoring
{
    public class LinuxSystemMetricsReader : ISystemMetricsReaderService
    {
        //Variables y objetos
        private readonly string _procStatPath;
        private readonly string _procMemInfoPath;
        private readonly string _thermalZonePath;
        private readonly string _diskRootPath;

        private long _previousIdleTime;
        private long _previousTotalTime;
        private bool _hasPreviousSample;

        //Constructores
        public LinuxSystemMetricsReader(
            string procStatPath = "/proc/stat",
            string procMemInfoPath = "/proc/meminfo",
            string thermalZonePath = "/sys/class/thermal/thermal_zone0/temp",
            string diskRootPath = "/")
        {
            _procStatPath = procStatPath;
            _procMemInfoPath = procMemInfoPath;
            _thermalZonePath = thermalZonePath;
            _diskRootPath = diskRootPath;
        }

        //Lee el uso de la cpu
        public async Task<CpuSnapshot?> ReadCpuAsync(CancellationToken cToken)
        {
            try
            {
                // /proc/stat tiene varias líneas (cpu, cpu0, cpu1...). Solo nos interesa
                // la primera línea "cpu " que es el agregado de todos los núcleos.
                string[] lines = await File.ReadAllLinesAsync(_procStatPath, cToken);
                string? cpuLine = lines.FirstOrDefault(l => l.StartsWith("cpu "));

                //No ha sido posible obtener el valor de uso de cpu
                if (cpuLine is null)
                    return null;

                // La línea tiene el formato: cpu  user nice system idle iowait irq softirq ...
                // Cada valor es tiempo de CPU acumulado en "jiffies" desde que arrancó el sistema.
                string[] parts = cpuLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                //TODO Darle una vuelta, ver si es mejor devoler null o si alguno de los campos no existe por x motivo lanzar un log crítico para revisarlo
                //if (parts.Length < 8)
                  //  return null;

                long user = long.Parse(parts[1]);
                long nice = long.Parse(parts[2]);
                long system = long.Parse(parts[3]);
                long idle = long.Parse(parts[4]);
                long ioWait = long.Parse(parts[5]);
                long irq = long.Parse(parts[6]);
                long softIrq = long.Parse(parts[7]);

                // Tiempo total de CPU y tiempo "inactivo" (idle + esperando I/O) acumulados.
                long totalTime = user + nice + system + idle + ioWait + irq + softIrq;
                long idleTime = idle + ioWait;

                // Primera lectura: no hay muestra anterior con la que comparar, así que
                // guardamos esta como punto de partida y devolvemos null. El % real se
                // podrá calcular a partir de la segunda llamada.
                if (!_hasPreviousSample)
                {
                    _previousIdleTime = idleTime;
                    _previousTotalTime = totalTime;
                    _hasPreviousSample = true;
                    return null;
                }

                // El % de uso de CPU se calcula con la diferencia entre dos lecturas:
                // cuánto tiempo total pasó vs cuánto de ese tiempo fue inactivo.
                long deltaTotal = totalTime - _previousTotalTime;
                long deltaIdle = idleTime - _previousIdleTime;

                // Actualizamos el estado para que la siguiente llamada compare contra esta lectura.
                _previousIdleTime = idleTime;
                _previousTotalTime = totalTime;

                // Si no ha pasado tiempo entre lecturas (delta 0 o negativo), no podemos calcular nada fiable.
                if (deltaTotal <= 0)
                    return null;

                // % de uso = (tiempo total - tiempo inactivo) / tiempo total * 100
                double usagePercent = 100.0 * (deltaTotal - deltaIdle) / deltaTotal;
                return new CpuSnapshot(usagePercent);
            }
            catch (IOException)
            {
                // El archivo no existe, no se puede leer, o cualquier problema de E/S.
                // Devolvemos null en lugar de propagar la excepción: una métrica caída
                // no debería tirar abajo todo el muestreo del sistema.
                return null;
            }
            catch (FormatException)
            {
                // El contenido de /proc/stat no tiene el formato numérico esperado.
                return null;
            }


        }

        //Devuelve el % de memoria RAM en uso
        public async Task<MemorySnapshot?> ReadMemoryAsync(CancellationToken cToken)
        {
            try
            {
                // /proc/meminfo tiene muchas líneas, una por cada tipo de métrica de memoria.
                // Solo necesitamos el total y el disponible para calcular el % usado.
                string[] lines = await File.ReadAllLinesAsync(_procMemInfoPath, cToken);

                long? totalKb = ParseMemInfoLine(lines, "MemTotal:");
                long? availableKb = ParseMemInfoLine(lines, "MemAvailable:");

                //Devolver null si no se pudo obtener uno de los dos valores
                if (totalKb is null || availableKb is null)
                    return null;

                //Convertir Kb en Bytes y calcular el uso de la RAM
                long totalBytes = totalKb.Value * 1024;
                long availableBytes = availableKb.Value * 1024;
                long usedBytes = totalBytes - availableBytes;

                // MemAvailable (no MemFree) es la métrica recomendada por el kernel para saber
                // cuánta memoria está realmente disponible, ya que tiene en cuenta la caché
                // que el kernel puede liberar bajo presión.
                double usagePercent = totalBytes == 0 ? 0 : 100.0 * usedBytes / totalBytes;
                
                //Devolver resultado
                return new MemorySnapshot(totalBytes, usedBytes, usagePercent);
            }
            catch (IOException)
            {
                return null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        //Lee el estado del disco duro en el sistema
        //La lectura es síncrona pero se usa Task para mantener consistencia
        public Task<DiskSnapshot?> ReadDiskAsync(CancellationToken cToken)
        {
            try
            {
                //Objeto para leer los datos del disco
                DriveInfo drive = new DriveInfo(_diskRootPath);

                long totalBytes = drive.TotalSize;
                long freeBytes = drive.AvailableFreeSpace;
                long usedBytes = totalBytes - freeBytes;

                //Calcular el % de uso
                double usagePercent = totalBytes == 0 ? 0 : 100.0 * usedBytes / totalBytes;

                //Devolver el resultado
                return Task.FromResult<DiskSnapshot?>(new DiskSnapshot(totalBytes, usedBytes, usagePercent));
            }
            catch (IOException)
            {
                return Task.FromResult<DiskSnapshot?>(null);
            }
        }

        //Lee la tempertatura de la CPU del sistema
        public async Task<TemperatureSnapshot?> ReadTemperatureAsync(CancellationToken cToken)
        {
            try
            {
                // El kernel expone la temperatura de cada sensor térmico como un único valor
                // en milicelsius dentro de un archivo de texto plano.
                string rawValue = await File.ReadAllTextAsync(_thermalZonePath, cToken);

                if (!long.TryParse(rawValue.Trim(), out long milliCelsius))
                    return null;

                // Convertimos de milicelsius a celsius (ej: 45000 -> 45.0).
                double celsius = milliCelsius / 1000.0;
                return new TemperatureSnapshot(celsius);
            }
            catch (IOException)
            {
                return null;
            }
        }

        #region Funciones privadas
        // Exreae la línea con el valor indicado por parámetro
        // Ejemplo de línea: "MemTotal: 16326144 kB"
        private static long? ParseMemInfoLine(string[] lines, string key)
        {
            //Busca la línea con el campo key
            string? line = lines.FirstOrDefault(l => l.StartsWith(key));

            //Devuelve null si no encuentra nada
            if (line is null)
                return null;

            //Divide la línea en columnas
            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
                return null;

            //Devuelve el valor de la memoria
            return long.TryParse(parts[1], out long value) ? value : null;
        }
        #endregion
    }
}