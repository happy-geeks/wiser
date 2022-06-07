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
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;

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

            var productionConnection = new MySqlConnection(GenerateConnectionStringFromCustomer(productionCustomer));
            var environmentConnection = new MySqlConnection(GenerateConnectionStringFromCustomer(selectedEnvironmentCustomer));
            await productionConnection.OpenAsync();
            await environmentConnection.OpenAsync();
            var productionTransaction = await productionConnection.BeginTransactionAsync();
            var environmentTransaction = await environmentConnection.BeginTransactionAsync();
            var sqlParameters = new Dictionary<string, object>();
            
            try
            {
                // Create the wiser_id_mappings table, in the selected environment, if it doesn't exist yet.
                // We need it to map IDs of the selected environment to IDs of the production environment, because they are not always the same.
                await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> {WiserTableNames.WiserIdMappings}, selectedEnvironmentCustomer.Database.DatabaseName);
                
                // Get all history since last synchronisation.
                var dataTable = new DataTable();
                await using (var environmentCommand = environmentConnection.CreateCommand())
                {
                    environmentCommand.CommandText = $"SELECT * FROM `{WiserTableNames.WiserHistory}` ORDER BY id ASC";
                    using var environmentAdapter = new MySqlDataAdapter(environmentCommand);
                    await environmentAdapter.FillAsync(dataTable);
                }

                // Srt saveHistory and username parameters for all queries.
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

                sqlParameters.Add("username", username);

                // We need to lock all tables we're going to use, to make sure no other changes can be done while we're busy synchronising.
                var tablesToLock = new List<string> { WiserTableNames.WiserHistory };
                foreach (DataRow dataRow in dataTable.Rows)
                {
                    var tableName = dataRow.Field<string>("tablename");
                    if (String.IsNullOrWhiteSpace(tableName) || tablesToLock.Contains(tableName))
                    {
                        continue;
                    }
                    
                    tablesToLock.Add(tableName);
                    if (WiserTableNames.TablesWithArchive.Any(table => tableName.EndsWith(table, StringComparison.OrdinalIgnoreCase)))
                    {
                        tablesToLock.Add($"{tableName}{WiserTableNames.ArchiveSuffix}");
                    }

                    // If we have a table that has an ID from wiser_item, then always lock wiser_item as well, because we will read from it later.
                    var originalItemId = Convert.ToUInt64(dataRow["item_id"]);
                    var (tablePrefix, isWiserItemChange) = GetTablePrefix(tableName, originalItemId);
                    var wiserItemTableName = $"{tablePrefix}{WiserTableNames.WiserItem}";
                    if (isWiserItemChange && originalItemId > 0 && !tablesToLock.Contains(wiserItemTableName))
                    {
                        tablesToLock.Add(wiserItemTableName);
                        tablesToLock.Add($"{wiserItemTableName}{WiserTableNames.ArchiveSuffix}");
                    }
                }

                // Add tables from wiser_id_mappings to tables to lock.
                await using (var command = environmentConnection.CreateCommand())
                {
                    command.CommandText = $@"SELECT DISTINCT table_name FROM `{WiserTableNames.WiserIdMappings}`";
                    var mappingDataTable = new DataTable();
                    using var adapter = new MySqlDataAdapter(command);
                    await adapter.FillAsync(mappingDataTable);
                    foreach (DataRow dataRow in mappingDataTable.Rows)
                    {
                        var tableName = dataRow.Field<string>("table_name");
                        if (String.IsNullOrWhiteSpace(tableName) || tablesToLock.Contains(tableName))
                        {
                            continue;
                        }
                        
                        tablesToLock.Add(tableName);
                        
                        if (WiserTableNames.TablesWithArchive.Any(table => tableName.EndsWith(table, StringComparison.OrdinalIgnoreCase)))
                        {
                            tablesToLock.Add($"{tableName}{WiserTableNames.ArchiveSuffix}");
                        }
                    }
                }

                // Lock the tables we're going to use, to be sure that other processes don't mess up our synchronisation.
                await using (var productionCommand = productionConnection.CreateCommand())
                {
                    productionCommand.CommandText = $"LOCK TABLES {String.Join(", ", tablesToLock.Select(table => $"{table} WRITE"))}";
                    await productionCommand.ExecuteNonQueryAsync();
                }
                await using (var environmentCommand = environmentConnection.CreateCommand())
                {
                    environmentCommand.CommandText = $"LOCK TABLES {WiserTableNames.WiserIdMappings} WRITE, {String.Join(", ", tablesToLock.Select(table => $"{table} WRITE"))}";
                    await environmentCommand.ExecuteNonQueryAsync();
                }

                // This is to cache the entity types for all changed items, so that we don't have to execute a query for every changed detail of the same item.
                var entityTypes = new Dictionary<ulong, string>();

                // This is to map one item ID to another. This is needed because when someone creates a new item in the other environment, that ID could already exist in the production environment.
                // So we need to map the ID that is saved in wiser_history to the new ID of the item that we create in the production environment.
                var idMapping = new Dictionary<string, Dictionary<ulong, ulong>>();
                await using (var environmentCommand = environmentConnection.CreateCommand())
                {
                    environmentCommand.CommandText = $@"SELECT table_name, our_id, production_id FROM `{WiserTableNames.WiserIdMappings}`";
                    using var environmentAdapter = new MySqlDataAdapter(environmentCommand);
                    
                    var idMappingDatatable = new DataTable();
                    await environmentAdapter.FillAsync(idMappingDatatable);
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
                }

                // Start synchronising all history items one by one.
                var historyItemsSynchronised = new List<ulong>();
                foreach (DataRow dataRow in dataTable.Rows)
                {
                    var historyId = Convert.ToUInt64(dataRow["id"]);
                    var action = dataRow.Field<string>("action").ToUpperInvariant();
                    var tableName = dataRow.Field<string>("tablename") ?? "";
                    var originalObjectId = Convert.ToUInt64(dataRow["item_id"]);
                    var objectId = originalObjectId;
                    var originalItemId = originalObjectId;
                    var itemId = originalObjectId;
                    var field = dataRow.Field<string>("field");
                    var oldValue = dataRow.Field<string>("oldvalue");
                    var newValue = dataRow.Field<string>("newvalue");
                    var languageCode = dataRow.Field<string>("language_code") ?? "";
                    var groupName = dataRow.Field<string>("groupname") ?? "";
                    ulong? linkId = null;
                    ulong? originalLinkId = null;
                    ulong? originalFileId = null;
                    ulong? fileId = null;
                    var entityType = "";

                    // Variables for item link changes.
                    var destinationItemId = 0UL;
                    ulong? oldItemId = null;
                    ulong? oldDestinationItemId = null;

                    try
                    {
                        // Make sure we have the correct item ID. For some actions, the item id is saved in a different column.
                        switch (action)
                        {
                            case "REMOVE_LINK":
                                destinationItemId = itemId;
                                itemId = Convert.ToUInt64(oldValue);
                                originalItemId = itemId;
                                break;
                            case "UPDATE_ITEMLINKDETAIL":
                            case "CHANGE_LINK":
                            {
                                linkId = itemId;
                                originalLinkId = linkId;

                                // When a link has been changed, it's possible that the ID of one of the items is changed.
                                // It's also possible that this is a new link that the production database didn't have yet (and so the ID of the link will most likely be different).
                                // Therefor we need to find the original item and destination IDs, so that we can use those to update the link in the production database.
                                sqlParameters["linkId"] = itemId;
                                
                                await using (var environmentCommand = environmentConnection.CreateCommand())
                                {
                                    AddParametersToCommand(sqlParameters, environmentCommand);
                                    environmentCommand.CommandText = $@"SELECT item_id, destination_item_id FROM `{WiserTableNames.WiserItemLink}` WHERE id = ?linkId";
                                    var linkDataTable = new DataTable();
                                    using var environmentAdapter = new MySqlDataAdapter(environmentCommand);
                                    await environmentAdapter.FillAsync(linkDataTable);
                                    if (linkDataTable.Rows.Count == 0)
                                    {
                                        environmentCommand.CommandText = $@"SELECT item_id, destination_item_id FROM `{WiserTableNames.WiserItemLink}{WiserTableNames.ArchiveSuffix}` WHERE id = ?linkId";
                                        await environmentAdapter.FillAsync(linkDataTable);
                                        if (linkDataTable.Rows.Count == 0)
                                        {
                                            // This should never happen, but just in case the ID somehow doesn't exist anymore, log a warning and continue on to the next item.
                                            logger.LogWarning($"Could not find link with id '{itemId}' in database '{selectedEnvironmentCustomer.Database.DatabaseName}'. Skipping this history record in synchronisation to production.");
                                            continue;
                                        }
                                    }

                                    itemId = Convert.ToUInt64(linkDataTable.Rows[0]["item_id"]);
                                    originalItemId = itemId;
                                    destinationItemId = Convert.ToUInt64(linkDataTable.Rows[0]["destination_item_id"]);
                                }

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
                                        originalItemId = itemId;
                                        oldDestinationItemId = destinationItemId;
                                        break;
                                }

                                break;
                            }
                            case "ADD_LINK":
                                destinationItemId = itemId;
                                itemId = Convert.ToUInt64(newValue);
                                originalItemId = itemId;

                                break;
                            case "ADD_FILE":
                            case "UPDATE_FILE":
                            case "DELETE_FILE":
                                fileId = itemId;
                                originalFileId = fileId;
                                itemId = String.Equals(oldValue, "item_id", StringComparison.OrdinalIgnoreCase) ? UInt64.Parse(newValue) : 0;
                                originalItemId = itemId;
                                linkId = String.Equals(oldValue, "itemlink_id", StringComparison.OrdinalIgnoreCase) ? UInt64.Parse(newValue) : 0;
                                originalLinkId = linkId;

                                break;
                            case "DELETE_ITEM":
                                entityType = field;
                                break;
                        }

                        // Did we map the item ID to something else? Then use that new ID.
                        var originalDestinationItemId = destinationItemId;
                        itemId = GetMappedId(tableName, idMapping, itemId).Value;
                        destinationItemId = GetMappedId(tableName, idMapping, destinationItemId).Value;
                        oldItemId = GetMappedId(tableName, idMapping, oldItemId);
                        oldDestinationItemId = GetMappedId(tableName, idMapping, oldDestinationItemId);
                        linkId = GetMappedId(tableName, idMapping, linkId);
                        fileId = GetMappedId(tableName, idMapping, fileId);
                        objectId = GetMappedId(tableName, idMapping, objectId) ?? 0;

                        var isWiserItemChange = true;

                        // Figure out the entity type of the item that was updated, so that we can check if we need to do anything with it.
                        // We don't want to synchronise certain entity types, such as users, relations and baskets.
                        if (entityTypes.ContainsKey(itemId))
                        {
                            entityType = entityTypes[itemId];
                        }
                        else if (String.IsNullOrWhiteSpace(entityType))
                        {
                            // Check if this item is saved in a dedicated table with a certain prefix.
                            var (tablePrefix, wiserItemChange) = GetTablePrefix(tableName, originalItemId);
                            isWiserItemChange = wiserItemChange;

                            if (isWiserItemChange && originalItemId > 0)
                            {
                                sqlParameters["itemId"] = originalItemId;
                                var itemDataTable = new DataTable();
                                await using var environmentCommand = environmentConnection.CreateCommand();
                                AddParametersToCommand(sqlParameters, environmentCommand);
                                environmentCommand.CommandText = $"SELECT entity_type FROM `{tablePrefix}{WiserTableNames.WiserItem}` WHERE id = ?itemId";
                                using var environmentAdapter = new MySqlDataAdapter(environmentCommand);
                                await environmentAdapter.FillAsync(itemDataTable);
                                if (itemDataTable.Rows.Count == 0)
                                {
                                    // If item doesn't exist, check the archive table, it might have been deleted.
                                    environmentCommand.CommandText = $"SELECT entity_type FROM `{tablePrefix}{WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix}` WHERE id = ?itemId";
                                    await environmentAdapter.FillAsync(itemDataTable);
                                    if (itemDataTable.Rows.Count == 0)
                                    {
                                        logger.LogWarning($"Could not find item with ID '{originalItemId}', so skipping it...");
                                        result.Errors.Add($"Item met ID '{originalItemId}' kon niet gevonden worden.");
                                        continue;
                                    }
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

                        // Update the item in the production environment.
                        switch (action)
                        {
                            case "CREATE_ITEM":
                            {
                                var newItemId = await GenerateNewId(tableName, productionConnection, environmentConnection);
                                sqlParameters["newId"] = newItemId;
                                
                                await using var productionCommand = productionConnection.CreateCommand();
                                AddParametersToCommand(sqlParameters, productionCommand);
                                productionCommand.CommandText = $@"{queryPrefix}
INSERT INTO `{tableName}` (id, entity_type) VALUES (?newId, '')";
                                await productionCommand.ExecuteNonQueryAsync();

                                // Map the item ID from wiser_history to the ID of the newly created item, locally and in database.
                                await AddIdMapping(idMapping, tableName, originalItemId, newItemId, environmentConnection);

                                break;
                            }
                            case "UPDATE_ITEM" when tableName.EndsWith(WiserTableNames.WiserItemDetail, StringComparison.OrdinalIgnoreCase):
                            {
                                sqlParameters["itemId"] = itemId;
                                sqlParameters["key"] = field;
                                sqlParameters["languageCode"] = languageCode;
                                sqlParameters["groupName"] = groupName;

                                await using var productionCommand = productionConnection.CreateCommand();
                                AddParametersToCommand(sqlParameters, productionCommand);
                                productionCommand.CommandText = queryPrefix;
                                if (String.IsNullOrWhiteSpace(newValue))
                                {
                                    productionCommand.CommandText += $@"DELETE FROM `{tableName}`
WHERE item_id = ?itemId
AND `key` = ?key
AND language_code = ?languageCode
AND groupname = ?groupName";
                                }
                                else
                                {
                                    var useLongValue = newValue.Length > 1000;
                                    sqlParameters["value"] = useLongValue ? "" : newValue;
                                    sqlParameters["longValue"] = useLongValue ? newValue : "";

                                    AddParametersToCommand(sqlParameters, productionCommand);
                                    productionCommand.CommandText += $@"INSERT INTO `{tableName}` (language_code, item_id, groupname, `key`, value, long_value)
VALUES (?languageCode, ?itemId, ?groupName, ?key, ?value, ?longValue)
ON DUPLICATE KEY UPDATE groupname = VALUES(groupname), value = VALUES(value), long_value = VALUES(long_value)";
                                }

                                await productionCommand.ExecuteNonQueryAsync();

                                break;
                            }
                            case "UPDATE_ITEM" when tableName.EndsWith(WiserTableNames.WiserItem, StringComparison.OrdinalIgnoreCase):
                            {
                                sqlParameters["itemId"] = itemId;
                                sqlParameters["newValue"] = newValue;
                                
                                await using var productionCommand = productionConnection.CreateCommand();
                                AddParametersToCommand(sqlParameters, productionCommand);
                                productionCommand.CommandText = $@"{queryPrefix}
UPDATE `{tableName}` 
SET `{field.ToMySqlSafeValue(false)}` = ?newValue
WHERE id = ?itemId";
                                await productionCommand.ExecuteNonQueryAsync();

                                break;
                            }
                            case "DELETE_ITEM":
                            {
                                sqlParameters["itemId"] = itemId;
                                sqlParameters["entityType"] = entityType;
                                
                                await using var productionCommand = productionConnection.CreateCommand();
                                AddParametersToCommand(sqlParameters, productionCommand);
                                productionCommand.CommandText = $@"{queryPrefix} CALL DeleteWiser2Item(?itemId, ?entityType);";
                                await productionCommand.ExecuteNonQueryAsync();

                                break;
                            }
                            case "ADD_LINK":
                            {
                                var split = field.Split(',');
                                var type = split[0];
                                var ordering = split.Length > 1 ? split[1] : "0";
                                sqlParameters["itemId"] = itemId;
                                sqlParameters["originalItemId"] = originalItemId;
                                sqlParameters["ordering"] = ordering;
                                sqlParameters["destinationItemId"] = destinationItemId;
                                sqlParameters["originalDestinationItemId"] = originalDestinationItemId;
                                sqlParameters["type"] = type;

                                // Get the original link ID, so we can map it to the new one.
                                await using (var environmentCommand = environmentConnection.CreateCommand())
                                {
                                    AddParametersToCommand(sqlParameters, environmentCommand);
                                    environmentCommand.CommandText = $@"SELECT id FROM `{tableName}` WHERE item_id = ?originalItemId AND destination_item_id = ?originalDestinationItemId AND type = ?type";
                                    var getLinkIdDataTable = new DataTable();
                                    using var environmentAdapter = new MySqlDataAdapter(environmentCommand);
                                    await environmentAdapter.FillAsync(getLinkIdDataTable);
                                    if (getLinkIdDataTable.Rows.Count == 0)
                                    {
                                        logger.LogWarning($"Could not find link ID with itemId = {originalItemId}, destinationItemId = {originalDestinationItemId} and type = {type}");
                                        result.Errors.Add($"Kan koppeling-ID met itemId = {originalItemId}, destinationItemId = {originalDestinationItemId} and type = {type} niet vinden");
                                        continue;
                                    }

                                    originalLinkId = Convert.ToUInt64(getLinkIdDataTable.Rows[0]["id"]);
                                    linkId = await GenerateNewId(tableName, productionConnection, environmentConnection);
                                }

                                sqlParameters["newId"] = linkId;
                                await using var productionCommand = productionConnection.CreateCommand();
                                AddParametersToCommand(sqlParameters, productionCommand);
                                productionCommand.CommandText = $@"{queryPrefix}
INSERT IGNORE INTO `{tableName}` (id, item_id, destination_item_id, ordering, type)
VALUES (?newId, ?itemId, ?destinationItemId, ?ordering, ?type);";
                                await productionCommand.ExecuteNonQueryAsync();

                                // Map the item ID from wiser_history to the ID of the newly created item, locally and in database.
                                await AddIdMapping(idMapping, tableName, originalLinkId.Value, linkId.Value, environmentConnection);

                                break;
                            }
                            case "CHANGE_LINK":
                            {
                                sqlParameters["oldItemId"] = oldItemId;
                                sqlParameters["oldDestinationItemId"] = oldDestinationItemId;
                                sqlParameters["newValue"] = newValue;
                                sqlParameters["type"] = field;
                                
                                await using var productionCommand = productionConnection.CreateCommand();
                                AddParametersToCommand(sqlParameters, productionCommand);
                                productionCommand.CommandText = $@"{queryPrefix}
UPDATE `{tableName}` 
SET `{field.ToMySqlSafeValue(false)}` = ?newValue
WHERE item_id = ?oldItemId
AND destination_item_id = ?oldDestinationItemId
AND type = ?type";
                                await productionCommand.ExecuteNonQueryAsync();
                                break;
                            }
                            case "REMOVE_LINK":
                            {
                                sqlParameters["oldItemId"] = oldItemId;
                                sqlParameters["oldDestinationItemId"] =  oldDestinationItemId;
                                sqlParameters["type"] = field;
                                
                                await using var productionCommand = productionConnection.CreateCommand();
                                AddParametersToCommand(sqlParameters, productionCommand);
                                productionCommand.CommandText = $@"{queryPrefix}
DELETE FROM `{tableName}`
WHERE item_id = ?oldItemId
AND destination_item_id = ?oldDestinationItemId
AND type = ?type";
                                await productionCommand.ExecuteNonQueryAsync();
                                break;
                            }
                            case "UPDATE_ITEMLINKDETAIL":
                            {
                                sqlParameters["linkId"] = linkId;
                                sqlParameters["key"] = field;
                                sqlParameters["languageCode"] = languageCode;
                                sqlParameters["groupName"] = groupName;

                                await using var productionCommand = productionConnection.CreateCommand();
                                productionCommand.CommandText = queryPrefix;
                                if (String.IsNullOrWhiteSpace(newValue))
                                {
                                    productionCommand.CommandText += $@"DELETE FROM `{tableName}`
WHERE itemlink_id = ?linkId
AND `key` = ?key
AND language_code = ?languageCode
AND groupname = ?groupName";
                                }
                                else
                                {
                                    var useLongValue = newValue.Length > 1000;
                                    sqlParameters["value"] = useLongValue ? "" : newValue;
                                    sqlParameters["longValue"] = useLongValue ? newValue : "";

                                    productionCommand.CommandText += $@"INSERT INTO `{tableName}` (language_code, itemlink_id, groupname, `key`, value, long_value)
VALUES (?languageCode, ?linkId, ?groupName, ?key, ?value, ?longValue)
ON DUPLICATE KEY UPDATE groupname = VALUES(groupname), value = VALUES(value), long_value = VALUES(long_value)";
                                }

                                AddParametersToCommand(sqlParameters, productionCommand);
                                await productionCommand.ExecuteNonQueryAsync();

                                break;
                            }
                            case "ADD_FILE":
                            {
                                // oldValue contains either "item_id" or "itemlink_id", to indicate which of these columns is used for the ID that is saved in newValue.
                                var newFileId = await GenerateNewId(tableName, productionConnection, environmentConnection);
                                sqlParameters["fileItemId"] = newValue;
                                sqlParameters["newId"] = newFileId;

                                await using var productionCommand = productionConnection.CreateCommand();
                                AddParametersToCommand(sqlParameters, productionCommand);
                                productionCommand.CommandText = $@"{queryPrefix}
INSERT INTO `{tableName}` (id, `{oldValue.ToMySqlSafeValue(false)}`) 
VALUES (?newId, ?fileItemId)";
                                await productionCommand.ExecuteReaderAsync();

                                // Map the item ID from wiser_history to the ID of the newly created item, locally and in database.
                                await AddIdMapping(idMapping, tableName, originalObjectId, newFileId, environmentConnection);

                                break;
                            }
                            case "UPDATE_FILE":
                            {
                                sqlParameters["fileId"] = fileId;
                                sqlParameters["originalFileId"] = originalFileId;

                                if (String.Equals(field, "content_length", StringComparison.OrdinalIgnoreCase))
                                {
                                    // If the content length has been updated, we need to get the actual content from wiser_itemfile.
                                    // We don't save the content bytes in wiser_history, because then the history table would become too huge.
                                    byte[] file = null;
                                    await using (var environmentCommand = environmentConnection.CreateCommand())
                                    {
                                        AddParametersToCommand(sqlParameters, environmentCommand);
                                        environmentCommand.CommandText = $"SELECT content FROM `{tableName}` WHERE id = ?originalFileId";
                                        await using var productionReader = await environmentCommand.ExecuteReaderAsync();
                                        if (await productionReader.ReadAsync())
                                        {
                                            file = (byte[]) productionReader.GetValue(0);
                                        }
                                    }

                                    sqlParameters["contents"] = file;
                                    
                                    await using var productionCommand = productionConnection.CreateCommand();
                                    AddParametersToCommand(sqlParameters, productionCommand);
                                    productionCommand.CommandText = $@"{queryPrefix}
UPDATE `{tableName}`
SET content = ?content
WHERE id = ?fileId";
                                    await productionCommand.ExecuteNonQueryAsync();
                                }
                                else
                                {
                                    sqlParameters["newValue"] = newValue;

                                    await using var productionCommand = productionConnection.CreateCommand();
                                    AddParametersToCommand(sqlParameters, productionCommand);
                                    productionCommand.CommandText = $@"{queryPrefix}
UPDATE `{tableName}` 
SET `{field.ToMySqlSafeValue(false)}` = ?newValue
WHERE id = ?fileId";
                                    await productionCommand.ExecuteNonQueryAsync();
                                }

                                break;
                            }
                            case "DELETE_FILE":
                            {
                                sqlParameters["itemId"] = newValue;

                                await using var productionCommand = productionConnection.CreateCommand();
                                AddParametersToCommand(sqlParameters, productionCommand);
                                productionCommand.CommandText = $@"{queryPrefix}
DELETE FROM `{tableName}`
WHERE `{oldValue.ToMySqlSafeValue(false)}` = ?itemId";
                                await productionCommand.ExecuteReaderAsync();

                                break;
                            }
                            case "INSERT_ENTITY":
                            case "INSERT_ENTITYPROPERTY":
                            case "INSERT_QUERY":
                            case "INSERT_MODULE":
                            case "INSERT_DATA_SELECTOR":
                            case "INSERT_PERMISSION":
                            case "INSERT_USER_ROLE":
                            case "INSERT_FIELD_TEMPLATE":
                            case "INSERT_LINK_SETTING":
                            case "INSERT_API_CONNECTION":
                            {
                                var newEntityId = await GenerateNewId(tableName, productionConnection, environmentConnection);
                                sqlParameters["newId"] = newEntityId;
                                
                                await using var productionCommand = productionConnection.CreateCommand();
                                AddParametersToCommand(sqlParameters, productionCommand);

                                if (tableName.Equals(WiserTableNames.WiserEntity, StringComparison.OrdinalIgnoreCase))
                                {
                                    productionCommand.CommandText = $@"{queryPrefix}
INSERT INTO `{tableName}` (id, `name`) 
VALUES (?newId, '')";
                                }
                                else
                                {
                                    productionCommand.CommandText = $@"{queryPrefix}
INSERT INTO `{tableName}` (id) 
VALUES (?newId)";
                                }

                                await productionCommand.ExecuteNonQueryAsync();

                                // Map the item ID from wiser_history to the ID of the newly created item, locally and in database.
                                await AddIdMapping(idMapping, tableName, originalObjectId, newEntityId, environmentConnection);

                                break;
                            }
                            case "UPDATE_ENTITY":
                            case "UPDATE_ENTITYPROPERTY":
                            case "UPDATE_QUERY":
                            case "UPDATE_DATA_SELECTOR":
                            case "UPDATE_MODULE":
                            case "UPDATE_PERMISSION":
                            case "UPDATE_USER_ROLE":
                            case "UPDATE_FIELD_TEMPLATE":
                            case "UPDATE_LINK_SETTING":
                            case "UPDATE_API_CONNECTION":
                            {
                                sqlParameters["id"] = objectId;
                                sqlParameters["newValue"] = newValue;

                                await using var productionCommand = productionConnection.CreateCommand();
                                AddParametersToCommand(sqlParameters, productionCommand);
                                productionCommand.CommandText = $@"{queryPrefix}
UPDATE `{tableName}` 
SET `{field.ToMySqlSafeValue(false)}` = ?newValue
WHERE id = ?id";
                                await productionCommand.ExecuteNonQueryAsync();

                                break;
                            }
                            case "DELETE_ENTITY":
                            case "DELETE_ENTITYPROPERTY":
                            case "DELETE_QUERY":
                            case "DELETE_DATA_SELECTOR":
                            case "DELETE_MODULE":
                            case "DELETE_PERMISSION":
                            case "DELETE_USER_ROLE":
                            case "DELETE_FIELD_TEMPLATE":
                            case "DELETE_LINK_SETTING":
                            case "DELETE_API_CONNECTION":
                            {
                                sqlParameters["id"] = objectId;

                                await using var productionCommand = productionConnection.CreateCommand();
                                AddParametersToCommand(sqlParameters, productionCommand);
                                productionCommand.CommandText = $@"{queryPrefix}
DELETE FROM `{tableName}`
WHERE `id` = ?id";
                                await productionCommand.ExecuteNonQueryAsync();

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

                try
                {
                    // Clear wiser_history in the selected environment, so that next time we can just sync all changes again.
                    if (historyItemsSynchronised.Any())
                    {
                        await using var environmentCommand = environmentConnection.CreateCommand();
                        environmentCommand.CommandText = $"DELETE FROM `{WiserTableNames.WiserHistory}` WHERE id IN ({String.Join(",", historyItemsSynchronised)})";
                        await environmentCommand.ExecuteNonQueryAsync();
                    }

                    await EqualizeMappedIds(environmentConnection);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, $"An error occurred while trying to clean up after synchronising from '{selectedEnvironmentCustomer.Database.DatabaseName}' to '{productionCustomer.Database.DatabaseName}'");
                    result.Errors.Add($"Er is iets fout gegaan tijdens het opruimen na de synchronisatie. Het wordt aangeraden om deze omgeving niet meer te gebruiken voor synchroniseren naar productie, anders kunnen dingen dubbel gescynchroniseerd worden. U kunt wel een nieuwe omgeving maken en vanuit daar weer verder werken. De fout was: {exception.Message}");
                }

                // Always commit, so we keep our progress.
                await environmentTransaction.CommitAsync();
                await productionTransaction.CommitAsync();
            }
            finally
            {
                // Make sure we always unlock all tables when we're done, no matter what happens.
                await using (var environmentCommand = environmentConnection.CreateCommand())
                {
                    environmentCommand.CommandText = "UNLOCK TABLES";
                    await environmentCommand.ExecuteNonQueryAsync();
                }

                await using (var productionCommand = productionConnection.CreateCommand())
                {
                    productionCommand.CommandText = "UNLOCK TABLES";
                    await productionCommand.ExecuteNonQueryAsync();
                }

                // Dispose and cleanup.
                await environmentTransaction.DisposeAsync();
                await productionTransaction.DisposeAsync();
                
                await environmentConnection.CloseAsync();
                await productionConnection.CloseAsync();
                
                await environmentConnection.DisposeAsync();
                await productionConnection.DisposeAsync();
            }

            return new ServiceResult<SynchroniseChangesToProductionResultModel>(result);
        }

        /// <summary>
        /// Get the prefix for a wiser item table.
        /// </summary>
        /// <param name="tableName">The full name of the table.</param>
        /// <param name="originalItemId">The original item ID.</param>
        /// <returns>The table prefix and whether or not this is something connected to an item from [prefix]wiser_item.</returns>
        private static (string tablePrefix, bool isWiserItemChange) GetTablePrefix(string tableName, ulong originalItemId)
        {
            var isWiserItemChange = true;
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
                if (originalItemId == 0)
                {
                    isWiserItemChange = false;
                }
            }
            else if (tableName.EndsWith(WiserTableNames.WiserItemLink, StringComparison.OrdinalIgnoreCase))
            {
                tablePrefix = tableName.ReplaceCaseInsensitive(WiserTableNames.WiserItemLink, "");
            }
            else if (tableName.EndsWith(WiserTableNames.WiserItemLinkDetail, StringComparison.OrdinalIgnoreCase))
            {
                tablePrefix = tableName.ReplaceCaseInsensitive(WiserTableNames.WiserItemLinkDetail, "");
            }
            else
            {
                isWiserItemChange = false;
            }

            return (tablePrefix, isWiserItemChange);
        }

        /// <summary>
        /// Add all parameters from a dictionary to a <see cref="MySqlCommand"/>.
        /// </summary>
        /// <param name="parameters">The dictionary with the parameters.</param>
        /// <param name="command">The database command.</param>
        private static void AddParametersToCommand(Dictionary<string, object> parameters, MySqlCommand command)
        {
            command.Parameters.Clear();
            foreach (var parameter in parameters)
            {
                command.Parameters.AddWithValue(parameter.Key, parameter.Value);
            }
        }

        /// <summary>
        /// This will update IDs of items/files/etc in the selected environment so that they all will have the same ID as in the production environment.
        /// </summary>
        /// <param name="environmentConnection">The database connection to the selected environment.</param>
        private async Task EqualizeMappedIds(MySqlConnection environmentConnection)
        {
            await using var command = environmentConnection.CreateCommand();
            command.CommandText = $@"SELECT * FROM `{WiserTableNames.WiserIdMappings}` ORDER BY id DESC";
            var dataTable = new DataTable();
            using var adapter = new MySqlDataAdapter(command);
            await adapter.FillAsync(dataTable);
            
            foreach (DataRow dataRow in dataTable.Rows)
            {
                var mappingRowId = dataRow.Field<ulong>("id");
                var tableName = dataRow.Field<string>("table_name") ?? "";
                var ourId = dataRow.Field<ulong>("our_id");
                var productionId = dataRow.Field<ulong>("production_id");
                
                command.Parameters.AddWithValue("mappingRowId", mappingRowId);
                command.Parameters.AddWithValue("ourId", ourId);
                command.Parameters.AddWithValue("productionId", productionId);
                
                if (tableName.EndsWith(WiserTableNames.WiserItem, StringComparison.OrdinalIgnoreCase))
                {
                    var tablePrefix = tableName.ReplaceCaseInsensitive(WiserTableNames.WiserItem, "");
                    command.CommandText = $@"SET @saveHistory = FALSE;

-- Update the ID of the item itself.
UPDATE `{tablePrefix}{WiserTableNames.WiserItem}`
SET id = ?productionId
WHERE id = ?ourId;

-- Update all original IDs of items that are using this ID.
UPDATE `{tablePrefix}{WiserTableNames.WiserItem}`
SET original_item_id = ?productionId
WHERE original_item_id = ?ourId;

-- Update all parent IDs of items that are using this ID.
UPDATE `{tablePrefix}{WiserTableNames.WiserItem}`
SET parent_item_id = ?productionId
WHERE parent_item_id = ?ourId;

-- Update item details to use the new ID.
UPDATE `{tablePrefix}{WiserTableNames.WiserItemDetail}`
SET item_id = ?productionId
WHERE item_id = ?ourId;

-- Update item files to use the new ID.
UPDATE `{WiserTableNames.WiserItemFile}`
SET item_id = ?productionId
WHERE item_id = ?ourId;

-- Update item links to use the new ID.
UPDATE `{WiserTableNames.WiserItemLink}`
SET item_id = ?productionId
WHERE item_id = ?ourId;

-- Update item links to use the new ID.
UPDATE `{WiserTableNames.WiserItemLink}`
SET destination_item_id = ?productionId
WHERE destination_item_id = ?ourId;";
                    await command.ExecuteNonQueryAsync();
                }
                else if (tableName.EndsWith(WiserTableNames.WiserItemFile, StringComparison.OrdinalIgnoreCase) || tableName.EndsWith(WiserTableNames.WiserEntity, StringComparison.OrdinalIgnoreCase))
                {
                    command.CommandText = $@"SET @saveHistory = FALSE;
UPDATE `{tableName.ToMySqlSafeValue(false)}` 
SET id = ?productionId 
WHERE id = ?ourId;";
                    await command.ExecuteNonQueryAsync();
                }
                else if (tableName.EndsWith(WiserTableNames.WiserItemLink, StringComparison.OrdinalIgnoreCase))
                {
                    command.CommandText = $@"SET @saveHistory = FALSE;

UPDATE `{WiserTableNames.WiserItemLink}` 
SET id = ?productionId 
WHERE id = ?ourId;

UPDATE `{WiserTableNames.WiserItemLinkDetail}` 
SET link_id = ?productionId 
WHERE link_id = ?ourId;";
                    await command.ExecuteNonQueryAsync();
                }
                else
                {
                    command.CommandText = $@"SET @saveHistory = FALSE;

UPDATE `{tableName}` 
SET id = ?productionId 
WHERE id = ?ourId;";
                    await command.ExecuteNonQueryAsync();
                }
                
                // Delete the row when we succeeded in updating the ID.
                command.CommandText = $"DELETE FROM `{WiserTableNames.WiserIdMappings}` WHERE id = ?mappingRowId";
                await command.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Generates a new ID for the specified table. This will get the highest number from both databases and add 1 to that number.
        /// This is to make sure that the new ID will not exist anywhere yet, to prevent later synchronisation problems.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="productionConnection">The connection to the production database.</param>
        /// <param name="environmentConnection">The connection to the environment database.</param>
        /// <returns>The new ID that should be used for the first new item to be inserted into this table.</returns>
        private async Task<ulong> GenerateNewId(string tableName, MySqlConnection productionConnection, MySqlConnection environmentConnection)
        {
            await using var productionCommand = productionConnection.CreateCommand();
            await using var environmentCommand = environmentConnection.CreateCommand();
            
            productionCommand.CommandText = $"SELECT MAX(id) AS maxId FROM `{tableName}`";
            environmentCommand.CommandText = $"SELECT MAX(id) AS maxId FROM `{tableName}`";

            var maxProductionId = 0UL;
            var maxEnvironmentId = 0UL;
            
            await using var productionReader = await productionCommand.ExecuteReaderAsync();
            if (await productionReader.ReadAsync())
            {
                maxProductionId = Convert.ToUInt64(productionReader.GetValue(0));
            }
            await using var environmentReader = await environmentCommand.ExecuteReaderAsync();
            if (await environmentReader.ReadAsync())
            {
                maxEnvironmentId = Convert.ToUInt64(environmentReader.GetValue(0));
            }


            return Math.Max(maxProductionId, maxEnvironmentId) + 1;
        }

        /// <summary>
        /// Add an ID mapping, to map the ID of the environment database to the same item with a different ID in the production database.
        /// </summary>
        /// <param name="idMappings">The dictionary that contains the in-memory mappings.</param>
        /// <param name="tableName">The table that the ID belongs to.</param>
        /// <param name="originalItemId">The ID of the item in the selected environment.</param>
        /// <param name="newItemId">The ID of the item in the production environment.</param>
        /// <param name="environmentConnection">The database connection to the selected environment.</param>
        private async Task AddIdMapping(IDictionary<string, Dictionary<ulong, ulong>> idMappings, string tableName, ulong originalItemId, ulong newItemId, MySqlConnection environmentConnection)
        {
            if (!idMappings.ContainsKey(tableName))
            {
                idMappings.Add(tableName, new Dictionary<ulong, ulong>());
            }

            idMappings[tableName].Add(originalItemId, newItemId);
            await using var environmentCommand = environmentConnection.CreateCommand();
            environmentCommand.CommandText = $@"INSERT INTO `{WiserTableNames.WiserIdMappings}` 
(table_name, our_id, production_id)
VALUES (?tableName, ?ourId, ?productionId)";
            
            environmentCommand.Parameters.AddWithValue("tableName", tableName);
            environmentCommand.Parameters.AddWithValue("ourId", originalItemId);
            environmentCommand.Parameters.AddWithValue("productionId", newItemId);
            await environmentCommand.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Get the ID of an item from the mappings. The returned ID will be the ID of the same item in the production environment.
        /// If there is no mapping for this ID, it means the ID is the same in both environments and the input will be returned.
        /// </summary>
        /// <param name="tableName">The table that the ID belongs to.</param>
        /// <param name="idMapping">The dictionary that contains all the ID mappings.</param>
        /// <param name="id">The ID to get the mapped value of.</param>
        /// <returns>The ID of the same item in the production environment.</returns>
        private static ulong? GetMappedId(string tableName, IReadOnlyDictionary<string, Dictionary<ulong, ulong>> idMapping, ulong? id)
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