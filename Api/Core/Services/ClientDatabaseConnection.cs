using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Models;
using Api.Modules.Tenants.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace Api.Core.Services
{
    /// <summary>
    /// This is meant to be used as a decorator pattern. This will use the original <see cref="IDatabaseConnection"/> to access the main Wiser database and find the tenant's info in easy_customers.
    /// It will then create a new connection string for that tenant and open a connection to that database. This means that using this class, you will always have access to the tenant database.
    /// </summary>
    public class ClientDatabaseConnection : IDatabaseConnection, IScopedService
    {
        /// <summary>
        /// A connection object to the Wiser Database.
        /// </summary>
        public readonly IDatabaseConnection WiserDatabaseConnection;
        private readonly ApiSettings apiSettings;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<ClientDatabaseConnection> logger;
        private readonly IWebHostEnvironment webHostEnvironment;

        private string connectionStringForReading;
        private string connectionStringForWriting;

        private MySqlConnection ConnectionForReading { get; set; }
        private MySqlConnection ConnectionForWriting { get; set; }
        private MySqlCommand CommandForReading { get; set; }
        private MySqlCommand CommandForWriting { get; set; }

        private readonly GclSettings gclSettings;

        private DbDataReader dataReader;

        private IDbTransaction transaction;

        private readonly ConcurrentDictionary<string, object> parameters = new();

        private readonly Guid instanceId;
        private int readConnectionLogId;
        private int writeConnectionLogId;
        private bool? logTableExists;

        private string subDomain;

        /// <summary>
        /// Creates a new instance of <see cref="ClientDatabaseConnection"/>.
        /// </summary>
        public ClientDatabaseConnection(IDatabaseConnection wiserDatabaseConnection, IOptions<GclSettings> gclSettings, IOptions<ApiSettings> apiSettings, IHttpContextAccessor httpContextAccessor, ILogger<ClientDatabaseConnection> logger, IWebHostEnvironment webHostEnvironment)
        {
            this.gclSettings = gclSettings.Value;
            this.WiserDatabaseConnection = wiserDatabaseConnection;
            this.apiSettings = apiSettings.Value;
            this.httpContextAccessor = httpContextAccessor;
            this.logger = logger;
            this.webHostEnvironment = webHostEnvironment;
            instanceId = Guid.NewGuid();
        }

        /// <inheritdoc />
        public bool HasActiveTransaction()
        {
            return transaction != null;
        }

        /// <inheritdoc />
        public string ConnectedDatabase { get; protected set; }

        /// <inheritdoc />
        public string ConnectedDatabaseForWriting { get; protected set; }

        /// <summary>
        /// Get whether or not the current sub domain is empty or the sub domain of the main Wiser database.
        /// </summary>
        private bool IsMainDatabase()
        {
            return String.IsNullOrWhiteSpace(subDomain) || String.Equals(subDomain, apiSettings.MainSubDomain, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the connection string for the tenant/client database. At least one of the parameters must contain a value.
        /// </summary>
        /// <param name="subDomain">The Wiser sub domain for the client/tenant. You can leave this empty or set the value to "main", to get the connection string for the Wiser database.</param>
        /// <returns>The connection string for the tenant/client database.</returns>
        public async Task<string> GetClientConnectionStringAsync(string subDomain)
        {
            this.subDomain = subDomain;
            if (IsMainDatabase())
            {
                return gclSettings.ConnectionString;
            }

            WiserDatabaseConnection.ClearParameters();
            WiserDatabaseConnection.AddParameter("subDomain", subDomain);
            var query = $"SELECT db_host, db_login, db_passencrypted, db_port, db_dbname, encryption_key FROM {ApiTableNames.WiserTenants} WHERE subdomain = ?subDomain";

            var dataTable = await WiserDatabaseConnection.GetAsync(query);

            if (dataTable.Rows.Count == 0)
            {
                throw new Exception($"No tenant record found for {(httpContextAccessor.HttpContext?.User.Identity is not ClaimsIdentity identity ? "Unknown" : IdentityHelpers.GetName(identity))}!");
            }

            var server = dataTable.Rows[0].Field<string>("db_host");
            var port = dataTable.Rows[0].Field<string>("db_port");
            var username = dataTable.Rows[0].Field<string>("db_login");
            var encryptedPassword = dataTable.Rows[0].Field<string>("db_passencrypted");
            var decryptedPassword = encryptedPassword.DecryptWithAesWithSalt(apiSettings.DatabasePasswordEncryptionKey);
            var database = dataTable.Rows[0].Field<string>("db_dbname");

            // Use the default port number for MySQL (which is 3306) if it isn't set in the tenant settings table.
            if (String.IsNullOrWhiteSpace(port))
            {
                port = "3306";
            }

            return $"server={server};port={port};uid={username};pwd={decryptedPassword};database={database};AllowUserVariables=True;ConvertZeroDateTime=true;CharSet=utf8";
        }

        /// <inheritdoc />
        public async Task<DbDataReader> GetReaderAsync(string query)
        {
            await EnsureOpenConnectionForReadingAsync();
            CommandForReading.CommandText = query;

            dataReader = await CommandForReading.ExecuteReaderAsync();

            return dataReader;
        }

        /// <inheritdoc />
        public Task<DataTable> GetAsync(string query, bool skipCache = false, bool cleanUp = true, bool useWritingConnectionIfAvailable = false)
        {
            return GetAsync(query, 0, cleanUp, useWritingConnectionIfAvailable);
        }

        private async Task<DataTable> GetAsync(string query, int retryCount, bool cleanUp = true, bool useWritingConnectionIfAvailable = false)
        {
            try
            {
                MySqlCommand commandToUse;
                if (useWritingConnectionIfAvailable && !String.IsNullOrWhiteSpace(connectionStringForWriting))
                {
                    await EnsureOpenConnectionForWritingAsync();
                    commandToUse = CommandForWriting;
                }
                else
                {
                    await EnsureOpenConnectionForReadingAsync();
                    commandToUse = CommandForReading;
                }

                var result = new DataTable();
                commandToUse.CommandText = query;
                using var dataAdapter = new MySqlDataAdapter(commandToUse);
                await dataAdapter.FillAsync(result);
                return result;
            }
            catch (MySqlException mySqlException)
            {
                if (retryCount >= gclSettings.MaximumRetryCountForQueries)
                {
                    throw;
                }

                switch (mySqlException.Number)
                {
                    case (int)MySqlErrorCode.LockDeadlock:
                    case (int)MySqlErrorCode.LockWaitTimeout:
                        return await GetAsync(query, retryCount + 1);
                    case (int)MySqlErrorCode.UnableToConnectToHost:
                    case (int)MySqlErrorCode.TooManyUserConnections:
                    case (int)MySqlErrorCode.ConnectionCountError:
                        Thread.Sleep(1000);
                        return await GetAsync(query, retryCount + 1);
                    default:
                        throw;
                }
            }
            finally
            {
                // If we're not using transactions, dispose everything here. Otherwise we will dispose it when the transaction gets comitted or rollbacked.
                if (transaction == null && cleanUp)
                {
                    await CleanUpAsync();
                }
            }
        }

        /// <inheritdoc />
        public async Task<string> GetAsJsonAsync(string query, bool formatResult = false, bool skipCache = false)
        {
            return JsonConvert.SerializeObject(await GetAsync(query), formatResult ? Formatting.Indented : Formatting.None);
        }

        /// <inheritdoc />
        public Task<int> ExecuteAsync(string query, bool useWritingConnectionIfAvailable = true, bool cleanUp = true)
        {
            return ExecuteAsync(query, 0, useWritingConnectionIfAvailable, cleanUp);
        }

        /// <summary>
        /// Executes a query and returns the amount of rows affected.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="retryCount">How many times the query has been attempted.</param>
        /// <param name="useWritingConnectionIfAvailable"></param>
        /// <param name="cleanUp">Clean up after the query has been completed.</param>
        /// <returns></returns>
        private async Task<int> ExecuteAsync(string query, int retryCount, bool useWritingConnectionIfAvailable = true, bool cleanUp = true)
        {
            try
            {
                MySqlCommand commandToUse;
                if (useWritingConnectionIfAvailable && !String.IsNullOrWhiteSpace(connectionStringForWriting))
                {
                    await EnsureOpenConnectionForWritingAsync();
                    commandToUse = CommandForWriting;
                }
                else
                {
                    await EnsureOpenConnectionForReadingAsync();
                    commandToUse = CommandForReading;
                }

                commandToUse.CommandText = query;
                return await commandToUse.ExecuteNonQueryAsync();
            }
            catch (MySqlException mySqlException)
            {
                if (retryCount >= gclSettings.MaximumRetryCountForQueries)
                {
                    throw;
                }

                switch (mySqlException.Number)
                {
                    case (int)MySqlErrorCode.LockDeadlock:
                    case (int)MySqlErrorCode.LockWaitTimeout:
                        return await ExecuteAsync(query, retryCount + 1);
                    case (int)MySqlErrorCode.UnableToConnectToHost:
                    case (int)MySqlErrorCode.TooManyUserConnections:
                    case (int)MySqlErrorCode.ConnectionCountError:
                        Thread.Sleep(1000);
                        return await ExecuteAsync(query, retryCount + 1);
                    default:
                        throw;
                }
            }
            finally
            {
                // If we're not using transactions, dispose everything here. Otherwise we will dispose it when the transaction gets comitted or rollbacked.
                if (transaction == null && cleanUp)
                {
                    await CleanUpAsync();
                }
            }
        }

        /// <inheritdoc />
        public async Task<T> InsertOrUpdateRecordBasedOnParametersAsync<T>(string tableName, T id = default, string idColumnName = "id", bool ignoreErrors = false, bool useWritingConnectionIfAvailable = true)
        {
            if (parameters.Count == 0)
            {
                return id;
            }

            AddParameter("InsertOrUpdateRecord_Id", id);
            var query = new StringBuilder();
            var idIsDefaultValue = id.Equals(default(T));
            if (idIsDefaultValue)
            {
                query.Append($"INSERT {(ignoreErrors ? "IGNORE" : "")} INTO `{tableName}`");
            }
            else
            {
                query.Append($"UPDATE {(ignoreErrors ? "IGNORE" : "")} `{tableName}` SET ");
            }

            if (idIsDefaultValue)
            {
                query.Append($"({String.Join(",", parameters.Select(p => $"`{(p.Key == "InsertOrUpdateRecord_Id" ? idColumnName : p.Key)}`"))}) VALUES ({String.Join(",", parameters.Select(p => $"?{p.Key}"))})");
            }
            else
            {
                query.Append($"{String.Join(",", parameters.Where(p => p.Key != "InsertOrUpdateRecord_Id").Select(p => $"`{p.Key}` = ?{p.Key}"))} WHERE `{idColumnName}` = ?InsertOrUpdateRecord_Id");
            }

            await ExecuteAsync(query.ToString(), useWritingConnectionIfAvailable, false);

            if (!idIsDefaultValue)
            {
                return id;
            }

            var result = await GetAsync("SELECT LAST_INSERT_ID()", useWritingConnectionIfAvailable: useWritingConnectionIfAvailable);
            return (T)Convert.ChangeType(result.Rows[0][0], typeof(T));
        }

        /// <inheritdoc />
        public Task<long> InsertRecordAsync(string query, bool useWritingConnectionIfAvailable = true)
        {
            return InsertRecordAsync(query, 0, useWritingConnectionIfAvailable);
        }

        private async Task<long> InsertRecordAsync(string query, int retryCount, bool useWritingConnectionIfAvailable = true)
        {
            if (String.IsNullOrWhiteSpace(query))
            {
                logger.LogWarning("Null or empty string was passed to InsertRecordAsync function.");
                return 0L;
            }

            try
            {
                MySqlCommand commandToUse;
                if (useWritingConnectionIfAvailable && !String.IsNullOrWhiteSpace(connectionStringForWriting))
                {
                    await EnsureOpenConnectionForWritingAsync();
                    commandToUse = CommandForWriting;
                }
                else
                {
                    await EnsureOpenConnectionForReadingAsync();
                    commandToUse = CommandForReading;
                }

                var finalQuery = new StringBuilder(query.TrimEnd());
                if (finalQuery[^1] != ';')
                {
                    finalQuery.Append(';');
                }

                // Add the query to retrieve the last inserted ID to the query that was passed to the function.
                finalQuery.Append("SELECT LAST_INSERT_ID();");

                commandToUse.CommandText = finalQuery.ToString();

                await using var reader = await commandToUse.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return 0L;
                }

                return Int64.TryParse(Convert.ToString(reader.GetValue(0)), out var tempId) ? tempId : 0L;
            }
            catch (MySqlException mySqlException)
            {
                if (retryCount >= gclSettings.MaximumRetryCountForQueries)
                {
                    throw;
                }

                switch (mySqlException.Number)
                {
                    case (int)MySqlErrorCode.LockDeadlock:
                    case (int)MySqlErrorCode.LockWaitTimeout:
                        return await InsertRecordAsync(query, retryCount + 1);
                    case (int)MySqlErrorCode.UnableToConnectToHost:
                    case (int)MySqlErrorCode.TooManyUserConnections:
                    case (int)MySqlErrorCode.ConnectionCountError:
                        Thread.Sleep(1000);
                        return await InsertRecordAsync(query, retryCount + 1);
                    default:
                        throw;
                }
            }
            finally
            {
                // If we're not using transactions, dispose everything here. Otherwise we will dispose it when the transaction gets comitted or rollbacked.
                if (transaction == null)
                {
                    await CleanUpAsync();
                }
            }
        }

        /// <inheritdoc />
        public async Task<IDbTransaction> BeginTransactionAsync(bool forceNewTransaction = false)
        {
            if (!forceNewTransaction && transaction != null)
            {
                throw new InvalidOperationException("Called BeginTransaction, but there already is an active transaction.");
            }

            transaction?.Rollback();

            // If we're using transactions, make sure to use it on the write connection, if we have one.
            MySqlConnection connectionToUse;
            if (!String.IsNullOrWhiteSpace(connectionStringForWriting))
            {
                await EnsureOpenConnectionForWritingAsync();
                connectionToUse = ConnectionForWriting;
            }
            else
            {
                await EnsureOpenConnectionForReadingAsync();
                connectionToUse = ConnectionForReading;
            }

            transaction = await connectionToUse.BeginTransactionAsync();

            return transaction;
        }

        /// <inheritdoc />
        public async Task CommitTransactionAsync(bool throwErrorIfNoActiveTransaction = true)
        {
            if (transaction == null)
            {
                if (throwErrorIfNoActiveTransaction)
                {
                    throw new InvalidOperationException("Called CommitTransactionAsync, but there is no active transaction.");
                }

                return;
            }

            transaction.Commit();

            // Dispose and set to null, so that we know there is no more active transaction.
            transaction.Dispose();
            transaction = null;
            await CleanUpAsync();
        }

        /// <inheritdoc />
        public async Task RollbackTransactionAsync(bool throwErrorIfNoActiveTransaction = true)
        {
            if (transaction == null)
            {
                if (throwErrorIfNoActiveTransaction)
                {
                    throw new InvalidOperationException("Called RollbackTransactionAsync, but there is no active transaction.");
                }

                return;
            }

            transaction.Rollback();

            // Dispose and set to null, so that we know there is no more active transaction.
            transaction.Dispose();
            transaction = null;
            await CleanUpAsync();
        }

        /// <inheritdoc />
        public void ClearParameters()
        {
            parameters.Clear();
        }

        /// <inheritdoc />
        public void AddParameter(string key, object value)
        {
            if (parameters.ContainsKey(key))
            {
                parameters.TryRemove(key, out _);
            }

            parameters.TryAdd(key, value);
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (dataReader != null)
            {
                await dataReader.DisposeAsync();
            }

            if (ConnectionForReading != null)
            {
                await AddConnectionCloseLogAsync(false, true);
            }

            if (ConnectionForWriting != null)
            {
                await AddConnectionCloseLogAsync(true, true);
            }

            if (WiserDatabaseConnection != null)
            {
                await WiserDatabaseConnection.DisposeAsync();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            logger.LogTrace($"Disposing instance of MySqlDatabaseConnection with ID '{instanceId}' on URL {HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext)}");
            dataReader?.Dispose();
            AddConnectionCloseLogAsync(false, true);
            AddConnectionCloseLogAsync(true, true);
            WiserDatabaseConnection?.Dispose();
        }

        /// <summary>
        /// If the connection is not open yet, open it.
        /// </summary>
        /// <returns></returns>
        public async Task EnsureOpenConnectionForReadingAsync()
        {
            var createdNewConnection = false;

            // If we don't have a connection string yet, get the connection string from the Wiser database.
            if (String.IsNullOrWhiteSpace(connectionStringForReading))
            {
                if (httpContextAccessor.HttpContext == null)
                {
                    throw new ArgumentNullException(nameof(httpContextAccessor.HttpContext));
                }

                if (httpContextAccessor.HttpContext.User.Identity is ClaimsIdentity identity && identity.Claims.Any())
                {
                    subDomain = IdentityHelpers.GetSubDomain(identity);
                }

                if (String.IsNullOrWhiteSpace(subDomain))
                {
                    subDomain = (string)httpContextAccessor.HttpContext.Items[HttpContextConstants.SubDomainKey];
                }

                if (String.IsNullOrWhiteSpace(subDomain))
                {
                    throw new Exception("No sub domain found!");
                }

                connectionStringForReading = await GetClientConnectionStringAsync(subDomain);
                if (String.IsNullOrWhiteSpace(connectionStringForReading))
                {
                    throw new Exception("No connection string found!");
                }

                if (String.IsNullOrWhiteSpace(connectionStringForWriting))
                {
                    connectionStringForWriting = connectionStringForReading;
                }

                ConnectionForReading = new MySqlConnection { ConnectionString = connectionStringForReading };
                CommandForReading = ConnectionForReading.CreateCommand();
                createdNewConnection = true;

               /* if (String.IsNullOrWhiteSpace(connectionStringForWriting))
                {
                    return;
                }

                ConnectionForWriting = new MySqlConnection { ConnectionString = connectionStringForWriting };
                CommandForWriting = ConnectionForWriting.CreateCommand();*/
            }

            if (ConnectionForReading == null)
            {
                ConnectionForReading = new MySqlConnection { ConnectionString = connectionStringForReading };
                CommandForReading = ConnectionForReading.CreateCommand();
                createdNewConnection = true;
            }

            CommandForReading ??= ConnectionForReading.CreateCommand();

            // Remember the database name that was connected to.
            ConnectedDatabase = ConnectionForReading.Database;

            // Copy parameters.
            foreach (var parameter in parameters)
            {
                if (CommandForReading.Parameters.Contains(parameter.Key))
                {
                    CommandForReading.Parameters.RemoveAt(parameter.Key);
                }

                CommandForReading.Parameters.AddWithValue(parameter.Key, parameter.Value);
            }

            switch (ConnectionForReading.State)
            {
                case ConnectionState.Open:
                    return;
                case ConnectionState.Closed:
                    await ConnectionForReading.OpenAsync();

                    try
                    {
                        // Make sure we always use the same character set and collation.
                        CommandForReading.CommandText = "SET NAMES utf8mb4 COLLATE utf8mb4_general_ci;";
                        await CommandForReading.ExecuteNonQueryAsync();

                        // Make sure we always use the correct timezone.
                        if (!String.IsNullOrWhiteSpace(gclSettings.DatabaseTimeZone))
                        {
                            CommandForReading.CommandText = $"SET @@time_zone = {gclSettings.DatabaseTimeZone.ToMySqlSafeValue(true)};";
                            await CommandForReading.ExecuteNonQueryAsync();
                        }
                    }
                    catch (Exception exception)
                    {
                        logger.LogWarning(exception, $"An error occurred while trying to set the time zone to '{gclSettings.DatabaseTimeZone}' or the collation to 'utf8mb4_general_ci'.");
                    }

                    if (createdNewConnection)
                    {
                        // Log the opening of the connection.
                        await AddConnectionOpenLogAsync(false);
                    }

                    break;
            }
        }

        /// <summary>
        /// If the connection is not open yet, open it.
        /// </summary>
        /// <returns></returns>
        public async Task EnsureOpenConnectionForWritingAsync()
        {
            if (String.IsNullOrWhiteSpace(connectionStringForWriting))
            {
                ConnectedDatabaseForWriting = null;
                return;
            }

            var createdNewConnection = false;
            if (ConnectionForWriting == null)
            {
                ConnectionForWriting = new MySqlConnection { ConnectionString = connectionStringForWriting };
                CommandForWriting = ConnectionForWriting.CreateCommand();
                createdNewConnection = true;
            }

            CommandForWriting ??= ConnectionForWriting.CreateCommand();

            // Remember the database name that was connected to.
            ConnectedDatabaseForWriting = ConnectionForWriting.Database;

            // Copy parameters.
            foreach (var parameter in parameters)
            {
                if (CommandForWriting.Parameters.Contains(parameter.Key))
                {
                    CommandForWriting.Parameters.RemoveAt(parameter.Key);
                }

                CommandForWriting.Parameters.AddWithValue(parameter.Key, parameter.Value);
            }

            switch (ConnectionForWriting.State)
            {
                case ConnectionState.Open:
                    return;
                case ConnectionState.Closed:
                    await ConnectionForWriting.OpenAsync();

                    try
                    {

                        // Make sure we always use the same character set and collation.
                        CommandForWriting.CommandText = "SET NAMES utf8mb4 COLLATE utf8mb4_general_ci;";
                        await CommandForWriting.ExecuteNonQueryAsync();

                        // Make sure we always use the correct timezone.
                        if (!String.IsNullOrWhiteSpace(gclSettings.DatabaseTimeZone))
                        {
                            CommandForWriting.CommandText = $"SET @@time_zone = {gclSettings.DatabaseTimeZone.ToMySqlSafeValue(true)};";
                            await CommandForWriting.ExecuteNonQueryAsync();
                        }
                    }
                    catch (Exception exception)
                    {
                        logger.LogWarning(exception, $"An error occurred while trying to set the time zone to '{gclSettings.DatabaseTimeZone}' or the collation to 'utf8mb4_general_ci'.");
                    }

                    if (createdNewConnection)
                    {
                        // Log the opening of the connection.
                        await AddConnectionOpenLogAsync(true);
                    }
                    break;
            }
        }

        /// <inheritdoc />
        public async Task ChangeConnectionStringsAsync(string newConnectionStringForReading, string newConnectionStringForWriting)
        {
            connectionStringForReading = newConnectionStringForReading;
            connectionStringForWriting = newConnectionStringForReading;
            await CleanUpAsync();
        }

        /// <inheritdoc />
        public string GetDatabaseNameForCaching(bool writeDatabase = false)
        {
            if (connectionStringForReading == null)
            {
                throw new Exception("connectionStringForReading is null, please call EnsureOpenConnectionForReadingAsync first");
            }

            var connectionStringBuilder = new DbConnectionStringBuilder { ConnectionString = writeDatabase && !String.IsNullOrWhiteSpace(connectionStringForWriting) ? connectionStringForWriting : connectionStringForReading };
            return $"{connectionStringBuilder["server"]}_{connectionStringBuilder["database"]}";
        }

        /// <inheritdoc />
        public void SetCommandTimeout(int value)
        {
            if (CommandForReading != null)
            {
                CommandForReading.CommandTimeout = value;
            }

            if (CommandForWriting != null)
            {
                CommandForWriting.CommandTimeout = value;
            }
        }

        private async Task CleanUpAsync()
        {
            if (dataReader != null) await dataReader.DisposeAsync();
            if (CommandForReading != null) await CommandForReading.DisposeAsync();
            if (CommandForWriting != null) await CommandForWriting.DisposeAsync();
            CommandForReading = null;
            CommandForWriting = null;
            dataReader = null;
        }

        /// <summary>
        /// Add a mention to the log table that a connection to the database has been opened.
        /// </summary>
        /// <param name="isWriteConnection">Is this a write connection (true) or a read connection (false)?</param>
        private async Task AddConnectionOpenLogAsync(bool isWriteConnection)
        {
            try
            {
                if (!gclSettings.LogOpeningAndClosingOfConnections)
                {
                    return;
                }

                var commandToUse = isWriteConnection && !String.IsNullOrWhiteSpace(connectionStringForWriting) ? CommandForWriting : CommandForReading;

                logTableExists ??= await LogTableExistsAsync(commandToUse);

                if (!logTableExists.Value)
                {
                    // Table for logging doesn't exist yet, don't do anything. The table gets created during startup, but that also uses this service for doing that.
                    // So the table obviously won't exist yet during startup and we don't want an error from that.
                    return;
                }

                var url = "";
                var httpMethod = "";
                if (httpContextAccessor.HttpContext != null)
                {
                    url = HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext).ToString();
                    httpMethod = httpContextAccessor.HttpContext.Request.Method;
                }

                if(commandToUse.Parameters.Contains("gclConnectionOpened")) commandToUse.Parameters.Remove("gclConnectionOpened");
                if(commandToUse.Parameters.Contains("gclConnectionUrl")) commandToUse.Parameters.Remove("gclConnectionUrl");
                if(commandToUse.Parameters.Contains("gclConnectionHttpMethod")) commandToUse.Parameters.Remove("gclConnectionHttpMethod");
                if(commandToUse.Parameters.Contains("gclConnectionInstanceId")) commandToUse.Parameters.Remove("gclConnectionInstanceId");
                if(commandToUse.Parameters.Contains("gclConnectionType")) commandToUse.Parameters.Remove("gclConnectionType");
                commandToUse.Parameters.AddWithValue("gclConnectionOpened", DateTime.Now);
                commandToUse.Parameters.AddWithValue("gclConnectionUrl", url);
                commandToUse.Parameters.AddWithValue("gclConnectionHttpMethod", httpMethod);
                commandToUse.Parameters.AddWithValue("gclConnectionInstanceId", instanceId);
                commandToUse.Parameters.AddWithValue("gclConnectionType", isWriteConnection ? "write" : "read");

                commandToUse.CommandText = $@"INSERT INTO {Constants.DatabaseConnectionLogTableName} (opened, url, http_method, database_service_instance_id, type)
VALUES (?gclConnectionOpened, ?gclConnectionUrl, ?gclConnectionHttpMethod, ?gclConnectionInstanceId, ?gclConnectionType);
SELECT LAST_INSERT_ID();";
                await using var reader = await commandToUse.ExecuteReaderAsync();
                var id = !await reader.ReadAsync() ? 0 : (Int32.TryParse(Convert.ToString(reader.GetValue(0)), out var tempId) ? tempId : 0);

                if (isWriteConnection)
                {
                    writeConnectionLogId = id;
                }
                else
                {
                    readConnectionLogId = id;
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error while trying to add connection open log.");
            }
        }

        /// <summary>
        /// Add a mention to the log table that a connection to the database has been closed.
        /// </summary>
        /// <param name="isWriteConnection">Is this a write connection (true) or a read connection (false)?</param>
        /// <param name="disposeConnection">Set to true to dispose the connection at the end.</param>
        private async Task AddConnectionCloseLogAsync(bool isWriteConnection, bool disposeConnection = false)
        {
            var commandToUse = isWriteConnection && !String.IsNullOrWhiteSpace(connectionStringForWriting) ? CommandForWriting : CommandForReading;

            try
            {
                if (!gclSettings.LogOpeningAndClosingOfConnections && ((isWriteConnection && writeConnectionLogId == 0) || (!isWriteConnection && readConnectionLogId == 0)))
                {
                    return;
                }

                if (!logTableExists.HasValue || !logTableExists.Value)
                {
                    // Table for logging doesn't exist yet, don't do anything. The table gets created during startup, but that also uses this service for doing that.
                    // So the table obviously won't exist yet during startup and we don't want an error from that.
                    return;
                }

                if (commandToUse == null)
                {
                    if (isWriteConnection && !String.IsNullOrWhiteSpace(connectionStringForWriting))
                    {
                        commandToUse = CommandForWriting ??= ConnectionForWriting.CreateCommand();
                    }
                    else
                    {
                        commandToUse = CommandForReading ??= ConnectionForReading.CreateCommand();
                    }
                }

                if (commandToUse.Connection.State == ConnectionState.Closed)
                {
                    await commandToUse.Connection.OpenAsync();
                }

                if(commandToUse.Parameters.Contains("gclConnectionClosed")) commandToUse.Parameters.Remove("gclConnectionClosed");
                if(commandToUse.Parameters.Contains("gclConnectionId")) commandToUse.Parameters.Remove("gclConnectionId");
                commandToUse.Parameters.AddWithValue("gclConnectionClosed", DateTime.Now);
                commandToUse.Parameters.AddWithValue("gclConnectionId", isWriteConnection ? writeConnectionLogId : readConnectionLogId);
                commandToUse.CommandText = $"UPDATE {Constants.DatabaseConnectionLogTableName} SET closed = ?gclConnectionClosed WHERE id = ?gclConnectionId";
                await commandToUse.ExecuteNonQueryAsync();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error while trying to add connection close log.");
            }
            finally
            {
                if (disposeConnection)
                {
                    if (commandToUse != null)
                    {
                        await commandToUse.DisposeAsync();
                    }

                    var connection = (isWriteConnection ? ConnectionForWriting : ConnectionForReading);
                    if (connection != null)
                    {
                        await connection.DisposeAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether or not the log table (for logging the opening and closing of database connections) exists.
        /// </summary>
        /// <param name="command">The MySqlCommand to execute the query on to check if the table exists.</param>
        /// <returns>A boolean indicating whether the log table exists or not.</returns>
        private async Task<bool> LogTableExistsAsync(MySqlCommand command)
        {
            // Simple text file that indicates whether or not the log table exists, so that we don't have to execute an extra query every time.
            var cacheDirectory = FileSystemHelpers.GetContentCacheFolderPath(webHostEnvironment);
            // TODO: Use Constants.LogTableExistsCacheFileName instead of hardcoding the file name, once the GCL pull request has been approved.
            var filePath = cacheDirectory == null ? null : Path.Combine(cacheDirectory, String.Format("MySqlDatabaseConnection-LogTableExistsAsync-{0}.txt", (ConnectionForWriting ?? ConnectionForReading).Database));
            if (filePath != null && File.Exists(filePath))
            {
                return true;
            }

            var dataTable = new DataTable();
            command.CommandText = $"SELECT TABLE_NAME FROM information_schema.`TABLES` WHERE TABLE_NAME = '{Constants.DatabaseConnectionLogTableName}' AND TABLE_SCHEMA = '{(ConnectionForWriting ?? ConnectionForReading).Database.ToMySqlSafeValue(false)}'";
            using var dataAdapter = new MySqlDataAdapter(command);
            await dataAdapter.FillAsync(dataTable);

            if (dataTable.Rows.Count == 0)
            {
                return false;
            }

            if (filePath == null)
            {
                return true;
            }

            try
            {
                // Create the file to indicate that the table exists.
                await File.WriteAllTextAsync(filePath, "");
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, $"An error occurred while trying to create the file '{filePath}'.");
            }

            return true;
        }
    }
}