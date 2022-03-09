using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Models;
using Api.Modules.Customers.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace Api.Core.Services
{
    /// <summary>
    /// This is meant to be used as a decorator pattern. This will use the original <see cref="IDatabaseConnection"/> to access the main Wiser database and find the customer's info in easy_customers.
    /// It will then create a new connection string for that customer and open a connection to that database. This means that using this class, you will always have access to the customer database.
    /// </summary>
    public class ClientDatabaseConnection : IDatabaseConnection, IScopedService
    {
        public readonly IDatabaseConnection WiserDatabaseConnection;
        private readonly ApiSettings apiSettings;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<ClientDatabaseConnection> logger;

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

        private string subDomain;

        /// <summary>
        /// Creates a new instance of <see cref="ClientDatabaseConnection"/>.
        /// </summary>
        public ClientDatabaseConnection(IDatabaseConnection wiserDatabaseConnection, IOptions<GclSettings> gclSettings, IOptions<ApiSettings> apiSettings, IHttpContextAccessor httpContextAccessor, ILogger<ClientDatabaseConnection> logger)
        {
            this.gclSettings = gclSettings.Value;
            this.WiserDatabaseConnection = wiserDatabaseConnection;
            this.apiSettings = apiSettings.Value;
            this.httpContextAccessor = httpContextAccessor;
            this.logger = logger;
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
            return String.IsNullOrWhiteSpace(subDomain) || String.Equals(subDomain, CustomerConstants.MainSubDomain, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the connection string for the customer/client database. At least one of the parameters must contain a value.
        /// </summary>
        /// <param name="subDomain">The Wiser sub domain for the client/customer. You can leave this empty or set the value to "main", to get the connection string for the Wiser database.</param>
        /// <returns>The connection string for the customer/client database.</returns>
        public async Task<string> GetClientConnectionStringAsync(string subDomain)
        {
            this.subDomain = subDomain;
            if (IsMainDatabase())
            {
                return gclSettings.ConnectionString;
            }

            WiserDatabaseConnection.ClearParameters();
            WiserDatabaseConnection.AddParameter("subDomain", subDomain);
            var query = $"SELECT propertys, db_host, db_login, db_passencrypted, db_port, db_dbname, encryption_key FROM {ApiTableNames.WiserCustomers} WHERE subdomain = ?subDomain";

            var dataTable = await WiserDatabaseConnection.GetAsync(query);

            if (dataTable.Rows.Count == 0)
            {
                throw new Exception($"No customer record found for {(httpContextAccessor.HttpContext?.User.Identity is not ClaimsIdentity identity ? "Unknown" : IdentityHelpers.GetName(identity))}!");
            }

            var server = dataTable.Rows[0].Field<string>("db_host");
            var port = dataTable.Rows[0].Field<string>("db_port");
            var username = dataTable.Rows[0].Field<string>("db_login");
            var encryptedPassword = dataTable.Rows[0].Field<string>("db_passencrypted");
            var decryptedPassword = encryptedPassword.DecryptWithAesWithSalt(apiSettings.DatabasePasswordEncryptionKey);
            var database = dataTable.Rows[0].Field<string>("db_dbname");
            
            // Check if there is a custom database name set for the Wiser API. This is mainly for customers who have multiple databases.
            var customerProperties = dataTable.Rows[0].Field<string>("propertys")?.Replace("\r", "").Split('\n');
            var wiserApiDatabaseName = customerProperties?.FirstOrDefault(p => p.StartsWith("wiser_api_database_name=", StringComparison.Ordinal));
            if (wiserApiDatabaseName != null)
            {
                var propertyValue = wiserApiDatabaseName.Split('=')[1].Trim();
                if (propertyValue != "")
                {
                    database = propertyValue;
                }
            }

            // Use the default port number for MySQL (which is 3306) if it isn't set in the customer settings table.
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

                switch (mySqlException.ErrorCode)
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

                switch (mySqlException.ErrorCode)
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

                var reader = await commandToUse.ExecuteReaderAsync();
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

                switch (mySqlException.ErrorCode)
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
        public void Dispose()
        {
            dataReader?.Dispose();
            ConnectionForReading?.Dispose();
            CommandForReading?.Dispose();
            ConnectionForWriting?.Dispose();
            CommandForWriting?.Dispose();
            WiserDatabaseConnection?.Dispose();
        }

        /// <summary>
        /// If the connection is not open yet, open it.
        /// </summary>
        /// <returns></returns>
        public async Task EnsureOpenConnectionForReadingAsync()
        {
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

                if (String.IsNullOrWhiteSpace(connectionStringForWriting))
                {
                    return;
                }

                ConnectionForWriting = new MySqlConnection { ConnectionString = connectionStringForWriting };
                CommandForWriting = ConnectionForWriting.CreateCommand();
            }

            if (ConnectionForReading == null)
            {
                ConnectionForReading = new MySqlConnection { ConnectionString = connectionStringForReading };
                CommandForReading = ConnectionForReading.CreateCommand();
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

            if (ConnectionForWriting == null)
            {
                ConnectionForWriting = new MySqlConnection { ConnectionString = connectionStringForWriting };
                CommandForWriting = ConnectionForWriting.CreateCommand();
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
                    break;
            }
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
    }
}