using System;
using System.Data;
using System.Net;
using System.Security.Claims;
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
        private readonly GclSettings gclSettings;
        private readonly ApiSettings apiSettings;
        private readonly IDatabaseConnection wiserDatabaseConnection;

        #endregion

        /// <summary>
        /// Creates a new instance of WiserCustomersService.
        /// </summary>
        public WiserCustomersService(IDatabaseConnection connection, IOptions<ApiSettings> apiSettings, IOptions<GclSettings> gclSettings)
        {
            clientDatabaseConnection = connection;
            this.gclSettings = gclSettings.Value;
            this.apiSettings = apiSettings.Value;

            if (clientDatabaseConnection is ClientDatabaseConnection databaseConnection)
            {
                wiserDatabaseConnection = databaseConnection.WiserDatabaseConnection;
            }
        }

        /// <inheritdoc />
        public async Task<ServiceResult<CustomerModel>> GetSingleAsync(ClaimsIdentity identity)
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
                            subdomain
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
        public async Task<ServiceResult<string>> GetEncryptionKey(ClaimsIdentity identity, bool forceLiveKey = false)
        {
            var subDomain = IdentityHelpers.GetSubDomain(identity);
            if (IsMainDatabase(subDomain))
            {
                return new ServiceResult<string>(String.IsNullOrWhiteSpace(gclSettings.ExpiringEncryptionKey) ? gclSettings.DefaultEncryptionKey : gclSettings.ExpiringEncryptionKey);
            }
            
            // Get the customer data.
            wiserDatabaseConnection.ClearParameters();
            wiserDatabaseConnection.AddParameter("name", subDomain);

            var query = $@"SELECT {(IdentityHelpers.IsTestEnvironment(identity) && !forceLiveKey ? "encryption_key_test" : "encryption_key")} AS encryption_key
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
            // TODO: How are we supposed to do this with dependency injection?
            await using (var mysqlConnection = new MySqlConnection($"server={customer.LiveDatabase.Host};port={customer.LiveDatabase.PortNumber};uid={customer.LiveDatabase.Username};pwd={customer.LiveDatabase.Password};database={customer.LiveDatabase.DatabaseName};AllowUserVariables=True;ConvertZeroDateTime=true;CharSet=utf8"))
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

                    await CreateOrUpdateCustomerAsync(customer, SecurityHelpers.GenerateRandomPassword(20));

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
                    await transaction?.RollbackAsync();

                    throw;
                }
                finally
                {
                    transaction?.Dispose();
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

        #endregion

        #region Private functions

        /// <summary>
        /// Inserts or updates a customer in the database, based on <see cref="CustomerModel.Id"/>.
        /// </summary>
        /// <param name="customer">The customer to add or update.</param>
        /// <param name="encryptionKeyTest">Encryption key for test environment.</param>
        private async Task CreateOrUpdateCustomerAsync(CustomerModel customer, string encryptionKeyTest)
        {
            // Note: Passwords should be encrypted by Wiser before sending them to the API.
            wiserDatabaseConnection.ClearParameters();
            wiserDatabaseConnection.AddParameter("name", customer.Name);
            wiserDatabaseConnection.AddParameter("db_host", customer.LiveDatabase.Host);
            wiserDatabaseConnection.AddParameter("db_login", customer.LiveDatabase.Username);
            wiserDatabaseConnection.AddParameter("db_passencrypted", customer.LiveDatabase?.Password ?? String.Empty);
            wiserDatabaseConnection.AddParameter("db_port", customer.LiveDatabase.PortNumber);
            wiserDatabaseConnection.AddParameter("db_dbname", customer.LiveDatabase.DatabaseName);
            wiserDatabaseConnection.AddParameter("encryption_key", customer.EncryptionKey);
            wiserDatabaseConnection.AddParameter("encryption_key_test", encryptionKeyTest);
            wiserDatabaseConnection.AddParameter("subdomain", customer.SubDomain);

            // Set the ID
            var customerId = await wiserDatabaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(ApiTableNames.WiserCustomers, customer.Id);
            customer.Id = customerId;
        }
        
        #endregion
    }
}