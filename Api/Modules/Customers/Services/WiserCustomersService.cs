using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Models;
using Api.Core.Services;
using Api.Modules.Customers.Enums;
using Api.Modules.Customers.Interfaces;
using Api.Modules.Customers.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Helpers;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using Serilog.Core;

namespace Api.Modules.Customers.Services
{
    /// <summary>
    /// Service for operations related to Wiser customers.
    /// </summary>
    public class WiserCustomersService : IWiserCustomersService, IScopedService
    {
        #region Private fields
        
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IDatabaseHelpersService databaseHelpersService;
        private readonly ILogger<WiserCustomersService> logger;
        private readonly GclSettings gclSettings;
        private readonly ApiSettings apiSettings;
        private readonly IDatabaseConnection wiserDatabaseConnection;
        
        private readonly List<string> entityTypesToSkipWhenSynchronisingEnvironments = new()
        {
            GeeksCoreLibrary.Components.ShoppingBasket.Models.Constants.BasketEntityType,
            GeeksCoreLibrary.Components.ShoppingBasket.Models.Constants.BasketLineEntityType,
            GeeksCoreLibrary.Components.Account.Models.Constants.DefaultEntityType,
            GeeksCoreLibrary.Components.Account.Models.Constants.DefaultSubAccountEntityType,
            GeeksCoreLibrary.Components.OrderProcess.Models.Constants.OrderEntityType,
            GeeksCoreLibrary.Components.OrderProcess.Models.Constants.OrderLineEntityType,
            "relatie",
            "klant"
        };

        #endregion

        /// <summary>
        /// Creates a new instance of WiserCustomersService.
        /// </summary>
        public WiserCustomersService(IDatabaseConnection connection, IOptions<ApiSettings> apiSettings, IOptions<GclSettings> gclSettings, IDatabaseHelpersService databaseHelpersService, ILogger<WiserCustomersService> logger)
        {
            clientDatabaseConnection = connection;
            this.databaseHelpersService = databaseHelpersService;
            this.logger = logger;
            this.gclSettings = gclSettings.Value;
            this.apiSettings = apiSettings.Value;

            if (clientDatabaseConnection is ClientDatabaseConnection databaseConnection)
            {
                wiserDatabaseConnection = databaseConnection.WiserDatabaseConnection;
            }
        }

        /// <inheritdoc />
        public async Task<ServiceResult<CustomerModel>> GetSingleAsync(ClaimsIdentity identity, bool includeDatabaseInformation = false)
        {
            var subDomain = IdentityHelpers.GetSubDomain(identity);
            if (IsMainDatabase(subDomain))
            {
                return new ServiceResult<CustomerModel>(new CustomerModel
                {
                    CustomerId = 1,
                    Id = 1,
                    Name = "Main",
                    SubDomain = apiSettings.MainSubDomain,
                    EncryptionKey = String.IsNullOrWhiteSpace(gclSettings.ExpiringEncryptionKey) ? gclSettings.DefaultEncryptionKey : gclSettings.ExpiringEncryptionKey
                });
            }

            // Get the customer data.
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
                        FROM {ApiTableNames.WiserCustomers} 
                        WHERE subdomain = ?name";

            var customersDataTable = await wiserDatabaseConnection.GetAsync(query);
            if (customersDataTable.Rows.Count == 0)
            {
                return new ServiceResult<CustomerModel>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = $"Customer with sub domain '{subDomain}' not found.",
                    ReasonPhrase = "Customer not found"
                };
            }

            var result = CustomerModel.FromDataRow(customersDataTable.Rows[0]);
            return new ServiceResult<CustomerModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<CustomerModel>> GetSingleAsync(int id, bool includeDatabaseInformation = false)
        {
            // Get the customer data.
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
                        FROM {ApiTableNames.WiserCustomers} 
                        WHERE id = ?id";

            var customersDataTable = await wiserDatabaseConnection.GetAsync(query);
            if (customersDataTable.Rows.Count == 0)
            {
                return new ServiceResult<CustomerModel>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = $"Customer with ID '{id}' not found.",
                    ReasonPhrase = "Customer not found"
                };
            }

            var result = CustomerModel.FromDataRow(customersDataTable.Rows[0]);
            return new ServiceResult<CustomerModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> GetEncryptionKey(ClaimsIdentity identity)
        {
            var subDomain = IdentityHelpers.GetSubDomain(identity);
            if (IsMainDatabase(subDomain))
            {
                return new ServiceResult<string>(String.IsNullOrWhiteSpace(gclSettings.ExpiringEncryptionKey) ? gclSettings.DefaultEncryptionKey : gclSettings.ExpiringEncryptionKey);
            }
            
            // Get the customer data.
            wiserDatabaseConnection.ClearParameters();
            wiserDatabaseConnection.AddParameter("name", subDomain);

            var query = $@"SELECT {(IdentityHelpers.IsTestEnvironment(identity) ? "encryption_key_test" : "encryption_key")} AS encryption_key
                        FROM {ApiTableNames.WiserCustomers} 
                        WHERE subdomain = ?name";

            var customersDataTable = await wiserDatabaseConnection.GetAsync(query);
            if (customersDataTable.Rows.Count == 0)
            {
                return new ServiceResult<string>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = $"Customer with sub domain '{subDomain}' not found.",
                    ReasonPhrase = "Customer not found"
                };
            }
            
            return new ServiceResult<string>(customersDataTable.Rows[0].Field<string>("encryption_key"));
        }

        #region Wiser users data/settings
        
        /// <inheritdoc />
        public async Task<T> DecryptValue<T>(string encryptedValue, ClaimsIdentity identity)
        {
            var customer = await GetSingleAsync(identity);

            return DecryptValue<T>(encryptedValue, customer.ModelObject);
        }

        /// <inheritdoc />
        public T DecryptValue<T>(string encryptedValue, CustomerModel customer)
        {
            return String.IsNullOrWhiteSpace(encryptedValue) ? default : (T)Convert.ChangeType(encryptedValue.Replace(" ", "+").DecryptWithAesWithSalt(customer.EncryptionKey, true), typeof(T));
        }

        /// <inheritdoc />
        public async Task<string> EncryptValue(object valueToEncrypt, ClaimsIdentity identity)
        {
            var customer = await GetSingleAsync(identity);

            return EncryptValue(valueToEncrypt, customer.ModelObject);
        }

        /// <inheritdoc />
        public string EncryptValue(object valueToEncrypt, CustomerModel customer)
        {
            return valueToEncrypt?.ToString().EncryptWithAesWithSalt(customer.EncryptionKey, withDateTime: true);
        }
        
        /// <inheritdoc />
        public async Task<ServiceResult<CustomerExistsResults>> CustomerExistsAsync(string name, string subDomain)
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
                return new ServiceResult<CustomerExistsResults>(CustomerExistsResults.NameNotAvailable & CustomerExistsResults.SubDomainNotAvailable);
            }
            
            // Get the customer data.
            wiserDatabaseConnection.ClearParameters();
            wiserDatabaseConnection.AddParameter("name", name);
            wiserDatabaseConnection.AddParameter("subDomain", subDomain);

            var query = $@"SELECT name, subdomain
                        FROM {ApiTableNames.WiserCustomers} 
                        WHERE subdomain = ?subDomain 
                        OR name = ?name";

            var dataTable = await wiserDatabaseConnection.GetAsync(query);
            var result = CustomerExistsResults.Available;
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<CustomerExistsResults>(result);
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                var currentName = dataRow.Field<string>("name");
                var currentSubDomain = dataRow.Field<string>("subdomain");
                if ((result & CustomerExistsResults.NameNotAvailable) != CustomerExistsResults.NameNotAvailable && name.Equals(currentName, StringComparison.OrdinalIgnoreCase))
                {
                    result |= CustomerExistsResults.NameNotAvailable;
                }
                if ((result & CustomerExistsResults.SubDomainNotAvailable) != CustomerExistsResults.SubDomainNotAvailable && subDomain.Equals(currentSubDomain, StringComparison.OrdinalIgnoreCase))
                {
                    result |= CustomerExistsResults.SubDomainNotAvailable;
                }
            }

            return new ServiceResult<CustomerExistsResults>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<CustomerModel>> CreateCustomerAsync(CustomerModel customer, bool isWebShop = false, bool isConfigurator = false)
        {
            // Create a new connection to the newly created database.
            await using (var mysqlConnection = new MySqlConnection(GenerateConnectionStringFromCustomer(customer, false)))
            {
                MySqlTransaction transaction = null;
                try
                {
                    await wiserDatabaseConnection.BeginTransactionAsync();
                    
                    if (!String.IsNullOrWhiteSpace(customer.Database?.Password))
                    {
                        customer.Database.Password = customer.Database.Password.EncryptWithAesWithSalt(apiSettings.DatabasePasswordEncryptionKey);
                    }
                    
                    if (String.IsNullOrWhiteSpace(customer.EncryptionKey))
                    {
                        customer.EncryptionKey = SecurityHelpers.GenerateRandomPassword(20);
                    }

                    await CreateOrUpdateCustomerAsync(customer);

                    wiserDatabaseConnection.ClearParameters();
                    wiserDatabaseConnection.AddParameter("id", customer.Id);
                    await wiserDatabaseConnection.ExecuteAsync($"UPDATE {ApiTableNames.WiserCustomers} SET customerid = id WHERE id = ?id");

                    // Remove passwords from response
                    if (customer.Database != null)
                    {
                        customer.Database.Password = null;
                    }
                    
                    var createTablesQuery = await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.CreateTables.sql");
                    var createTriggersQuery = await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.CreateTriggers.sql");
                    var createdStoredProceduresQuery = await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.StoredProcedures.sql");
                    var insertInitialDataQuery = await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.InsertInitialData.sql");
                    var insertInitialDataEcommerceQuery = !isWebShop ? "" : await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.InsertInitialDataEcommerce.sql");
                    var createTablesConfiguratorQuery = !isConfigurator ? "" :await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.CreateTablesConfigurator.sql");
                    var insertInitialDataConfiguratorQuery = !isConfigurator ? "" : await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.InsertInitialDataConfigurator.sql");

                    if (customer.WiserSettings != null)
                    {
                        foreach (var (key, value) in customer.WiserSettings)
                        {
                            createTablesQuery = createTablesQuery.ReplaceCaseInsensitive($"{{{key}}}", value);
                            createTriggersQuery = createTriggersQuery.ReplaceCaseInsensitive($"{{{key}}}", value);
                            createdStoredProceduresQuery = createdStoredProceduresQuery.ReplaceCaseInsensitive($"{{{key}}}", value);
                            insertInitialDataQuery = insertInitialDataQuery.ReplaceCaseInsensitive($"{{{key}}}", value);
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
                        command.Parameters.AddWithValue("newCustomerId", customer.Id);
                        command.CommandText = createTablesQuery;
                        await command.ExecuteNonQueryAsync();
                        command.CommandText = createTriggersQuery;
                        await command.ExecuteNonQueryAsync();
                        command.CommandText = createdStoredProceduresQuery;
                        await command.ExecuteNonQueryAsync();
                        command.CommandText = insertInitialDataQuery;
                        await command.ExecuteNonQueryAsync();
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

                    return new ServiceResult<CustomerModel>(customer);
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
                    ErrorMessage = "No sub domain given",
                    ReasonPhrase = "No sub domain given"
                };
            }

            try
            {
                wiserDatabaseConnection.ClearParameters();
                wiserDatabaseConnection.AddParameter("subDomain", subDomain);

                var dataTable = await wiserDatabaseConnection.GetAsync($"SELECT name, wiser_title FROM {ApiTableNames.WiserCustomers} WHERE subdomain = ?subdomain");
                if (dataTable.Rows.Count == 0)
                {
                    return new ServiceResult<string>
                    {
                        StatusCode = HttpStatusCode.NotFound,
                        ErrorMessage = "No customer found with this sub domain",
                        ReasonPhrase = "No customer found with this sub domain"
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
        public async Task<ServiceResult<CustomerModel>> CreateNewEnvironmentAsync(ClaimsIdentity identity, string name)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                return new ServiceResult<CustomerModel>
                {
                    ErrorMessage = "Name is empty",
                    ReasonPhrase = "Name is empty",
                    StatusCode = HttpStatusCode.BadRequest
                };
            }

            var currentCustomer = (await GetSingleAsync(identity, true)).ModelObject;
            var subDomain = currentCustomer.SubDomain;
            var newCustomerName = $"{currentCustomer.Name} - {name}";
            var newCustomerTitle = $"{currentCustomer.WiserTitle} - {name}";

            // If the ID is not the same as the customer ID, it means this is not the main/production environment of this customer.
            // Then we want to get the sub domain of the main/production environment of the customer, to use as base for the new sub domain for the new environment.
            if (currentCustomer.Id != currentCustomer.CustomerId)
            {
                wiserDatabaseConnection.AddParameter("customerId", currentCustomer.CustomerId);
                var dataTable = await wiserDatabaseConnection.GetAsync($"SELECT subdomain, name, wiser_title FROM {ApiTableNames.WiserCustomers} WHERE id = ?customerId");
                if (dataTable.Rows.Count == 0)
                {
                    // This should never happen, hence the exception.
                    throw new Exception("Customer not found");
                }

                subDomain = dataTable.Rows[0].Field<string>("subdomain");
                newCustomerName = $"{dataTable.Rows[0].Field<string>("name")} - {name}";
                newCustomerTitle = $"{dataTable.Rows[0].Field<string>("wiser_title")} - {name}";
            }

            // Create a valid database and sub domain name for the new environment.
            var databaseNameBuilder = new StringBuilder(name.Trim().ToLowerInvariant());
            databaseNameBuilder = Path.GetInvalidFileNameChars().Aggregate(databaseNameBuilder, (current, invalidChar) => current.Replace(invalidChar.ToString(), ""));
            databaseNameBuilder = databaseNameBuilder.Replace(@"\", "_").Replace(@"/", "_").Replace(".", "_").Replace(" ", "_");

            var databaseName = $"{currentCustomer.Database.DatabaseName}_{databaseNameBuilder}".ToMySqlSafeValue(false);
            if (databaseName.Length > 64)
            {
                databaseName = databaseName[..64];
            }

            subDomain += $"_{databaseNameBuilder}";

            // Make sure no customer exists yet with this name and/or sub domain.
            var customerExists = await CustomerExistsAsync(newCustomerName, subDomain);
            if (customerExists.StatusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<CustomerModel>
                {
                    ErrorMessage = customerExists.ErrorMessage,
                    ReasonPhrase = customerExists.ReasonPhrase,
                    StatusCode = customerExists.StatusCode
                };
            }

            if (customerExists.ModelObject != CustomerExistsResults.Available)
            {
                return new ServiceResult<CustomerModel>
                {
                    StatusCode = HttpStatusCode.Conflict,
                    ErrorMessage = $"Een omgeving met de naam '{name}' bestaat al.",
                    ReasonPhrase = $"Een omgeving met de naam '{name}' bestaat al."
                };
            }

            // Make sure the database doesn't exist yet. This method is only meant for creating new environments.
            if (await databaseHelpersService.DatabaseExistsAsync(databaseName))
            {
                return new ServiceResult<CustomerModel>
                {
                    StatusCode = HttpStatusCode.Conflict,
                    ErrorMessage = $"We hebben geprobeerd een database aan te maken met de naam '{databaseName}', echter bestaat deze al. Kies a.u.b. een andere omgevingsnaam, of neem contact op met ons.",
                    ReasonPhrase = $"We hebben geprobeerd een database aan te maken met de naam '{databaseName}', echter bestaat deze al. Kies a.u.b. een andere omgevingsnaam, of neem contact op met ons."
                };
            }

            // Add the new customer environment to easy_customers.
            var newCustomer = new CustomerModel
            {
                CustomerId = currentCustomer.CustomerId,
                Name = newCustomerName,
                EncryptionKey = SecurityHelpers.GenerateRandomPassword(20),
                SubDomain = subDomain,
                WiserTitle = newCustomerTitle,
                Database = new ConnectionInformationModel
                {
                    Host = currentCustomer.Database.Host,
                    Password = currentCustomer.Database.Password,
                    Username = currentCustomer.Database.Username,
                    DatabaseName = databaseName,
                    PortNumber = currentCustomer.Database.PortNumber
                }
            };

            try
            {
                await wiserDatabaseConnection.BeginTransactionAsync();
                await clientDatabaseConnection.BeginTransactionAsync();

                await CreateOrUpdateCustomerAsync(newCustomer);

                // Create the database in the same server/cluster.
                await databaseHelpersService.CreateDatabaseAsync(databaseName);

                // Create tables in new database.
                var query = @"SELECT TABLE_NAME 
                            FROM INFORMATION_SCHEMA.TABLES
                            WHERE TABLE_SCHEMA = ?currentSchema
                            AND TABLE_TYPE = 'BASE TABLE'
                            AND TABLE_NAME NOT LIKE '\_%'";

                clientDatabaseConnection.AddParameter("currentSchema", currentCustomer.Database.DatabaseName);
                clientDatabaseConnection.AddParameter("newSchema", newCustomer.Database.DatabaseName);
                var dataTable = await clientDatabaseConnection.GetAsync(query);
                var tablesToAlwaysLeaveEmpty = new List<string>
                {
                    WiserTableNames.WiserHistory, 
                    WiserTableNames.WiserImport, 
                    WiserTableNames.WiserImportLog, 
                    WiserTableNames.WiserUsersAuthenticationTokens, 
                    WiserTableNames.WiserCommunicationGenerated, 
                    WiserTableNames.AisLogs, 
                    "ais_serilog", 
                    "jcl_email"
                };
                
                // Create the tables in a new connection, because these cause implicit commits.
                await using (var mysqlConnection = new MySqlConnection(GenerateConnectionStringFromCustomer(newCustomer)))
                {
                    await mysqlConnection.OpenAsync();
                    await using (var command = mysqlConnection.CreateCommand())
                    {
                        foreach (DataRow dataRow in dataTable.Rows)
                        {
                            var tableName = dataRow.Field<string>("TABLE_NAME");

                            command.CommandText = $"CREATE TABLE `{newCustomer.Database.DatabaseName.ToMySqlSafeValue(false)}`.`{tableName.ToMySqlSafeValue(false)}` LIKE `{currentCustomer.Database.DatabaseName.ToMySqlSafeValue(false)}`.`{tableName.ToMySqlSafeValue(false)}`";
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }

                // Fill the tables with data.
                var entityTypesString = String.Join(",", entityTypesToSkipWhenSynchronisingEnvironments.Select(x => $"'{x}'"));
                foreach (DataRow dataRow in dataTable.Rows)
                {
                    var tableName = dataRow.Field<string>("TABLE_NAME");

                    // For Wiser tables, we don't want to copy customer data, so copy everything except data of certain entity types.
                    if (tableName!.EndsWith(WiserTableNames.WiserItem, StringComparison.OrdinalIgnoreCase))
                    {
                        await clientDatabaseConnection.ExecuteAsync($@"INSERT INTO `{newCustomer.Database.DatabaseName}`.`{tableName}` 
                                                                    SELECT * FROM `{currentCustomer.Database.DatabaseName}`.`{tableName}` 
                                                                    WHERE entity_type NOT IN ('{String.Join("','", entityTypesToSkipWhenSynchronisingEnvironments)}')");
                        continue;
                    }

                    if (tableName!.EndsWith(WiserTableNames.WiserItemDetail, StringComparison.OrdinalIgnoreCase))
                    {
                        var prefix = tableName.Replace(WiserTableNames.WiserItemDetail, "");
                        await clientDatabaseConnection.ExecuteAsync($@"INSERT INTO `{newCustomer.Database.DatabaseName}`.`{tableName}` 
                                                                    SELECT detail.* FROM `{currentCustomer.Database.DatabaseName}`.`{tableName}` AS detail
                                                                    JOIN `{currentCustomer.Database.DatabaseName}`.`{prefix}{WiserTableNames.WiserItem}` AS item ON item.id = detail.item_id AND item.entity_type NOT IN ({entityTypesString})");
                        continue;
                    }

                    if (tableName!.EndsWith(WiserTableNames.WiserItemFile, StringComparison.OrdinalIgnoreCase))
                    {
                        var prefix = tableName.Replace(WiserTableNames.WiserItemFile, "");
                        await clientDatabaseConnection.ExecuteAsync($@"INSERT INTO `{newCustomer.Database.DatabaseName}`.`{tableName}` 
                                                                    SELECT file.* FROM `{currentCustomer.Database.DatabaseName}`.`{tableName}` AS file
                                                                    JOIN `{currentCustomer.Database.DatabaseName}`.`{prefix}{WiserTableNames.WiserItem}` AS item ON item.id = file.item_id AND item.entity_type NOT IN ({entityTypesString})");
                        continue;
                    }

                    // Don't copy data from certain tables, such as log and archive tables.
                    if (tablesToAlwaysLeaveEmpty.Any(t => String.Equals(t, tableName, StringComparison.OrdinalIgnoreCase))
                        || tableName!.StartsWith("log_", StringComparison.OrdinalIgnoreCase)
                        || tableName.EndsWith("_log", StringComparison.OrdinalIgnoreCase)
                        || tableName.EndsWith(WiserTableNames.ArchiveSuffix))
                    {
                        continue;
                    }

                    await clientDatabaseConnection.ExecuteAsync($"INSERT INTO `{newCustomer.Database.DatabaseName}`.`{tableName}` SELECT * FROM `{currentCustomer.Database.DatabaseName}`.`{tableName}`");
                }
                
                // Add triggers (and stored procedures) to database, after inserting all data, so that the wiser_history table will still be empty.
                // We use wiser_history to later synchronise all changes to production, so it needs to be empty before the user starts to make changes in the new environment.
                query = $@"SELECT 
                            TRIGGER_NAME,
                            EVENT_MANIPULATION,
                            EVENT_OBJECT_TABLE,
	                        ACTION_STATEMENT,
	                        ACTION_ORIENTATION,
	                        ACTION_TIMING
                        FROM information_schema.TRIGGERS
                        WHERE TRIGGER_SCHEMA = ?currentSchema
                        AND EVENT_OBJECT_TABLE NOT LIKE '\_%'";
                dataTable = await clientDatabaseConnection.GetAsync(query);
                
                var createdStoredProceduresQuery = await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.StoredProcedures.sql");
                await using (var mysqlConnection = new MySqlConnection(GenerateConnectionStringFromCustomer(newCustomer)))
                {
                    await mysqlConnection.OpenAsync();
                    await using (var command = mysqlConnection.CreateCommand())
                    {
                        foreach (DataRow dataRow in dataTable.Rows)
                        {
                            query = $@"CREATE TRIGGER `{dataRow.Field<string>("TRIGGER_NAME")}` {dataRow.Field<string>("ACTION_TIMING")} {dataRow.Field<string>("EVENT_MANIPULATION")} ON `{newCustomer.Database.DatabaseName.ToMySqlSafeValue(false)}`.`{dataRow.Field<string>("EVENT_OBJECT_TABLE")}` FOR EACH {dataRow.Field<string>("ACTION_ORIENTATION")} {dataRow.Field<string>("ACTION_STATEMENT")}";
                            
                            command.CommandText = query;
                            await command.ExecuteNonQueryAsync();
                        }

                        command.CommandText = createdStoredProceduresQuery;
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Remove passwords from response.
                newCustomer.Database.Password = null;

                await clientDatabaseConnection.CommitTransactionAsync();
                await wiserDatabaseConnection.CommitTransactionAsync();

                return new ServiceResult<CustomerModel>(newCustomer);
            }
            catch
            {
                await wiserDatabaseConnection.RollbackTransactionAsync();
                await clientDatabaseConnection.RollbackTransactionAsync();

                // Drop the new database it something went wrong, so that we can start over again later.
                // We can safely do this, because this method will return an error if the database already exists,
                // so we can be sure that this database was created here and we can drop it again it something went wrong.
                if (await databaseHelpersService.DatabaseExistsAsync(databaseName))
                {
                    await databaseHelpersService.DropDatabaseAsync(databaseName);
                }

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<CustomerModel>>> GetEnvironmentsAsync(ClaimsIdentity identity)
        {
            var currentCustomer = (await GetSingleAsync(identity, true)).ModelObject;

            var query = $@"SELECT id, name
                        FROM {ApiTableNames.WiserCustomers}
                        WHERE customerid = ?id
                        AND id <> ?id";
            
            wiserDatabaseConnection.AddParameter("id", currentCustomer.CustomerId);
            var dataTable = await wiserDatabaseConnection.GetAsync(query);
            var results = new List<CustomerModel>();
            foreach (DataRow dataRow in dataTable.Rows)
            {
                results.Add(new CustomerModel
                {
                    Id = dataRow.Field<int>("id"),
                    CustomerId = currentCustomer.CustomerId,
                    Name = dataRow.Field<string>("name")
                });
            }

            return new ServiceResult<List<CustomerModel>>(results);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<SynchroniseChangesToProductionResultModel>> SynchroniseChangesToProductionAsync(ClaimsIdentity identity, int id)
        {
            // Get the data for the different environments.
            var currentCustomer = (await GetSingleAsync(identity, true)).ModelObject;
            var selectedEnvironmentCustomer = (await GetSingleAsync(id, true)).ModelObject;
            var productionCustomer = (await GetSingleAsync(currentCustomer.CustomerId, true)).ModelObject;

            // Check to make sure someone is not trying to copy changes from an environment that does not belong to them.
            if (selectedEnvironmentCustomer == null || currentCustomer.CustomerId != selectedEnvironmentCustomer.CustomerId)
            {
                return new ServiceResult<SynchroniseChangesToProductionResultModel>
                {
                    StatusCode = HttpStatusCode.Forbidden
                };
            }

            var result = new SynchroniseChangesToProductionResultModel();
            try
            {
                // Start a transaction so that we can roll back any changes we made if an error occurs.
                //await clientDatabaseConnection.BeginTransactionAsync();

                // Create the wiser_id_mappings table, in the selected environment, if it doesn't exist yet.
                // We need it to map IDs of the selected environment to IDs of the production environment, because they are not always the same.
                await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> {WiserTableNames.WiserIdMappings}, selectedEnvironmentCustomer.Database.DatabaseName);
                
                // Get all history since last synchronisation.
                var dataTable = await clientDatabaseConnection.GetAsync($"SELECT * FROM `{selectedEnvironmentCustomer.Database.DatabaseName}`.{WiserTableNames.WiserHistory} ORDER BY id ASC");
                var queryPrefix = @"SET @saveHistory = TRUE; SET @_username = ?username; ";
                var username = $"{IdentityHelpers.GetUserName(identity, true)} (Sync from {selectedEnvironmentCustomer.Name})";
                if (username.Length > 50)
                {
                    username = $"{IdentityHelpers.GetUserName(identity)} (Sync from {selectedEnvironmentCustomer.Name})";

                    if (username.Length > 50)
                    {
                        username = IdentityHelpers.GetUserName(identity);
                    }
                }
                clientDatabaseConnection.AddParameter("username", username);

                // This is to cache the entity types for all changed items, so that we don't have to execute a query for every changed detail of the same item.
                var entityTypes = new Dictionary<ulong, string>();

                // This is to map one item ID to another. This is needed because when someone creates a new item in the other environment, that ID could already exist in the production environment.
                // So we need to map the ID that is saved in wiser_history to the new ID of the item that we create in the production environment.
                var idMapping = new Dictionary<string, Dictionary<ulong, ulong>>();
                var idMappingQuery = $@"SELECT table_name, our_id, production_id FROM `{selectedEnvironmentCustomer.Database.DatabaseName}`.`{WiserTableNames.WiserIdMappings}`";
                var idMappingDatatable = await clientDatabaseConnection.GetAsync(idMappingQuery);
                foreach (DataRow dataRow in idMappingDatatable.Rows)
                {
                    var tableName = dataRow.Field<string>("table_name");
                    var ourId = dataRow.Field<ulong>("our_id");
                    var productionId = dataRow.Field<ulong>("production_id");

                    if (!idMapping.ContainsKey(tableName!))
                    {
                        idMapping.Add(tableName, new Dictionary<ulong, ulong>());
                    }

                    idMapping[tableName][ourId] = productionId;
                }

                // Start synchronising all history items one by one.
                var historyItemsSynchronised = new List<ulong>();
                foreach (DataRow dataRow in dataTable.Rows)
                {
                    var historyId = Convert.ToUInt64(dataRow["id"]);
                    var action = dataRow.Field<string>("action").ToUpperInvariant();
                    var tableName = dataRow.Field<string>("tablename") ?? "";
                    var originalItemId = Convert.ToUInt64(dataRow["item_id"]);
                    var itemId = originalItemId;
                    var field = dataRow.Field<string>("field");
                    var oldValue = dataRow.Field<string>("oldvalue");
                    var newValue = dataRow.Field<string>("newvalue");
                    var languageCode = dataRow.Field<string>("language_code") ?? "";
                    var groupName = dataRow.Field<string>("groupname") ?? "";

                    try
                    {
                        // Variables for item link changes.
                        var destinationItemId = 0UL;
                        ulong? oldItemId = null;
                        ulong? oldDestinationItemId = null;

                        // Make sure we have the correct item ID. For some actions, the item id is saved in a different column.
                        switch (action)
                        {
                            case "REMOVE_LINK":
                                destinationItemId = itemId;
                                itemId = Convert.ToUInt64(oldValue);
                                break;
                            case "CHANGE_LINK":
                            {
                                // When a link has been changed, it's possible that the ID of one of the items is changed.
                                // It's also possible that this is a new link that the production database didn't have yet (and so the ID of the link will most likely be different).
                                // Therefor we need to find the original item and destination IDs, so that we can use those to update the link in the production database.
                                clientDatabaseConnection.AddParameter("linkId", itemId);
                                var query = $@"SELECT item_id, destination_item_id FROM `{selectedEnvironmentCustomer.Database.DatabaseName}`.`{WiserTableNames.WiserItemLink}` WHERE id = ?linkId";
                                var linkDataTable = await clientDatabaseConnection.GetAsync(query);
                                if (linkDataTable.Rows.Count == 0)
                                {
                                    query = $@"SELECT item_id, destination_item_id FROM `{selectedEnvironmentCustomer.Database.DatabaseName}`.`{WiserTableNames.WiserItemLink}`{WiserTableNames.ArchiveSuffix} WHERE id = ?linkId";
                                    linkDataTable = await clientDatabaseConnection.GetAsync(query);
                                    if (linkDataTable.Rows.Count == 0)
                                    {
                                        // This should never happen, but just in case the ID somehow doesn't exist anymore, log a warning and continue on to the next item.
                                        logger.LogWarning($"Could not find link with id '{itemId}' in database '{selectedEnvironmentCustomer.Database.DatabaseName}'. Skipping this history record in synchronisation to production.");
                                        continue;
                                    }
                                }

                                itemId = Convert.ToUInt64(linkDataTable.Rows[0]["item_id"]);
                                destinationItemId = Convert.ToUInt64(linkDataTable.Rows[0]["destination_item_id"]);

                                switch (field)
                                {
                                    case "destination_item_id":
                                        oldDestinationItemId = Convert.ToUInt64(oldValue);
                                        destinationItemId = Convert.ToUInt64(newValue);
                                        oldItemId = itemId;
                                        break;
                                    case "item_id":
                                        oldItemId = Convert.ToUInt64(oldValue);
                                        itemId = Convert.ToUInt64(newValue);
                                        oldDestinationItemId = destinationItemId;
                                        break;
                                }

                                break;
                            }
                            case "ADD_LINK":
                                destinationItemId = itemId;
                                itemId = Convert.ToUInt64(newValue);

                                break;
                        }

                        // Did we map the item ID to something else? Then use that new ID.
                        var originalDestinationItemId = destinationItemId;
                        itemId = GetMappedId(tableName, idMapping, itemId).Value;
                        destinationItemId = GetMappedId(tableName, idMapping, destinationItemId).Value;
                        oldItemId = GetMappedId(tableName, idMapping, oldItemId);
                        oldDestinationItemId = GetMappedId(tableName, idMapping, oldDestinationItemId);

                        var isWiserItemChange = true;

                        // Figure out the entity type of the item that was updated, so that we can check if we need to do anything with it.
                        // We don't want to synchronise certain entity types, such as users, relations and baskets.
                        var entityType = "";
                        if (entityTypes.ContainsKey(itemId))
                        {
                            entityType = entityTypes[itemId];
                        }
                        else
                        {
                            // Check if this item is saved in a dedicated table with a certain prefix.
                            var tablePrefix = "";
                            if (tableName.EndsWith(WiserTableNames.WiserItem, StringComparison.OrdinalIgnoreCase))
                            {
                                tablePrefix = tableName.ReplaceCaseInsensitive(WiserTableNames.WiserItem, "");
                            }
                            else if (tableName.EndsWith(WiserTableNames.WiserItemDetail, StringComparison.OrdinalIgnoreCase))
                            {
                                tablePrefix = tableName.ReplaceCaseInsensitive(WiserTableNames.WiserItemDetail, "");
                            }
                            else if (tableName.EndsWith(WiserTableNames.WiserItemFile, StringComparison.OrdinalIgnoreCase))
                            {
                                tablePrefix = tableName.ReplaceCaseInsensitive(WiserTableNames.WiserItemFile, "");
                            }
                            else if (tableName.EndsWith(WiserTableNames.WiserItemLink, StringComparison.OrdinalIgnoreCase))
                            {
                                tablePrefix = tableName.ReplaceCaseInsensitive(WiserTableNames.WiserItemLink, "");
                            }
                            else if (tableName.EndsWith(WiserTableNames.WiserItemLinkDetail, StringComparison.OrdinalIgnoreCase))
                            {
                                tablePrefix = tableName.ReplaceCaseInsensitive(WiserTableNames.WiserItemLinkDetail, "");
                            }
                            else if (String.Equals(tableName, WiserTableNames.WiserPermission, StringComparison.OrdinalIgnoreCase)
                                     || String.Equals(tableName, WiserTableNames.WiserUserRoles, StringComparison.OrdinalIgnoreCase))
                            {
                                isWiserItemChange = false;
                            }
                            else
                            {
                                // Skip any other tables, we don't want to synchronise wiser_entitypropert, wiser_query etc.
                                continue;
                            }

                            if (isWiserItemChange)
                            {
                                clientDatabaseConnection.AddParameter("itemId", originalItemId);
                                var getEntityTypeQuery = $"SELECT entity_type FROM `{selectedEnvironmentCustomer.Database.DatabaseName}`.`{tablePrefix}{WiserTableNames.WiserItem}` WHERE id = ?itemId";
                                var itemDataTable = await clientDatabaseConnection.GetAsync(getEntityTypeQuery);
                                if (itemDataTable.Rows.Count == 0)
                                {
                                    logger.LogWarning($"Could not find item with ID '{itemId}', so skipping it...");
                                    result.Errors.Add($"Item met ID '{itemId}' kon niet gevonden worden.");
                                    continue;
                                }

                                entityType = itemDataTable.Rows[0].Field<string>("entity_type");
                                entityTypes.Add(itemId, entityType);
                            }
                        }

                        // We don't want to synchronise certain entity types, such as users, relations and baskets.
                        if (isWiserItemChange && entityTypesToSkipWhenSynchronisingEnvironments.Any(x => String.Equals(x, entityType, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }

                        clientDatabaseConnection.AddParameter("entityType", entityType);

                        // Update the item in the production environment.
                        switch (action)
                        {
                            case "CREATE_ITEM":
                            {
                                // Item doesn't exist yet, create it (wiser_history does not show the creation of new items).
                                var query = $@"{queryPrefix}
                                            INSERT INTO `{productionCustomer.Database.DatabaseName}`.`{tableName}` (entity_type) VALUES ('')";
                                var newItemId = Convert.ToUInt64(await clientDatabaseConnection.InsertRecordAsync(query));

                                // Map the item ID from wiser_history to the ID of the newly created item, locally and in database.
                                await AddIdMapping(idMapping, tableName, originalItemId, newItemId, selectedEnvironmentCustomer);

                                break;
                            }
                            case "UPDATE_ITEM" when tableName.EndsWith(WiserTableNames.WiserItemDetail, StringComparison.OrdinalIgnoreCase):
                            {
                                clientDatabaseConnection.AddParameter("key", field);
                                clientDatabaseConnection.AddParameter("languageCode", languageCode);
                                clientDatabaseConnection.AddParameter("groupName", groupName);

                                var query = queryPrefix;
                                if (String.IsNullOrWhiteSpace(newValue))
                                {
                                    query += $@"DELETE FROM `{productionCustomer.Database.DatabaseName}`.`{tableName}`
                                                WHERE item_id = ?itemId
                                                AND `key` = ?key
                                                AND language_code = ?languageCode
                                                AND groupname = ?groupName";
                                }
                                else
                                {
                                    var useLongValue = newValue.Length > 1000;
                                    clientDatabaseConnection.AddParameter("value", useLongValue ? "" : newValue);
                                    clientDatabaseConnection.AddParameter("longValue", useLongValue ? newValue : "");

                                    query += $@"INSERT INTO `{productionCustomer.Database.DatabaseName}`.`{tableName}` (language_code, item_id, groupname, `key`, value, long_value)
                                                VALUES (?languageCode, ?itemId, ?groupName, ?key, ?value, ?longValue)
                                                ON DUPLICATE KEY UPDATE groupname = VALUES(groupname), value = VALUES(value), long_value = VALUES(long_value)";
                                }

                                await clientDatabaseConnection.ExecuteAsync(query);

                                break;
                            }
                            case "UPDATE_ITEM" when tableName.EndsWith(WiserTableNames.WiserItem, StringComparison.OrdinalIgnoreCase):
                            {
                                clientDatabaseConnection.AddParameter("newValue", newValue);
                                var query = $@"{queryPrefix}
                                            UPDATE `{productionCustomer.Database.DatabaseName}`.`{tableName}` 
                                            SET `{field.ToMySqlSafeValue(false)}` = ?newValue
                                            WHERE id = ?itemId";
                                await clientDatabaseConnection.ExecuteAsync(query);

                                break;
                            }
                            case "DELETE_ITEM":
                            {
                                var query = $@"{queryPrefix} CALL DeleteWiser2Item(?itemId, ?entityType);";
                                await clientDatabaseConnection.ExecuteAsync(query);

                                break;
                            }
                            case "ADD_LINK":
                            {
                                var split = field.Split(',');
                                var type = split[0];
                                var ordering = split.Length > 1 ? split[1] : "0";
                                clientDatabaseConnection.AddParameter("itemId", itemId);
                                clientDatabaseConnection.AddParameter("originalItemId", originalItemId);
                                clientDatabaseConnection.AddParameter("destinationItemId", destinationItemId);
                                clientDatabaseConnection.AddParameter("originalDestinationItemId", originalDestinationItemId);
                                clientDatabaseConnection.AddParameter("type", type);
                                clientDatabaseConnection.AddParameter("ordering", ordering);

                                // Get the original link ID, so we can map it to the new one.
                                var query = $@"SELECT id FROM `{selectedEnvironmentCustomer.Database.DatabaseName}`.`{tableName}` WHERE item_id = ?originalItemId AND destination_item_id = ?originalDestinationItemId AND type = ?type";
                                var getLinkIdDataTable = await clientDatabaseConnection.GetAsync(query);
                                if (getLinkIdDataTable.Rows.Count == 0)
                                {
                                    logger.LogWarning($"Could not find link ID with itemId = {originalItemId}, destinationItemId = {originalDestinationItemId} and type = {type}");
                                    result.Errors.Add($"Kan koppeling-ID met itemId = {originalItemId}, destinationItemId = {originalDestinationItemId} and type = {type} niet vinden");
                                    continue;
                                }

                                var originalLinkId = getLinkIdDataTable.Rows[0].Field<ulong>("id");

                                query = $@"{queryPrefix}
                                        INSERT IGNORE INTO `{productionCustomer.Database.DatabaseName}`.`{tableName}` (item_id, destination_item_id, ordering, type)
                                        VALUES (?itemId, ?destinationItemId, ?ordering, ?type);";
                                var newLinkId = Convert.ToUInt64(await clientDatabaseConnection.InsertRecordAsync(query));

                                // Map the item ID from wiser_history to the ID of the newly created item, locally and in database.
                                await AddIdMapping(idMapping, tableName, originalLinkId, newLinkId, selectedEnvironmentCustomer);

                                break;
                            }
                            case "CHANGE_LINK":
                            {
                                clientDatabaseConnection.AddParameter("oldItemId", oldItemId);
                                clientDatabaseConnection.AddParameter("oldDestinationItemId", oldDestinationItemId);
                                clientDatabaseConnection.AddParameter("newValue", newValue);
                                var query = $@"{queryPrefix}
                                            UPDATE `{productionCustomer.Database.DatabaseName}`.`{tableName}` 
                                            SET `{field.ToMySqlSafeValue(false)}` = ?newValue
                                            WHERE item_id = ?oldItemId
                                            AND destination_item_id = ?oldDestinationItemId";
                                await clientDatabaseConnection.ExecuteAsync(query);
                                break;
                            }
                            case "REMOVE_LINK":
                            {
                                clientDatabaseConnection.AddParameter("oldItemId", oldItemId);
                                clientDatabaseConnection.AddParameter("oldDestinationItemId", oldDestinationItemId);
                                var query = $@"{queryPrefix}
                                            DELETE FROM `{productionCustomer.Database.DatabaseName}`.`{tableName}`
                                            WHERE item_id = ?oldItemId
                                            AND destination_item_id = ?oldDestinationItemId";
                                await clientDatabaseConnection.ExecuteAsync(query);
                                break;
                            }
                            case "UPDATE_ITEMLINKDETAIL":
                            {
                                throw new NotImplementedException();

                                break;
                            }
                            case "ADD_FILE":
                            {
                                throw new NotImplementedException();

                                break;
                            }
                            case "UPDATE_FILE":
                            {
                                throw new NotImplementedException();

                                break;
                            }
                            case "DELETE_FILE":
                            {
                                throw new NotImplementedException();

                                break;
                            }
                            case "INSERT_PERMISSION":
                            {
                                throw new NotImplementedException();

                                break;
                            }
                            case "UPDATE_PERMISSION":
                            {
                                throw new NotImplementedException();

                                break;
                            }
                            case "DELETE_PERMISSION":
                            {
                                throw new NotImplementedException();

                                break;
                            }
                            case "INSERT_USER_ROLE":
                            {
                                throw new NotImplementedException();

                                break;
                            }
                            case "UPDATE_USER_ROLE":
                            {
                                throw new NotImplementedException();

                                break;
                            }
                            case "DELETE_USER_ROLE":
                            {
                                throw new NotImplementedException();

                                break;
                            }
                            default:
                                throw new ArgumentOutOfRangeException(nameof(action), action, $"Unsupported action for history synchronisation: '{action}'");
                        }

                        result.SuccessfulChanges++;
                        historyItemsSynchronised.Add(historyId);
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, $"An error occurred while trying to synchronise history ID '{historyId}' from '{selectedEnvironmentCustomer.Database.DatabaseName}' to '{productionCustomer.Database.DatabaseName}'");
                        result.Errors.Add($"Het is niet gelukt om de wijziging '{action}' voor item '{originalItemId}' over te zetten. De fout was: {exception.Message}");
                    }
                }
                
                // Clear wiser_history in the selected environment, so that next time we can just sync all changes again.
                if (historyItemsSynchronised.Any())
                {
                    await clientDatabaseConnection.ExecuteAsync($"DELETE FROM `{selectedEnvironmentCustomer.Database.DatabaseName}`.`{WiserTableNames.WiserHistory}` WHERE id IN ({String.Join(",", historyItemsSynchronised)})");
                }

                // Commit the transaction when everything succeeded.
                //await clientDatabaseConnection.CommitTransactionAsync();
            }
            catch (Exception exception)
            {
                //await clientDatabaseConnection.RollbackTransactionAsync();

                throw;
            }

            return new ServiceResult<SynchroniseChangesToProductionResultModel>(result);
        }

        private async Task AddIdMapping(Dictionary<string, Dictionary<ulong, ulong>> idMappings, string tableName, ulong originalItemId, ulong newItemId, CustomerModel selectedEnvironmentCustomer)
        {
            string query;
            if (!idMappings.ContainsKey(tableName))
            {
                idMappings.Add(tableName, new Dictionary<ulong, ulong>());
            }

            idMappings[tableName].Add(originalItemId, newItemId);
            query = $@"INSERT INTO `{selectedEnvironmentCustomer.Database.DatabaseName}`.`{WiserTableNames.WiserIdMappings}` 
                                    (table_name, our_id, production_id)
                                    VALUES (?tableName, ?ourId, ?productionId)";
            clientDatabaseConnection.AddParameter("tableName", tableName);
            clientDatabaseConnection.AddParameter("ourId", originalItemId);
            clientDatabaseConnection.AddParameter("productionId", newItemId);
            await clientDatabaseConnection.ExecuteAsync(query);
        }

        private static ulong? GetMappedId(string tableName, Dictionary<string, Dictionary<ulong, ulong>> idMapping, ulong? id)
        {
            if (id is null or 0)
            {
                return id;
            }

            if (tableName.EndsWith(WiserTableNames.WiserItem, StringComparison.OrdinalIgnoreCase) && idMapping.ContainsKey(tableName) && idMapping[tableName].ContainsKey(id.Value))
            {
                id = idMapping[tableName][id.Value];
            }
            else
            {
                id = idMapping.FirstOrDefault(x => x.Value.ContainsKey(id.Value)).Value?[id.Value] ?? id;
            }

            return id;
        }

        #endregion

        #region Private functions

        /// <summary>
        /// Inserts or updates a customer in the database, based on <see cref="CustomerModel.Id"/>.
        /// </summary>
        /// <param name="customer">The customer to add or update.</param>
        private async Task CreateOrUpdateCustomerAsync(CustomerModel customer)
        {
            // Note: Passwords should be encrypted by Wiser before sending them to the API.
            wiserDatabaseConnection.ClearParameters();
            wiserDatabaseConnection.AddParameter("customerid", customer.CustomerId);
            wiserDatabaseConnection.AddParameter("name", customer.Name);
            wiserDatabaseConnection.AddParameter("db_host", customer.Database.Host);
            wiserDatabaseConnection.AddParameter("db_login", customer.Database.Username);
            wiserDatabaseConnection.AddParameter("db_passencrypted", customer.Database.Password ?? String.Empty);
            wiserDatabaseConnection.AddParameter("db_port", customer.Database.PortNumber);
            wiserDatabaseConnection.AddParameter("db_dbname", customer.Database.DatabaseName);
            wiserDatabaseConnection.AddParameter("encryption_key", customer.EncryptionKey);
            wiserDatabaseConnection.AddParameter("encryption_key_test", customer.EncryptionKey);
            wiserDatabaseConnection.AddParameter("subdomain", customer.SubDomain);
            wiserDatabaseConnection.AddParameter("wiser_title", customer.WiserTitle);

            customer.Id = await wiserDatabaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(ApiTableNames.WiserCustomers, customer.Id);
        }

        /// <summary>
        /// Generates a connection string for a customer.
        /// </summary>
        /// <param name="customer">The customer.</param>
        /// <param name="passwordIsEncrypted">Whether the password is saved encrypted in the <see cref="CustomerModel"/>.</param>
        private string GenerateConnectionStringFromCustomer(CustomerModel customer, bool passwordIsEncrypted = true)
        {
            var decryptedPassword = passwordIsEncrypted ? customer.Database.Password.DecryptWithAesWithSalt(apiSettings.DatabasePasswordEncryptionKey) : customer.Database.Password;
            return $"server={customer.Database.Host};port={(customer.Database.PortNumber > 0 ? customer.Database.PortNumber : 3306)};uid={customer.Database.Username};pwd={decryptedPassword};database={customer.Database.DatabaseName};AllowUserVariables=True;ConvertZeroDateTime=true;CharSet=utf8";
        }

        #endregion
    }
}