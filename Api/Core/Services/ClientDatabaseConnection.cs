using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Exceptions;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Helpers;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Newtonsoft.Json;
using Renci.SshNet;
using ConnectionInfo = Renci.SshNet.ConnectionInfo;
using Constants = GeeksCoreLibrary.Modules.Databases.Models.Constants;

namespace Api.Core.Services;

/// <summary>
/// This is meant to be used as a decorator pattern. This will use the original <see cref="IDatabaseConnection"/> to access the main Wiser database and find the tenant's info in easy_customers.
/// It will then create a new connection string for that tenant and open a connection to that database. This means that using this class, you will always have access to the tenant database.
/// </summary>
public class ClientDatabaseConnection : IDatabaseConnection, IScopedService
{
    private const string Localhost = "127.0.0.1";

    /// <summary>
    /// A connection object to the Wiser Database.
    /// </summary>
    public readonly IDatabaseConnection WiserDatabaseConnection;
    
    private readonly ApiSettings _apiSettings;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ClientDatabaseConnection> _logger;
    private readonly IWebHostEnvironment _webHostEnvironment;

    private MySqlConnectionStringBuilder _connectionStringForReading;

    private SshSettings _sshSettingsForReading;

    private MySqlConnection _connectionForReading { get; set; }

    private SshClient _sshClientForReading { get; set; }

    private ForwardedPortLocal _forwardedPortLocalForReading { get; set; }

    private readonly GclSettings _gclSettings;

    private DbDataReader _dataReader;

    private MySqlTransaction _transaction;
    private int? _commandTimeout;

    private readonly ConcurrentDictionary<string, object> parameters = new();

    private readonly Guid _instanceId;
    private int _readConnectionLogId;
    private int _writeConnectionLogId;
    private bool? _logTableExists;

    private string _subDomain;

    /// <summary>
    /// Creates a new instance of <see cref="ClientDatabaseConnection"/>.
    /// </summary>
    public ClientDatabaseConnection(IDatabaseConnection wiserDatabaseConnection, IOptions<GclSettings> gclSettings, IOptions<ApiSettings> apiSettings, IHttpContextAccessor httpContextAccessor, ILogger<ClientDatabaseConnection> logger, IWebHostEnvironment webHostEnvironment)
    {
        _gclSettings = gclSettings.Value;
        WiserDatabaseConnection = wiserDatabaseConnection;
        _apiSettings = apiSettings.Value;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _webHostEnvironment = webHostEnvironment;
        _instanceId = Guid.NewGuid();
        _sshSettingsForReading = this._gclSettings.DatabaseSshSettings;
    }

    /// <inheritdoc />
    public bool HasActiveTransaction()
    {
        return _transaction != null;
    }

    /// <inheritdoc />
    public DbConnection GetConnectionForReading()
    {
        return _connectionForReading;
    }

    /// <inheritdoc />
    public DbConnection GetConnectionForWriting()
    {
        return _connectionForReading;
    }

    /// <inheritdoc />
    public Task<int> BulkInsertAsync(DataTable dataTable, string tableName, bool useWritingConnectionIfAvailable = true, bool useInsertIgnore = false)
    {
        throw new NotImplementedException();
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
        return String.IsNullOrWhiteSpace(_subDomain) || String.Equals(_subDomain, _apiSettings.MainSubDomain, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the connection string for the tenant/client database. At least one of the parameters must contain a value.
    /// </summary>
    /// <param name="subDomainToUse">The Wiser sub domain for the client/tenant. You can leave this empty or set the value to "main", to get the connection string for the Wiser database.</param>
    /// <returns>The connection string for the tenant/client database.</returns>
    public async Task<MySqlConnectionStringBuilder> GetClientConnectionStringAsync(string subDomainToUse)
    {
        _subDomain = subDomainToUse;
        if (IsMainDatabase())
        {
            return new MySqlConnectionStringBuilder(_gclSettings.ConnectionString);
        }

        WiserDatabaseConnection.ClearParameters();
        WiserDatabaseConnection.AddParameter("subDomain", subDomainToUse);
        var query = $"SELECT db_host, db_login, db_passencrypted, db_port, db_dbname, encryption_key FROM {ApiTableNames.WiserTenants} WHERE subdomain = ?subDomain";

        var dataTable = await WiserDatabaseConnection.GetAsync(query);

        if (dataTable.Rows.Count == 0)
        {
            throw new Exception($"No tenant record found for {(_httpContextAccessor.HttpContext?.User.Identity is not ClaimsIdentity identity ? "Unknown" : IdentityHelpers.GetName(identity))}!");
        }

        var server = dataTable.Rows[0].Field<string>("db_host");
        var port = dataTable.Rows[0].Field<string>("db_port");
        var username = dataTable.Rows[0].Field<string>("db_login");
        var encryptedPassword = dataTable.Rows[0].Field<string>("db_passencrypted");
        var decryptedPassword = encryptedPassword.DecryptWithAesWithSalt(_apiSettings.DatabasePasswordEncryptionKey);
        var database = dataTable.Rows[0].Field<string>("db_dbname");

        // Use the default port number for MySQL (which is 3306) if it isn't set in the tenant settings table.
        if (String.IsNullOrWhiteSpace(port))
        {
            port = "3306";
        }

        return new MySqlConnectionStringBuilder
        {
            Server = server,
            Port = Convert.ToUInt32(port),
            UserID = username,
            Password = decryptedPassword,
            Database = database,
            CharacterSet = "utf8",
            AllowUserVariables = true,
            ConvertZeroDateTime = true,
            IgnoreCommandTransaction = true
        };
    }

    /// <inheritdoc />
    public async Task<DbDataReader> GetReaderAsync(string query)
    {
        await EnsureOpenConnectionForReadingAsync();
        await using var command = new MySqlCommand(query, _connectionForReading);
        await SetupMySqlCommandAsync(command);
        _dataReader = await command.ExecuteReaderAsync();

        return _dataReader;
    }

    /// <inheritdoc />
    public async Task<T> ExecuteScalarAsync<T>(string query, bool skipCache = false, bool cleanUp = true, bool useWritingConnectionIfAvailable = false)
    {
        var result = await ExecuteScalarAsync(query, skipCache, cleanUp, useWritingConnectionIfAvailable);
        return result == null ? default : (T)Convert.ChangeType(result, typeof(T));
    }

    /// <inheritdoc />
    public async Task<object> ExecuteScalarAsync(string query, bool skipCache = false, bool cleanUp = true, bool useWritingConnectionIfAvailable = false)
    {
        return await ExecuteScalarAsync(query, 0, cleanUp, useWritingConnectionIfAvailable);
    }

    private async Task<object> ExecuteScalarAsync(string query, int retryCount, bool cleanUp = true, bool useWritingConnectionIfAvailable = false)
    {
        MySqlCommand commandToUse = null;
        try
        {
            await EnsureOpenConnectionForReadingAsync();
            commandToUse = new MySqlCommand(query, _connectionForReading);

            await SetupMySqlCommandAsync(commandToUse);

            commandToUse.CommandText = query;

            _logger.LogDebug("Query: {query}", query);

            return await commandToUse.ExecuteScalarAsync();
        }
        catch (InvalidOperationException invalidOperationException)
        {
            if (retryCount >= _gclSettings.MaximumRetryCountForQueries || !MySqlHelpers.IsErrorToRetry(invalidOperationException))
            {
                _logger.LogError(invalidOperationException, "Error trying to run this query: {query}", query);
                throw new GclQueryException("Error trying to run query", query, invalidOperationException);
            }

            await Task.Delay(_gclSettings.TimeToWaitBeforeRetryingQueryInMilliseconds);
            return await ExecuteScalarAsync(query, retryCount + 1, cleanUp, useWritingConnectionIfAvailable);
        }
        catch (MySqlException mySqlException)
        {
            // Never retry single queries if we're in a transaction, because transactions will get rolled back when a deadlock occurs,
            // so retrying a single query in a transaction is not very useful on most/all cases.
            // Also, if we've reached the maximum number of retries, don't retry anymore.
            if (HasActiveTransaction() || retryCount >= _gclSettings.MaximumRetryCountForQueries || !MySqlHelpers.IsErrorToRetry(mySqlException))
            {
                _logger.LogError(mySqlException, "Error trying to run this query: {query}", query);
                throw new GclQueryException("Error trying to run query", query, mySqlException);
            }

            // If we're not in a transaction, retry the query if it's a deadlock.
            await Task.Delay(_gclSettings.TimeToWaitBeforeRetryingQueryInMilliseconds);
            return await ExecuteScalarAsync(query, retryCount + 1, cleanUp, useWritingConnectionIfAvailable);
        }
        finally
        {
            if (commandToUse != null)
            {
                await commandToUse.DisposeAsync();
            }

            // If we're not using transactions, dispose everything here. Otherwise we will dispose it when the transaction gets committed or roll backed.
            if (!HasActiveTransaction() && cleanUp)
            {
                await CleanUpAsync();
            }
        }
    }

    /// <inheritdoc />
    public Task<DataTable> GetAsync(string query, bool skipCache = false, bool cleanUp = true, bool useWritingConnectionIfAvailable = false)
    {
        return GetAsync(query, 0, cleanUp);
    }

    private async Task<DataTable> GetAsync(string query, int retryCount, bool cleanUp = true)
    {
        MySqlCommand commandToUse = null;
        try
        {
            await EnsureOpenConnectionForReadingAsync();
            commandToUse = new MySqlCommand(query, _connectionForReading);

            await SetupMySqlCommandAsync(commandToUse);

            var result = new DataTable();
            commandToUse.CommandText = query;
            using var dataAdapter = new MySqlDataAdapter(commandToUse);
            dataAdapter.Fill(result);

            _logger.LogDebug("Query: {query}", query);

            return result;
        }
        catch (InvalidOperationException invalidOperationException)
        {
            if (retryCount >= _gclSettings.MaximumRetryCountForQueries || !invalidOperationException.Message.Contains("This MySqlConnection is already in use", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(invalidOperationException, "Error trying to run this query: {query}", query);
                throw new GclQueryException("Error trying to run query", query, invalidOperationException);
            }

            await Task.Delay(_gclSettings.TimeToWaitBeforeRetryingQueryInMilliseconds);
            return await GetAsync(query, retryCount + 1, cleanUp);
        }
        catch (MySqlException mySqlException)
        {
            // Never retry single queries if we're in a transaction, because transactions will get rolled back when a deadlock occurs,
            // so retrying a single query in a transaction is not very useful on most/all cases.
            // Also, if we've reached the maximum number of retries, don't retry anymore.
            if (HasActiveTransaction() || retryCount >= _gclSettings.MaximumRetryCountForQueries || !MySqlHelpers.IsErrorToRetry(mySqlException))
            {
                _logger.LogError(mySqlException, "Error trying to run this query: {query}", query);
                throw new GclQueryException("Error trying to run query", query, mySqlException);
            }

            // If we're not in a transaction, retry the query if it's a deadlock.
            await Task.Delay(_gclSettings.TimeToWaitBeforeRetryingQueryInMilliseconds);
            return await GetAsync(query, retryCount + 1, cleanUp);
        }
        finally
        {
            if (commandToUse != null)
            {
                await commandToUse.DisposeAsync();
            }

            // If we're not using transactions, dispose everything here. Otherwise we will dispose it when the transaction gets committed or roll backed.
            if (!HasActiveTransaction() && cleanUp)
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
        return ExecuteAsync(query, 0, cleanUp);
    }

    /// <summary>
    /// Executes a query and returns the amount of rows affected.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="retryCount">How many times the query has been attempted.</param>
    /// <param name="cleanUp">Clean up after the query has been completed.</param>
    /// <returns></returns>
    private async Task<int> ExecuteAsync(string query, int retryCount, bool cleanUp = true)
    {
        MySqlCommand commandToUse = null;
        try
        {
            await EnsureOpenConnectionForReadingAsync();
            commandToUse = new MySqlCommand(query, _connectionForReading);

            await SetupMySqlCommandAsync(commandToUse);

            commandToUse.CommandText = query;
            _logger.LogDebug("Query: {query}", query);
            return await commandToUse.ExecuteNonQueryAsync();
        }
        catch (InvalidOperationException invalidOperationException)
        {
            if (retryCount >= _gclSettings.MaximumRetryCountForQueries || !invalidOperationException.Message.Contains("This MySqlConnection is already in use", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(invalidOperationException, "Error trying to run this query: {query}", query);
                throw new GclQueryException("Error trying to run query", query, invalidOperationException);
            }

            await Task.Delay(_gclSettings.TimeToWaitBeforeRetryingQueryInMilliseconds);
            return await ExecuteAsync(query, retryCount + 1, cleanUp);
        }
        catch (MySqlException mySqlException)
        {
            // Never retry single queries if we're in a transaction, because transactions will get rolled back when a deadlock occurs,
            // so retrying a single query in a transaction is not very useful on most/all cases.
            // Also, if we've reached the maximum number of retries, don't retry anymore.
            if (HasActiveTransaction() || retryCount >= _gclSettings.MaximumRetryCountForQueries || !MySqlHelpers.IsErrorToRetry(mySqlException))
            {
                _logger.LogError(mySqlException, "Error trying to run this query: {query}", query);
                throw new GclQueryException("Error trying to run query", query, mySqlException);
            }

            // If we're not in a transaction, retry the query if it's a deadlock.
            await Task.Delay(_gclSettings.TimeToWaitBeforeRetryingQueryInMilliseconds);
            return await ExecuteAsync(query, retryCount + 1, cleanUp);
        }
        finally
        {
            if (commandToUse != null)
            {
                await commandToUse.DisposeAsync();
            }

            // If we're not using transactions, dispose everything here. Otherwise we will dispose it when the transaction gets comitted or rollbacked.
            if (_transaction == null && cleanUp)
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

        if (!idIsDefaultValue)
        {
            await ExecuteAsync(query.ToString(), useWritingConnectionIfAvailable, false);
            return id;
        }

        query.Append("; SELECT LAST_INSERT_ID()");
        var result = await GetAsync(query.ToString(), useWritingConnectionIfAvailable: useWritingConnectionIfAvailable);
        return (T)Convert.ChangeType(result.Rows[0][0], typeof(T));
    }

    /// <inheritdoc />
    public Task<long> InsertRecordAsync(string query, bool useWritingConnectionIfAvailable = true)
    {
        return InsertRecordAsync(query, 0);
    }

    private async Task<long> InsertRecordAsync(string query, int retryCount)
    {
        if (String.IsNullOrWhiteSpace(query))
        {
            return 0L;
        }

        MySqlCommand commandToUse = null;
        try
        {
            var finalQuery = new StringBuilder(query.TrimEnd());
            if (finalQuery[^1] != ';')
            {
                finalQuery.Append(';');
            }

            // Add the query to retrieve the last inserted ID to the query that was passed to the function.
            finalQuery.Append("SELECT LAST_INSERT_ID();");

            await EnsureOpenConnectionForReadingAsync();
            commandToUse = new MySqlCommand(finalQuery.ToString(), _connectionForReading);

            await SetupMySqlCommandAsync(commandToUse);

            await using var reader = await commandToUse.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return 0L;
            }

            return Int64.TryParse(Convert.ToString(reader.GetValue(0)), out var tempId) ? tempId : 0L;
        }
        catch (InvalidOperationException invalidOperationException)
        {
            if (retryCount >= _gclSettings.MaximumRetryCountForQueries || !invalidOperationException.Message.Contains("This MySqlConnection is already in use", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(invalidOperationException, "Error trying to run this query: {query}", query);
                throw new GclQueryException("Error trying to run query", query, invalidOperationException);
            }

            await Task.Delay(_gclSettings.TimeToWaitBeforeRetryingQueryInMilliseconds);
            return await InsertRecordAsync(query, retryCount + 1);
        }
        catch (MySqlException mySqlException)
        {
            // Never retry single queries if we're in a transaction, because transactions will get rolled back when a deadlock occurs,
            // so retrying a single query in a transaction is not very useful on most/all cases.
            // Also, if we've reached the maximum number of retries, don't retry anymore.
            if (HasActiveTransaction() || retryCount >= _gclSettings.MaximumRetryCountForQueries || !MySqlHelpers.IsErrorToRetry(mySqlException))
            {
                _logger.LogError(mySqlException, "Error trying to run this query: {query}", query);
                throw new GclQueryException("Error trying to run query", query, mySqlException);
            }

            // If we're not in a transaction, retry the query if it's a deadlock.
            await Task.Delay(_gclSettings.TimeToWaitBeforeRetryingQueryInMilliseconds);
            return await InsertRecordAsync(query, retryCount + 1);
        }
        finally
        {
            if (commandToUse != null)
            {
                await commandToUse.DisposeAsync();
            }

            // If we're not using transactions, dispose everything here. Otherwise we will dispose it when the transaction gets comitted or rollbacked.
            if (_transaction == null)
            {
                await CleanUpAsync();
            }
        }
    }

    /// <inheritdoc />
    public async Task<IDbTransaction> BeginTransactionAsync(bool forceNewTransaction = false)
    {
        if (!forceNewTransaction && _transaction != null)
        {
            throw new InvalidOperationException("Called BeginTransaction, but there already is an active transaction.");
        }

        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
        }

        // If we're using transactions, make sure to use it on the write connection, if we have one.
        MySqlConnection connectionToUse;
        await EnsureOpenConnectionForReadingAsync();
        connectionToUse = _connectionForReading;

        _transaction = await connectionToUse.BeginTransactionAsync();

        return _transaction;
    }

    /// <inheritdoc />
    public async Task CommitTransactionAsync(bool throwErrorIfNoActiveTransaction = true)
    {
        if (_transaction == null)
        {
            if (throwErrorIfNoActiveTransaction)
            {
                throw new InvalidOperationException("Called CommitTransactionAsync, but there is no active transaction.");
            }

            return;
        }

        await _transaction.CommitAsync();

        // Dispose and set to null, so that we know there is no more active transaction.
        await _transaction.DisposeAsync();
        _transaction = null;

        await CleanUpAsync();
    }

    /// <inheritdoc />
    public async Task RollbackTransactionAsync(bool throwErrorIfNoActiveTransaction = true)
    {
        if (_transaction == null)
        {
            if (throwErrorIfNoActiveTransaction)
            {
                throw new InvalidOperationException("Called RollbackTransactionAsync, but there is no active transaction.");
            }

            return;
        }

        await _transaction.RollbackAsync();

        // Dispose and set to null, so that we know there is no more active transaction.
        await _transaction.DisposeAsync();
        _transaction = null;

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
        if (_dataReader != null)
        {
            await _dataReader.DisposeAsync();
        }

        if (_connectionForReading != null)
        {
            await AddConnectionCloseLogAsync(false, true);
        }

        if (WiserDatabaseConnection != null)
        {
            await WiserDatabaseConnection.DisposeAsync();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _logger.LogTrace($"Disposing instance of MySqlDatabaseConnection with ID '{_instanceId}' on URL {HttpContextHelpers.GetOriginalRequestUri(_httpContextAccessor.HttpContext)}");
        _dataReader?.Dispose();
        _ = AddConnectionCloseLogAsync(false, true);
        _ = AddConnectionCloseLogAsync(true, true);
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
        if (string.IsNullOrWhiteSpace(_connectionStringForReading?.ConnectionString))
        {
            if (_httpContextAccessor.HttpContext == null)
            {
                throw new ArgumentNullException(nameof(_httpContextAccessor.HttpContext));
            }

            if (_httpContextAccessor.HttpContext.User.Identity is ClaimsIdentity identity && identity.Claims.Any())
            {
                _subDomain = IdentityHelpers.GetSubDomain(identity);
            }

            if (string.IsNullOrWhiteSpace(_subDomain))
            {
                _subDomain = (string)_httpContextAccessor.HttpContext.Items[HttpContextConstants.SubDomainKey];
            }

            if (string.IsNullOrWhiteSpace(_subDomain) && _httpContextAccessor.HttpContext.Request.Path.StartsWithSegments("/health"))
            {
                _subDomain = _apiSettings.MainSubDomain;
            }

            if (string.IsNullOrWhiteSpace(_subDomain))
            {
                throw new Exception("No sub domain found!");
            }

            _connectionStringForReading = await GetClientConnectionStringAsync(_subDomain);
            
            if (string.IsNullOrWhiteSpace(_connectionStringForReading?.ConnectionString))
            {
                throw new Exception("No connection string found!");
            }

            var (sshClient, localPort, forwardedPortLocal) = ConnectToSsh(_sshSettingsForReading, _connectionStringForReading.Server, _connectionStringForReading.Port);
            _sshClientForReading = sshClient;
            _forwardedPortLocalForReading = forwardedPortLocal;
            if (sshClient != null)
            {
                _connectionStringForReading.Server = Localhost;
                _connectionStringForReading.Port = localPort;
            }

            _connectionForReading = new MySqlConnection { ConnectionString = _connectionStringForReading.ConnectionString };
            createdNewConnection = true;
        }

        if (_connectionForReading == null)
        {
            var (sshClient, localPort, forwardedPortLocal) = ConnectToSsh(_sshSettingsForReading, _connectionStringForReading.Server, _connectionStringForReading.Port);
            _sshClientForReading = sshClient;
            _forwardedPortLocalForReading = forwardedPortLocal;
            if (sshClient != null)
            {
                _connectionStringForReading.Server = Localhost;
                _connectionStringForReading.Port = localPort;
            }

            _connectionForReading = new MySqlConnection { ConnectionString = _connectionStringForReading.ConnectionString };
            createdNewConnection = true;
        }

        // Remember the database name that was connected to.
        ConnectedDatabase = _connectionForReading.Database;

        if (_connectionForReading.State != ConnectionState.Closed)
        {
            return;
        }

        await _connectionForReading.OpenAsync();

        await SetTimezone(_connectionForReading);
        await SetCharacterSetAndCollationAsync(_connectionForReading);

        if (createdNewConnection)
        {
            // Log the opening of the connection.
            await AddConnectionOpenLogAsync(isWriteConnection: true);
        }
    }

    /// <summary>
    /// If the connection is not open yet, open it.
    /// </summary>
    /// <returns></returns>
    public async Task EnsureOpenConnectionForWritingAsync()
    {
        await EnsureOpenConnectionForReadingAsync();
    }

    /// <inheritdoc />
    public async Task ChangeConnectionStringsAsync(
        string newConnectionStringForReading,
        string newConnectionStringForWriting = null, 
        SshSettings sshSettingsForReading = null, 
        SshSettings sshSettingsForWriting = null)
    {
        _connectionStringForReading ??= new MySqlConnectionStringBuilder();

        _connectionStringForReading.ConnectionString = newConnectionStringForReading;
        _connectionStringForReading.IgnoreCommandTransaction = true;
        _sshSettingsForReading = sshSettingsForReading;

        await CleanUpAsync();
        
        if (_connectionForReading != null)
        {
            await AddConnectionCloseLogAsync(false);
            await _connectionForReading.DisposeAsync();
        }

        if (_sshClientForReading != null)
        {
            _sshClientForReading.Dispose();
            _sshClientForReading = null;
        }

        if (_forwardedPortLocalForReading != null)
        {
            _forwardedPortLocalForReading.Dispose();
            _forwardedPortLocalForReading = null;
        }

        _connectionForReading = null;
    }

    /// <inheritdoc />
    public string GetDatabaseNameForCaching(bool writeDatabase = false)
    {
        return $"{_connectionStringForReading["server"]}_{_connectionStringForReading["database"]}";
    }

    /// <inheritdoc />
    public void SetCommandTimeout(int value)
    {
        _commandTimeout = value;
    }

    private async Task CleanUpAsync()
    {
        if (_dataReader != null)
        {
            await _dataReader.DisposeAsync();
        }

        _dataReader = null;
    }

    /// <summary>
    /// Add a mention to the log table that a connection to the database has been opened.
    /// </summary>
    /// <param name="isWriteConnection">Is this a write connection (true) or a read connection (false)?</param>
    private async Task AddConnectionOpenLogAsync(bool isWriteConnection)
    {
        try
        {
            if (!_gclSettings.LogOpeningAndClosingOfConnections)
            {
                return;
            }

            await using var commandToUse = new MySqlCommand("", _connectionForReading);

            _logTableExists ??= await LogTableExistsAsync(commandToUse);

            if (!_logTableExists.Value)
            {
                // Table for logging doesn't exist yet, don't do anything. The table gets created during startup, but that also uses this service for doing that.
                // So the table obviously won't exist yet during startup and we don't want an error from that.
                return;
            }

            var url = "";
            var httpMethod = "";
            if (_httpContextAccessor.HttpContext != null)
            {
                url = HttpContextHelpers.GetOriginalRequestUri(_httpContextAccessor.HttpContext).ToString();
                httpMethod = _httpContextAccessor.HttpContext.Request.Method;
            }

            if(commandToUse.Parameters.Contains("gclConnectionOpened"))
            {
                commandToUse.Parameters.Remove("gclConnectionOpened");
            }

            if(commandToUse.Parameters.Contains("gclConnectionUrl"))
            {
                commandToUse.Parameters.Remove("gclConnectionUrl");
            }

            if(commandToUse.Parameters.Contains("gclConnectionHttpMethod"))
            {
                commandToUse.Parameters.Remove("gclConnectionHttpMethod");
            }

            if(commandToUse.Parameters.Contains("gclConnectionInstanceId"))
            {
                commandToUse.Parameters.Remove("gclConnectionInstanceId");
            }

            if(commandToUse.Parameters.Contains("gclConnectionType"))
            {
                commandToUse.Parameters.Remove("gclConnectionType");
            }

            commandToUse.Parameters.AddWithValue("gclConnectionOpened", DateTime.Now);
            commandToUse.Parameters.AddWithValue("gclConnectionUrl", url);
            commandToUse.Parameters.AddWithValue("gclConnectionHttpMethod", httpMethod);
            commandToUse.Parameters.AddWithValue("gclConnectionInstanceId", _instanceId);
            commandToUse.Parameters.AddWithValue("gclConnectionType", isWriteConnection ? "write" : "read");

            commandToUse.CommandText = $"""
                                        INSERT INTO {Constants.DatabaseConnectionLogTableName} (opened, url, http_method, database_service_instance_id, type)
                                        VALUES (?gclConnectionOpened, ?gclConnectionUrl, ?gclConnectionHttpMethod, ?gclConnectionInstanceId, ?gclConnectionType);
                                        SELECT LAST_INSERT_ID();
                                        """;
            await using var reader = await commandToUse.ExecuteReaderAsync();
            var id = !await reader.ReadAsync() ? 0 : (Int32.TryParse(Convert.ToString(reader.GetValue(0)), out var tempId) ? tempId : 0);

            if (isWriteConnection)
            {
                _writeConnectionLogId = id;
            }
            else
            {
                _readConnectionLogId = id;
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error while trying to add connection open log.");
        }
    }

    /// <summary>
    /// Add a mention to the log table that a connection to the database has been closed.
    /// </summary>
    /// <param name="isWriteConnection">Is this a write connection (true) or a read connection (false)?</param>
    /// <param name="disposeConnection">Set to true to dispose the connection at the end.</param>
    private async Task AddConnectionCloseLogAsync(bool isWriteConnection, bool disposeConnection = false)
    {
        try
        {
            if (!_gclSettings.LogOpeningAndClosingOfConnections && ((isWriteConnection && _writeConnectionLogId == 0) || (!isWriteConnection && _readConnectionLogId == 0)))
            {
                return;
            }

            if (!_logTableExists.HasValue || !_logTableExists.Value)
            {
                // Table for logging doesn't exist yet, don't do anything. The table gets created during startup, but that also uses this service for doing that.
                // So the table obviously won't exist yet during startup and we don't want an error from that.
                return;
            }

            await using var commandToUse = new MySqlCommand("", _connectionForReading);

            if (commandToUse.Connection is { State: ConnectionState.Closed })
            {
                await commandToUse.Connection.OpenAsync();
            }

            if(commandToUse.Parameters.Contains("gclConnectionClosed"))
            {
                commandToUse.Parameters.Remove("gclConnectionClosed");
            }

            if(commandToUse.Parameters.Contains("gclConnectionId"))
            {
                commandToUse.Parameters.Remove("gclConnectionId");
            }

            commandToUse.Parameters.AddWithValue("gclConnectionClosed", DateTime.Now);
            commandToUse.Parameters.AddWithValue("gclConnectionId", isWriteConnection ? _writeConnectionLogId : _readConnectionLogId);
            commandToUse.CommandText = $"UPDATE {Constants.DatabaseConnectionLogTableName} SET closed = ?gclConnectionClosed WHERE id = ?gclConnectionId";
            await commandToUse.ExecuteNonQueryAsync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error while trying to add connection close log.");
        }
        finally
        {
            if (disposeConnection)
            {
                var connection = _connectionForReading;
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
        var cacheDirectory = FileSystemHelpers.GetOutputCacheDirectory(_webHostEnvironment);
        var filePath = cacheDirectory == null ? null : Path.Combine(cacheDirectory, String.Format(Constants.LogTableExistsCacheFileName, _connectionForReading.Database));
        if (filePath != null && File.Exists(filePath))
        {
            return true;
        }

        if (_webHostEnvironment == null && cacheDirectory != null && !Directory.Exists(cacheDirectory))
        {
            try
            {
                Directory.CreateDirectory(cacheDirectory);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, $"An error occurred while trying to create the directory '{cacheDirectory}'.");
                filePath = null;
            }
        }

        var dataTable = new DataTable();
        command.CommandText = $"SELECT TABLE_NAME FROM information_schema.`TABLES` WHERE TABLE_NAME = '{Constants.DatabaseConnectionLogTableName}' AND TABLE_SCHEMA = '{_connectionForReading.Database.ToMySqlSafeValue(false)}'";
        using var dataAdapter = new MySqlDataAdapter(command);
        dataAdapter.Fill(dataTable);

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
            _logger.LogWarning(exception, $"An error occurred while trying to create the file '{filePath}'.");
        }

        return true;
    }

    private async Task SetTimezone(MySqlConnection connection)
    {
        try
        {
            // Make sure we always use the correct timezone.
            if (!String.IsNullOrWhiteSpace(_gclSettings.DatabaseTimeZone))
            {
                await using var command = new MySqlCommand($"SET @@time_zone = {_gclSettings.DatabaseTimeZone.ToMySqlSafeValue(true)};", connection);
                await command.ExecuteNonQueryAsync();
            }
        }
        catch (MySqlException mySqlException)
        {
            // Checks if the exception is about the timezone or something else related to MySQL.
            // Not setting timezones when they are not available should not be logged as en error.
            if (mySqlException.Number == 1298)
            {
                _logger.LogInformation($"The time zone is not set to '{_gclSettings.DatabaseTimeZone}', because that timezone is not available in the database.");
            }
            else
            {
                _logger.LogWarning(mySqlException, $"An error occurred while trying to set the time zone to '{_gclSettings.DatabaseTimeZone}'");
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, $"An error occurred while trying to set the time zone to '{_gclSettings.DatabaseTimeZone}'");
        }
    }

    /// <summary>
    /// Sets the correct character set and collation for the database connection.
    /// </summary>
    /// <param name="connection">The <see cref="MySqlConnection"/> object that will execute the query.</param>
    private async Task SetCharacterSetAndCollationAsync(MySqlConnection connection)
    {
        try
        {
            var characterSet = !String.IsNullOrWhiteSpace(_gclSettings.DatabaseCharacterSet) ? _gclSettings.DatabaseCharacterSet : "utf8mb4";
            var collation = !String.IsNullOrWhiteSpace(_gclSettings.DatabaseCollation) ? _gclSettings.DatabaseCollation : "utf8mb4_general_ci";

            // Make sure we always use the correct timezone.
            if (!String.IsNullOrWhiteSpace(_gclSettings.DatabaseTimeZone))
            {
                await using var command = new MySqlCommand($"SET NAMES {characterSet} COLLATE {collation};", connection);
                await command.ExecuteNonQueryAsync();
            }
        }
        catch (MySqlException mySqlException)
        {
            _logger.LogWarning(mySqlException, $"An error occurred while trying to set the character set to '{_gclSettings.DatabaseCharacterSet}' and the collation to '{_gclSettings.DatabaseCollation}'");
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, $"An error occurred while trying to set the character set to '{_gclSettings.DatabaseCharacterSet}' and the collation to '{_gclSettings.DatabaseCollation}'");
        }
    }

    /// <summary>
    /// Setups a <see cref="MySqlCommand"/> by doing the following things:
    /// - Copy all current parameters to the given command.
    /// - Set the transaction on the command.
    /// - Set the command timeout.
    /// - Wait until the connection is no longer in the state "Connecting".
    /// </summary>
    /// <param name="command">The <see cref="MySqlCommand"/> to copy the parameters to.</param>
    private async Task SetupMySqlCommandAsync(MySqlCommand command)
    {
        // Copy all current parameters to the given command.
        foreach (var parameter in parameters)
        {
            if (command.Parameters.Contains(parameter.Key))
            {
                command.Parameters.RemoveAt(parameter.Key);
            }

            command.Parameters.AddWithValue(parameter.Key, parameter.Value);
        }

        // MySqlConnector wants us to set the transaction on the command, so that it knows which transaction to use.
        if (_transaction != null)
        {
            command.Transaction = _transaction;
        }

        // Set the command timeout.
        if (_commandTimeout.HasValue)
        {
            command.CommandTimeout = _commandTimeout.Value;
        }

        // Sometimes, the connection is in the state "Connecting", which causes exceptions if we then try to execute a query.
        var counter = 0;
        while (command.Connection?.State == ConnectionState.Connecting && counter < 100)
        {
            await Task.Delay(10);
            counter++;
        }
    }

    /// <summary>
    /// Connect to an SSH tunnel/server and forward the MySQL port through this tunnel.
    /// If no SSH settings are given, then this method will return null and will not setup an SSH tunnel.
    /// </summary>
    /// <param name="sshSettings">The SSH settings to use.</param>
    /// <param name="databaseServer">The host of the database that requires an SSH tunnel.</param>
    /// <param name="databasePort">The database port to use.</param>
    /// <returns>The <see cref="SshClient"/> and the port of the SSH tunnel.</returns>
    /// <exception cref="ArgumentException">When not all required settings have been set.</exception>
    private (SshClient sshClient, uint BoundPort, ForwardedPortLocal ForwardedPortLocal) ConnectToSsh(SshSettings sshSettings, string databaseServer, uint databasePort)
    {
        // Return null if we have no SSH settings.
        if (String.IsNullOrEmpty(sshSettings?.Host) || String.IsNullOrEmpty(sshSettings.Username))
        {
            return (null, 0, null);
        }

        // Make sure that the settings are fully set.
        if (String.IsNullOrEmpty(sshSettings.Password) && String.IsNullOrEmpty(sshSettings.PrivateKeyPath))
        {
            throw new ArgumentException($"One of {nameof(sshSettings.Password)} and {nameof(sshSettings.PrivateKeyPath)} must be specified.");
        }

        // Define the authentication methods to use (in order).
        var authenticationMethods = new List<AuthenticationMethod>();
        if (!String.IsNullOrEmpty(sshSettings.PrivateKeyPath))
        {
            authenticationMethods.Add(new PrivateKeyAuthenticationMethod(sshSettings.Username, new PrivateKeyFile(sshSettings.PrivateKeyPath, String.IsNullOrEmpty(sshSettings.PrivateKeyPassphrase) ? null : sshSettings.PrivateKeyPassphrase)));
        }

        if (!String.IsNullOrEmpty(sshSettings.Password))
        {
            authenticationMethods.Add(new PasswordAuthenticationMethod(sshSettings.Username, sshSettings.Password));
        }

        // Connect to the SSH server.
        var sshClient = new SshClient(new ConnectionInfo(sshSettings.Host, sshSettings.Port, sshSettings.Username, authenticationMethods.ToArray()));

        // Validate the finger print, if we know which finger print the host is supposed to have.
        if (!String.IsNullOrWhiteSpace(sshSettings.ExpectedFingerPrint))
        {
            sshClient.HostKeyReceived += (sender, hostKeyEventArgs) => { hostKeyEventArgs.CanTrust = sshSettings.ExpectedFingerPrint.Equals(hostKeyEventArgs.FingerPrintSHA256); };
        }
        else
        {
            _logger.LogWarning("No expected finger print is set for the SSH connection. This means that the connection will not be validated and man-in-the-middle attacks might be possible.");
        }

        sshClient.Connect();

        // Forward a local port to the database server and port, using the SSH server.
        var forwardedPort = new ForwardedPortLocal(Localhost, databaseServer, databasePort);
        sshClient.AddForwardedPort(forwardedPort);
        forwardedPort.Start();

        return (sshClient, forwardedPort.BoundPort, forwardedPort);
    }
}