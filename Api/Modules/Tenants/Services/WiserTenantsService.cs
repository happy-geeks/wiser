using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Models;
using Api.Core.Services;
using Api.Modules.Tenants.Enums;
using Api.Modules.Tenants.Interfaces;
using Api.Modules.Tenants.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;

namespace Api.Modules.Tenants.Services
{
    /// <summary>
    /// Service for operations related to Wiser tenants.
    /// </summary>
    public class WiserTenantsService : IWiserTenantsService, IScopedService
    {
        #region Private fields

        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IDatabaseHelpersService databaseHelpersService;
        private readonly ILogger<WiserTenantsService> logger;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly GclSettings gclSettings;
        private readonly ApiSettings apiSettings;
        private readonly IDatabaseConnection wiserDatabaseConnection;

        #endregion

        /// <summary>
        /// Creates a new instance of WiserTenantsService.
        /// </summary>
        public WiserTenantsService(IDatabaseConnection connection, IOptions<ApiSettings> apiSettings, IOptions<GclSettings> gclSettings, IDatabaseHelpersService databaseHelpersService, ILogger<WiserTenantsService> logger, IHttpContextAccessor httpContextAccessor)
        {
            clientDatabaseConnection = connection;
            this.databaseHelpersService = databaseHelpersService;
            this.logger = logger;
            this.httpContextAccessor = httpContextAccessor;
            this.gclSettings = gclSettings.Value;
            this.apiSettings = apiSettings.Value;

            if (clientDatabaseConnection is ClientDatabaseConnection databaseConnection)
            {
                wiserDatabaseConnection = databaseConnection.WiserDatabaseConnection;
            }
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TenantModel>> GetSingleAsync(ClaimsIdentity identity, bool includeDatabaseInformation = false)
        {
            var subDomain = IdentityHelpers.GetSubDomain(identity);
            if (IsMainDatabase(subDomain))
            {
                var connectionString = new DbConnectionStringBuilder
                {
                    ConnectionString = gclSettings.ConnectionString
                };

                return new ServiceResult<TenantModel>(new TenantModel
                {
                    TenantId = 1,
                    Id = 1,
                    Name = "Main",
                    SubDomain = apiSettings.MainSubDomain,
                    EncryptionKey = String.IsNullOrWhiteSpace(gclSettings.ExpiringEncryptionKey) ? gclSettings.DefaultEncryptionKey : gclSettings.ExpiringEncryptionKey,
                    Database = new ConnectionInformationModel
                    {
                        DatabaseName = connectionString["database"].ToString(),
                        PortNumber = Convert.ToInt32(connectionString["port"]),
                        Host = connectionString["server"].ToString(),
                        Username = connectionString["uid"].ToString(),
                        Password = connectionString["pwd"].ToString()
                    }
                });
            }

            // Get the tenant data.
            wiserDatabaseConnection.ClearParameters();
            wiserDatabaseConnection.AddParameter("name", subDomain);

            var query = $@"SELECT
                            id,
                            customerid,
                            name,
                            {(IdentityHelpers.IsTestEnvironment(identity) ? "encryption_key_test" : "encryption_key")} AS encryption_key,
                            subdomain,
                            wiser_title
                            {(includeDatabaseInformation ? ", db_host, db_login, db_passencrypted, db_port, db_dbname" : "")}
                        FROM {ApiTableNames.WiserTenants} 
                        WHERE subdomain = ?name";

            var tenantsDataTable = await wiserDatabaseConnection.GetAsync(query);
            if (tenantsDataTable.Rows.Count == 0)
            {
                return new ServiceResult<TenantModel>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = $"Tenant with sub domain '{subDomain}' not found."
                };
            }

            var result = TenantModel.FromDataRow(tenantsDataTable.Rows[0]);
            return new ServiceResult<TenantModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TenantModel>> GetSingleAsync(int id, bool includeDatabaseInformation = false)
        {
            // Get the tenant data.
            wiserDatabaseConnection.ClearParameters();
            wiserDatabaseConnection.AddParameter("id", id);

            var query = $@"SELECT
                            id,
                            customerid,
                            name,
                            encryption_key,
                            subdomain,
                            wiser_title
                            {(includeDatabaseInformation ? ", db_host, db_login, db_passencrypted, db_port, db_dbname" : "")}
                        FROM {ApiTableNames.WiserTenants} 
                        WHERE id = ?id";

            var tenantsDataTable = await wiserDatabaseConnection.GetAsync(query);
            if (tenantsDataTable.Rows.Count == 0)
            {
                return new ServiceResult<TenantModel>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = $"Tenant with ID '{id}' not found."
                };
            }

            var result = TenantModel.FromDataRow(tenantsDataTable.Rows[0]);
            return new ServiceResult<TenantModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> GetEncryptionKey(ClaimsIdentity identity, bool forceLiveKey = false)
        {
            var subDomain = IdentityHelpers.GetSubDomain(identity);
            if (IsMainDatabase(subDomain))
            {
                return new ServiceResult<string>(String.IsNullOrWhiteSpace(apiSettings.DatabasePasswordEncryptionKey) ? gclSettings.DefaultEncryptionKey : apiSettings.DatabasePasswordEncryptionKey);
            }

            // Get the tenant data.
            wiserDatabaseConnection.ClearParameters();
            wiserDatabaseConnection.AddParameter("name", subDomain);

            var query = $@"SELECT {(IdentityHelpers.IsTestEnvironment(identity) && !forceLiveKey ? "encryption_key_test" : "encryption_key")} AS encryption_key
                        FROM {ApiTableNames.WiserTenants} 
                        WHERE subdomain = ?name";

            var tenantsDataTable = await wiserDatabaseConnection.GetAsync(query);
            if (tenantsDataTable.Rows.Count == 0)
            {
                return new ServiceResult<string>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = $"Tenant with sub domain '{subDomain}' not found."
                };
            }

            return new ServiceResult<string>(tenantsDataTable.Rows[0].Field<string>("encryption_key"));
        }

        /// <inheritdoc />
        public async Task<T> DecryptValue<T>(string encryptedValue, ClaimsIdentity identity)
        {
            var tenant = await GetSingleAsync(identity);

            return DecryptValue<T>(encryptedValue, tenant.ModelObject);
        }

        /// <inheritdoc />
        public T DecryptValue<T>(string encryptedValue, TenantModel tenant)
        {
            return String.IsNullOrWhiteSpace(encryptedValue) ? default : (T)Convert.ChangeType(encryptedValue.Replace(" ", "+").DecryptWithAesWithSalt(tenant.EncryptionKey, true), typeof(T));
        }

        /// <inheritdoc />
        public async Task<string> EncryptValue(object valueToEncrypt, ClaimsIdentity identity)
        {
            var tenant = await GetSingleAsync(identity);

            return EncryptValue(valueToEncrypt, tenant.ModelObject);
        }

        /// <inheritdoc />
        public string EncryptValue(object valueToEncrypt, TenantModel tenant)
        {
            return valueToEncrypt?.ToString().EncryptWithAesWithSalt(tenant.EncryptionKey, withDateTime: true);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TenantExistsResults>> TenantExistsAsync(string name, string subDomain)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (String.IsNullOrWhiteSpace(subDomain))
            {
                throw new ArgumentNullException(nameof(subDomain));
            }

            if (IsMainDatabase(subDomain))
            {
                return new ServiceResult<TenantExistsResults>(TenantExistsResults.NameNotAvailable & TenantExistsResults.SubDomainNotAvailable);
            }

            // Get the tenant data.
            wiserDatabaseConnection.ClearParameters();
            wiserDatabaseConnection.AddParameter("name", name);
            wiserDatabaseConnection.AddParameter("subDomain", subDomain);

            var query = $@"SELECT name, subdomain
                        FROM {ApiTableNames.WiserTenants} 
                        WHERE subdomain = ?subDomain 
                        OR name = ?name";

            var dataTable = await wiserDatabaseConnection.GetAsync(query);
            var result = TenantExistsResults.Available;
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<TenantExistsResults>(result);
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                var currentName = dataRow.Field<string>("name");
                var currentSubDomain = dataRow.Field<string>("subdomain");
                if ((result & TenantExistsResults.NameNotAvailable) != TenantExistsResults.NameNotAvailable && name.Equals(currentName, StringComparison.OrdinalIgnoreCase))
                {
                    result |= TenantExistsResults.NameNotAvailable;
                }
                if ((result & TenantExistsResults.SubDomainNotAvailable) != TenantExistsResults.SubDomainNotAvailable && subDomain.Equals(currentSubDomain, StringComparison.OrdinalIgnoreCase))
                {
                    result |= TenantExistsResults.SubDomainNotAvailable;
                }
            }

            return new ServiceResult<TenantExistsResults>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TenantModel>> CreateTenantAsync(TenantModel tenant, bool isWebShop = false, bool isConfigurator = false, bool isMultiLanguage = false)
        {
            // Create a new connection to the newly created database.
            await using (var mysqlConnection = new MySqlConnection(GenerateConnectionStringFromTenant(tenant, false)))
            {
                MySqlTransaction transaction = null;
                try
                {
                    await wiserDatabaseConnection.BeginTransactionAsync();

                    if (!String.IsNullOrWhiteSpace(tenant.Database?.Password))
                    {
                        tenant.Database.Password = tenant.Database.Password.EncryptWithAesWithSalt(apiSettings.DatabasePasswordEncryptionKey);
                    }

                    if (String.IsNullOrWhiteSpace(tenant.EncryptionKey))
                    {
                        tenant.EncryptionKey = SecurityHelpers.GenerateRandomPassword(20);
                    }

                    await CreateOrUpdateTenantAsync(tenant);

                    wiserDatabaseConnection.ClearParameters();
                    wiserDatabaseConnection.AddParameter("id", tenant.Id);
                    await wiserDatabaseConnection.ExecuteAsync($"UPDATE {ApiTableNames.WiserTenants} SET tenantid = id WHERE id = ?id");

                    // Remove passwords from response
                    if (tenant.Database != null)
                    {
                        tenant.Database.Password = null;
                    }

                    var createTablesQuery = await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.CreateTables.sql");
                    var createTriggersQuery = await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.CreateTriggers.sql");
                    var createdStoredProceduresQuery = await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.StoredProcedures.sql");
                    var insertInitialDataQuery = await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.InsertInitialData.sql");
                    var insertInitialDataEcommerceQuery = !isWebShop ? "" : await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.InsertInitialDataEcommerce.sql");
                    var createTablesConfiguratorQuery = !isConfigurator ? "" :await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.CreateTablesConfigurator.sql");
                    var insertInitialDataConfiguratorQuery = !isConfigurator ? "" : await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.InsertInitialDataConfigurator.sql");
                    var insertInitialDataMultiLanguageQuery = !isMultiLanguage ? "" : await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.InsertInitialDataMultiLanguage.sql");

                    if (tenant.WiserSettings != null)
                    {
                        foreach (var (key, value) in tenant.WiserSettings)
                        {
                            createTablesQuery = createTablesQuery.ReplaceCaseInsensitive($"{{{key}}}", value);
                            createTriggersQuery = createTriggersQuery.ReplaceCaseInsensitive($"{{{key}}}", value);
                            createdStoredProceduresQuery = createdStoredProceduresQuery.ReplaceCaseInsensitive($"{{{key}}}", value);
                            insertInitialDataQuery = insertInitialDataQuery.ReplaceCaseInsensitive($"{{{key}}}", value);

                            if (isMultiLanguage)
                            {
                                insertInitialDataMultiLanguageQuery = insertInitialDataMultiLanguageQuery.ReplaceCaseInsensitive($"{{{key}}}", value);
                            }

                            if (isWebShop)
                            {
                                insertInitialDataEcommerceQuery = insertInitialDataEcommerceQuery.ReplaceCaseInsensitive($"{{{key}}}", value);
                            }

                            if (isConfigurator)
                            {
                                createTablesConfiguratorQuery = createTablesConfiguratorQuery.ReplaceCaseInsensitive($"{{{key}}}", value);
                                insertInitialDataConfiguratorQuery = insertInitialDataConfiguratorQuery.ReplaceCaseInsensitive($"{{{key}}}", value);
                            }
                        }
                    }

                    await mysqlConnection.OpenAsync();
                    transaction = await mysqlConnection.BeginTransactionAsync();
                    await using (var command = mysqlConnection.CreateCommand())
                    {
                        command.Parameters.AddWithValue("newTenantId", tenant.Id);
                        command.CommandText = createTablesQuery;
                        await command.ExecuteNonQueryAsync();
                        command.CommandText = createTriggersQuery;
                        await command.ExecuteNonQueryAsync();
                        command.CommandText = createdStoredProceduresQuery;
                        await command.ExecuteNonQueryAsync();
                        command.CommandText = insertInitialDataQuery;
                        await command.ExecuteNonQueryAsync();

                        if (isMultiLanguage)
                        {
                            command.CommandText = insertInitialDataMultiLanguageQuery;
                            await command.ExecuteNonQueryAsync();
                        }

                        if (isWebShop)
                        {
                            command.CommandText = insertInitialDataEcommerceQuery;
                            await command.ExecuteNonQueryAsync();
                        }

                        if (isConfigurator)
                        {
                            command.CommandText = createTablesConfiguratorQuery;
                            await command.ExecuteNonQueryAsync();
                            command.CommandText = insertInitialDataConfiguratorQuery;
                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    await transaction.CommitAsync();
                    await wiserDatabaseConnection.CommitTransactionAsync();

                    return new ServiceResult<TenantModel>(tenant);
                }
                catch
                {
                    await wiserDatabaseConnection.RollbackTransactionAsync();
                    await transaction.RollbackAsync();

                    throw;
                }
                finally
                {
                    if (transaction != null)
                    {
                        await transaction.DisposeAsync();
                    }
                }
            }
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> GetTitleAsync(string subDomain)
        {
            if (String.IsNullOrWhiteSpace(subDomain))
            {
                return new ServiceResult<string>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "No sub domain given"
                };
            }

            try
            {
                if (httpContextAccessor.HttpContext != null)
                {
                    // Set sub domain to main and then make sure the database connection log table in the main database is up-to-date.
                    httpContextAccessor.HttpContext.Items[HttpContextConstants.SubDomainKey] = apiSettings.MainSubDomain;
                    await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> { GeeksCoreLibrary.Modules.Databases.Models.Constants.DatabaseConnectionLogTableName });
                }

                wiserDatabaseConnection.ClearParameters();
                wiserDatabaseConnection.AddParameter("subDomain", subDomain);

                var dataTable = await wiserDatabaseConnection.GetAsync($"SELECT name, wiser_title FROM {ApiTableNames.WiserTenants} WHERE subdomain = ?subdomain");
                if (dataTable.Rows.Count == 0)
                {
                    return new ServiceResult<string>
                    {
                        StatusCode = HttpStatusCode.NotFound,
                        ErrorMessage = "No tenant found with this sub domain"
                    };
                }

                var result = dataTable.Rows[0].Field<string>("wiser_title");
                if (String.IsNullOrWhiteSpace(result))
                {
                    result = dataTable.Rows[0].Field<string>("name");
                }

                return new ServiceResult<string>(result);
            }
            catch (MySqlException mySqlException)
            {
                // If easy_customers does not exist, just return null.
                if (mySqlException.Number is (int)MySqlErrorCode.UnknownTable or (int)MySqlErrorCode.NoSuchTable)
                {
                    return new ServiceResult<string>(null);
                }

                throw;
            }
        }

        /// <inheritdoc />
        public bool IsMainDatabase(ClaimsIdentity identity)
        {
            return IsMainDatabase(IdentityHelpers.GetSubDomain(identity));
        }

        /// <inheritdoc />
        public bool IsMainDatabase(string subDomain)
        {
            return String.IsNullOrWhiteSpace(subDomain) || String.Equals(subDomain, apiSettings.MainSubDomain, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public async Task CreateOrUpdateTenantAsync(TenantModel tenant)
        {
            // Note: Passwords should be encrypted by Wiser before sending them to the API.
            wiserDatabaseConnection.ClearParameters();
            wiserDatabaseConnection.AddParameter("customerid", tenant.TenantId);
            wiserDatabaseConnection.AddParameter("name", tenant.Name);
            wiserDatabaseConnection.AddParameter("db_host", tenant.Database.Host);
            wiserDatabaseConnection.AddParameter("db_login", tenant.Database.Username);
            wiserDatabaseConnection.AddParameter("db_passencrypted", tenant.Database.Password ?? String.Empty);
            wiserDatabaseConnection.AddParameter("db_port", tenant.Database.PortNumber);
            wiserDatabaseConnection.AddParameter("db_dbname", tenant.Database.DatabaseName);
            wiserDatabaseConnection.AddParameter("encryption_key", tenant.EncryptionKey);
            wiserDatabaseConnection.AddParameter("encryption_key_test", tenant.EncryptionKey);
            wiserDatabaseConnection.AddParameter("subdomain", tenant.SubDomain);
            wiserDatabaseConnection.AddParameter("wiser_title", tenant.WiserTitle);

            tenant.Id = await wiserDatabaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(ApiTableNames.WiserTenants, tenant.Id);
        }

        /// <inheritdoc />
        public string GenerateConnectionStringFromTenant(TenantModel tenant, bool passwordIsEncrypted = true)
        {
            var decryptedPassword = passwordIsEncrypted ? tenant.Database.Password.DecryptWithAesWithSalt(apiSettings.DatabasePasswordEncryptionKey) : tenant.Database.Password;
            return $"server={tenant.Database.Host};port={(tenant.Database.PortNumber > 0 ? tenant.Database.PortNumber : 3306)};uid={tenant.Database.Username};pwd={decryptedPassword};database={tenant.Database.DatabaseName};AllowUserVariables=True;ConvertZeroDateTime=true;CharSet=utf8";
        }

        #region Private functions

        #endregion
    }
}