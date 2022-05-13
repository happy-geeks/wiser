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
        private readonly GclSettings gclSettings;
        private readonly ApiSettings apiSettings;
        private readonly IDatabaseConnection wiserDatabaseConnection;

        #endregion

        /// <summary>
        /// Creates a new instance of WiserCustomersService.
        /// </summary>
        public WiserCustomersService(IDatabaseConnection connection, IOptions<ApiSettings> apiSettings, IOptions<GclSettings> gclSettings, IDatabaseHelpersService databaseHelpersService)
        {
            clientDatabaseConnection = connection;
            this.databaseHelpersService = databaseHelpersService;
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
                    
                    if (!String.IsNullOrWhiteSpace(customer.LiveDatabase?.Password))
                    {
                        customer.LiveDatabase.Password = customer.LiveDatabase.Password.EncryptWithAesWithSalt(apiSettings.DatabasePasswordEncryptionKey);
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
                    if (customer.LiveDatabase != null)
                    {
                        customer.LiveDatabase.Password = null;
                    }
                    
                    var createTablesQuery = await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.CreateTables.sql");
                    var createTriggersQuery = await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.CreateTriggers.sql");
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

            var databaseName = $"{currentCustomer.LiveDatabase.DatabaseName}_{databaseNameBuilder}".ToMySqlSafeValue(false);
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
                LiveDatabase = new ConnectionInformationModel
                {
                    Host = currentCustomer.LiveDatabase.Host,
                    Password = currentCustomer.LiveDatabase.Password,
                    Username = currentCustomer.LiveDatabase.Username,
                    DatabaseName = databaseName,
                    PortNumber = currentCustomer.LiveDatabase.PortNumber
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
                var query = @"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
                            WHERE TABLE_SCHEMA = ?currentSchema
                            AND TABLE_TYPE = 'BASE TABLE'
                            AND TABLE_NAME NOT LIKE '\_%'";

                clientDatabaseConnection.AddParameter("currentSchema", currentCustomer.LiveDatabase.DatabaseName);
                clientDatabaseConnection.AddParameter("newSchema", newCustomer.LiveDatabase.DatabaseName);
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
                var entityTypesToSkip = new List<string>
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
                
                // Create the tables in a new connection, because these cause implicit commits.
                await using (var mysqlConnection = new MySqlConnection(GenerateConnectionStringFromCustomer(newCustomer)))
                {
                    await mysqlConnection.OpenAsync();
                    await using (var command = mysqlConnection.CreateCommand())
                    {
                        foreach (DataRow dataRow in dataTable.Rows)
                        {
                            var tableName = dataRow.Field<string>("TABLE_NAME");

                            command.CommandText = $"CREATE TABLE `{newCustomer.LiveDatabase.DatabaseName.ToMySqlSafeValue(false)}`.`{tableName.ToMySqlSafeValue(false)}` LIKE `{currentCustomer.LiveDatabase.DatabaseName.ToMySqlSafeValue(false)}`.`{tableName.ToMySqlSafeValue(false)}`";
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }

                // Fill the tables with data.
                var entityTypesString = String.Join(",", entityTypesToSkip.Select(x => $"'{x}'"));
                foreach (DataRow dataRow in dataTable.Rows)
                {
                    var tableName = dataRow.Field<string>("TABLE_NAME");

                    try
                    {
                        // For Wiser tables, we don't want to copy customer data, so copy everything except data of certain entity types.
                        if (tableName!.EndsWith(WiserTableNames.WiserItem, StringComparison.OrdinalIgnoreCase))
                        {
                            await clientDatabaseConnection.ExecuteAsync($@"INSERT INTO `{newCustomer.LiveDatabase.DatabaseName}`.`{tableName}` 
                                                                        SELECT * FROM `{currentCustomer.LiveDatabase.DatabaseName}`.`{tableName}` 
                                                                        WHERE entity_type NOT IN ('{String.Join("','", entityTypesToSkip)}')");
                            continue;
                        }

                        if (tableName!.EndsWith(WiserTableNames.WiserItemDetail, StringComparison.OrdinalIgnoreCase))
                        {
                            var prefix = tableName.Replace(WiserTableNames.WiserItemDetail, "");
                            await clientDatabaseConnection.ExecuteAsync($@"INSERT INTO `{newCustomer.LiveDatabase.DatabaseName}`.`{tableName}` 
                                                                        SELECT detail.* FROM `{currentCustomer.LiveDatabase.DatabaseName}`.`{tableName}` AS detail
                                                                        JOIN `{currentCustomer.LiveDatabase.DatabaseName}`.`{prefix}{WiserTableNames.WiserItem}` AS item ON item.id = detail.item_id AND item.entity_type NOT IN ({entityTypesString})");
                            continue;
                        }

                        if (tableName!.EndsWith(WiserTableNames.WiserItemFile, StringComparison.OrdinalIgnoreCase))
                        {
                            var prefix = tableName.Replace(WiserTableNames.WiserItemFile, "");
                            await clientDatabaseConnection.ExecuteAsync($@"INSERT INTO `{newCustomer.LiveDatabase.DatabaseName}`.`{tableName}` 
                                                                        SELECT file.* FROM `{currentCustomer.LiveDatabase.DatabaseName}`.`{tableName}` AS file
                                                                        JOIN `{currentCustomer.LiveDatabase.DatabaseName}`.`{prefix}{WiserTableNames.WiserItem}` AS item ON item.id = file.item_id AND item.entity_type NOT IN ({entityTypesString})");
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

                        await clientDatabaseConnection.ExecuteAsync($"INSERT INTO `{newCustomer.LiveDatabase.DatabaseName}`.`{tableName}` SELECT * FROM `{currentCustomer.LiveDatabase.DatabaseName}`.`{tableName}`");
                    }
                    catch (Exception exception)
                    {
                        throw new Exception($"Error while trying to fill table '{tableName}'", exception);
                    }
                }

                // Remove passwords from response.
                newCustomer.LiveDatabase.Password = null;

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
                    //await databaseHelpersService.DropDatabaseAsync(databaseName);
                }

                throw;
            }
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
            wiserDatabaseConnection.AddParameter("db_host", customer.LiveDatabase.Host);
            wiserDatabaseConnection.AddParameter("db_login", customer.LiveDatabase.Username);
            wiserDatabaseConnection.AddParameter("db_passencrypted", customer.LiveDatabase.Password ?? String.Empty);
            wiserDatabaseConnection.AddParameter("db_port", customer.LiveDatabase.PortNumber);
            wiserDatabaseConnection.AddParameter("db_dbname", customer.LiveDatabase.DatabaseName);
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
            var decryptedPassword = passwordIsEncrypted ? customer.LiveDatabase.Password.DecryptWithAesWithSalt(apiSettings.DatabasePasswordEncryptionKey) : customer.LiveDatabase.Password;
            return $"server={customer.LiveDatabase.Host};port={(customer.LiveDatabase.PortNumber > 0 ? customer.LiveDatabase.PortNumber : 3306)};uid={customer.LiveDatabase.Username};pwd={decryptedPassword};database={customer.LiveDatabase.DatabaseName};AllowUserVariables=True;ConvertZeroDateTime=true;CharSet=utf8";
        }

        #endregion
    }
}