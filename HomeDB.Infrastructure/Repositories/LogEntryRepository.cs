using HomeDB.Domain.Entities;
using HomeDB.Domain.Interfaces;

namespace HomeDB.Infrastructure.Repositories
{
    public class LogEntryRepository : ILogEntryRepository
    {
        #region Constructores
        public LogEntryRepository(string encyptedConnection)
        {
            //decryptedConnectionString = DecryptConnectionString(encyptedConnection);
        }
        #endregion

        /// <summary>
        /// Agrega un log a la tabla log_entry
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        public async Task<int> InsertLogAsync(LogEntry log, CancellationToken cToken)
        {
            ////Variables y objetos
            //int newId = 0;

            ////Query
            //const string sql = @"
            //    INSERT INTO log_entry 
            //        (level, source, operation, message, correlationId, userId, durationMs, timeStamp, exception, metadataJson)
            //    VALUES
            //        (@level, @source, @operation, @message, @correlationId, @userId, @durationMs, @timeStamp, @exception, @metadataJson);

            //    SELECT LAST_INSERT_ID();
            //";


            //try
            //{
            //    //Compruebo que el log exista
            //    if (log == null) throw new ArgumentNullException(nameof(log));


            //    //Creo la conexion a DB
            //    await using MySqlConnection connection = new MySqlConnection(decryptedConnectionString);
            //    await connection.OpenAsync(cToken);


            //    //Inserto el log
            //    newId = await connection.ExecuteScalarAsync<int>(sql, log);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Error al insertar log: {ex}");
            //}

            ////Devuelvo el resultado
            //return newId;

            return -1; //TODO Implementar función
        }
    }
}
