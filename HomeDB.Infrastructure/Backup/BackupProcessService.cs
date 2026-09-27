using HomeDB.Domain.Common.RecordsInfrastructure;
using HomeDB.Domain.Interfaces.Services;
using Npgsql;
using System.Diagnostics;

namespace HomeDB.Infrastructure.Backup
{
    public class BackupProcessService : IBackupProcessService
    {
        //Crea un backup de archivos utilizando rsync y devuelve el resultado del proceso.
        public async Task<BackupProcessResult> RunRsyncAsync(string sourcePath, string destinationPath, string? linkDestPath, CancellationToken cToken)
        {
            //Si se indica linkDestPath, se agrega el argumento --link-dest a la línea de comandos de rsync para que reuse los archivos existentes ahorrando memoria.
            string linkDestArgument = string.IsNullOrEmpty(linkDestPath) 
                ? string.Empty 
                : $"--link-dest=\"{linkDestPath}\" ";

            //Se construye la línea de comandos de rsync con los argumentos necesarios.
            string arguments = $"-a {linkDestArgument}\"{sourcePath}\" \"{destinationPath}\"";

            //Se ejecuta el proceso de rsync y se obtiene el resultado.
            (int ExitCode, string StandardError) processResult = await RunProcessAsync("rsync", arguments, null, cToken);

            //Comprobar si el proceso se ejecuto correctamente
            if (processResult.ExitCode != 0)
                return new BackupProcessResult(false, processResult.ExitCode, 0, processResult.StandardError);

            //Leer el tamaño del directorio en bytes
            long sizeBytes = GetDirectorySizeBytes(destinationPath);

            //Devolver el resultado del proceso de backup con éxito, código de salida, tamaño en bytes y sin mensaje de error.
            return new BackupProcessResult(true, processResult.ExitCode, sizeBytes, null);
        }

        //Genera un backup de la base de datos PostgreSQL utilizando pg_dump y devuelve el resultado del proceso.
        public async Task<BackupProcessResult> RunPgDumpAsync(string connectionString, string outputFilePath, CancellationToken cToken)
        {
            //Se decompone la cadena de conexión para obtener host, puerto, base de datos, usuario y contraseña.
            NpgsqlConnectionStringBuilder connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);

            //Se construye la línea de comandos de pg_dump con los argumentos necesarios para conectarse a la base de datos y generar el backup en formato personalizado.
            string arguments = $"--host={connectionStringBuilder.Host} --port={connectionStringBuilder.Port} --dbname={connectionStringBuilder.Database} " +
                                $"--username={connectionStringBuilder.Username} --format=custom --file=\"{outputFilePath}\"";

            //Se establece la variable de entorno PGPASSWORD para la conexión a la base de datos. (Por argumento se podría leer, mas seguro)
            Dictionary<string, string> environmentVariables = new Dictionary<string, string> { ["PGPASSWORD"] = connectionStringBuilder.Password ?? string.Empty };

            //Se ejecuta el proceso de pg_dump y se obtiene el resultado.
            (int ExitCode, string StandardError) processResult = await RunProcessAsync("pg_dump", arguments, environmentVariables, cToken);

            //Se comprueba si el proceso se ejecutó correctamente.
            if (processResult.ExitCode != 0)
                return new BackupProcessResult(false, processResult.ExitCode, 0, processResult.StandardError);

            //Se obtiene la información del archivo de salida generado por pg_dump para determinar su tamaño y se devuelve el resultado.
            FileInfo dumpFileInfo = new FileInfo(outputFilePath);
            return new BackupProcessResult(true, processResult.ExitCode, dumpFileInfo.Length, null);
        }

        //Devuelve el tamaño total de un directorio en bytes.
        private static long GetDirectorySizeBytes(string path)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            return directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
        }

        //Ejecuta un proceso y devuelve su código de salida y mensaje de error.
        private static async Task<(int ExitCode, string StandardError)> RunProcessAsync(string fileName, string arguments, Dictionary<string, string>? environmentVariables, CancellationToken cToken)
        {
            //Configuración del proceso a ejecutar
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            //Si se proporcionan variables de entorno, se agregan al proceso.
            if (environmentVariables is not null)
            {
                foreach (KeyValuePair<string, string> variable in environmentVariables)
                    startInfo.Environment[variable.Key] = variable.Value;
            }

            //Se inicia el proceso y se espera a que termine, capturando cualquier mensaje de error.
            using Process process = new Process { StartInfo = startInfo };
            process.Start();

            //Se lee el mensaje de error estándar de manera asíncrona.
            string standardError = await process.StandardError.ReadToEndAsync(cToken);
            await process.WaitForExitAsync(cToken);

            //Se devuelve el código de salida del proceso y el mensaje de error capturado.
            return (process.ExitCode, standardError);
        }
    }
}