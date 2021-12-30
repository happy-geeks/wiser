using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
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
        private readonly ApiSettings apiSettings;
        private readonly IDatabaseConnection wiserDatabaseConnection;

        #endregion

        /// <summary>
        /// Creates a new instance of WiserCustomersService.
        /// </summary>
        public WiserCustomersService(IDatabaseConnection connection, IOptions<ApiSettings> apiSettings)
        {
            clientDatabaseConnection = connection;
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
            if (String.IsNullOrWhiteSpace(subDomain))
            {
                throw new Exception("No sub domain found in identity!");
            }

            // Get the customer data.
            wiserDatabaseConnection.ClearParameters();
            wiserDatabaseConnection.AddParameter("name", subDomain);

            var query = $@"SELECT
                            id,
                            customerid,
                            name,
                            propertys,
                            db_host,
                            db_login,
                            db_passencrypted,
                            db_port,
                            db_dbname,
                            db_host_test,
                            db_login_test,
                            db_passencrypted_test,
                            db_port_test,
                            db_dbname_test,
                            ftp_host,
                            ftp_user,
                            ftp_passencrypted,
                            ftp_root,
                            emailadres,
                            startdatum,
                            enddate,
                            notes,
                            google_auth,
                            backup_ordernr,
                            webmanagerManualURL,
                            data_url,
                            {(IdentityHelpers.IsTestEnvironment(identity) ? "encryption_key_test" : "encryption_key")} AS encryption_key,
                            data_url_test,
                            mailhost,
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
        public async Task<ServiceResult<string>> GetEncryptionKey(ClaimsIdentity identity)
        {
            var subDomain = IdentityHelpers.GetSubDomain(identity);
            if (String.IsNullOrWhiteSpace(subDomain))
            {
                throw new Exception("No sub domain found in identity!");
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
        public async Task<ServiceResult<CustomerModel>> CreateCustomerAsync(CustomerModel customer, bool isWebShop = false)
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
                        customer.LiveDatabase.Password = customer.LiveDatabase.Password.EncryptWithAes(apiSettings.DatabasePasswordEncryptionKey);
                    }

                    if (!String.IsNullOrWhiteSpace(customer.TestDatabase?.Password))
                    {
                        customer.TestDatabase.Password = customer.TestDatabase.Password.EncryptWithAes(apiSettings.DatabasePasswordEncryptionKey);
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

                    if (customer.Ftp != null)
                    {
                        customer.Ftp.Password = null;
                    }

                    if (customer.TestDatabase != null)
                    {
                        customer.TestDatabase.Password = null;
                    }

                    var createTablesQuery = ReadTextResourceFromAssembly("CreateTables");
                    var createTriggersQuery = ReadTextResourceFromAssembly("CreateTriggers");
                    var insertInitialDataQuery = ReadTextResourceFromAssembly("InsertInitialData");
                    var insertInitialDataEcommerceQuery = !isWebShop ? "" : ReadTextResourceFromAssembly("InsertInitialDataEcommerce");

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
            wiserDatabaseConnection.AddParameter("name", customer.Name);
            wiserDatabaseConnection.AddParameter("propertys", String.Join(Environment.NewLine, customer.Properties.Select(x => $"{x.Key}={x.Value}")));
            wiserDatabaseConnection.AddParameter("db_host", customer.LiveDatabase.Host);
            wiserDatabaseConnection.AddParameter("db_login", customer.LiveDatabase.Username);
            wiserDatabaseConnection.AddParameter("db_passencrypted", customer.LiveDatabase?.Password ?? String.Empty);
            wiserDatabaseConnection.AddParameter("db_port", customer.LiveDatabase.PortNumber);
            wiserDatabaseConnection.AddParameter("db_dbname", customer.LiveDatabase.DatabaseName);
            wiserDatabaseConnection.AddParameter("db_connectionmethod", "direct");
            wiserDatabaseConnection.AddParameter("startdatum", customer.StartDate);
            wiserDatabaseConnection.AddParameter("ftp_host", customer.Ftp?.Host);
            wiserDatabaseConnection.AddParameter("ftp_user", customer.Ftp?.Username);
            wiserDatabaseConnection.AddParameter("ftp_passencrypted", customer.Ftp?.Password ?? String.Empty);
            wiserDatabaseConnection.AddParameter("ftp_root", customer.Ftp?.RootFolder);
            wiserDatabaseConnection.AddParameter("emailadres", customer.EmailAddress);
            wiserDatabaseConnection.AddParameter("webmanagerManualURL", customer.InstructionsUrl);
            wiserDatabaseConnection.AddParameter("enddate", customer.EndDate);
            wiserDatabaseConnection.AddParameter("notes", customer.Notes);
            wiserDatabaseConnection.AddParameter("google_auth", customer.GoogleAuthenticationEnabled);
            wiserDatabaseConnection.AddParameter("backup_ordernr", customer.BackupOrderNumber);
            wiserDatabaseConnection.AddParameter("db_host_test", customer.TestDatabase?.Host);
            wiserDatabaseConnection.AddParameter("db_login_test", customer.TestDatabase?.Username);
            wiserDatabaseConnection.AddParameter("db_passencrypted_test", customer.TestDatabase?.Password ?? String.Empty);
            wiserDatabaseConnection.AddParameter("db_port_test", customer.TestDatabase?.PortNumber ?? 3306);
            wiserDatabaseConnection.AddParameter("db_connectionmethod_test", "direct");
            wiserDatabaseConnection.AddParameter("db_dbname_test", customer.TestDatabase?.DatabaseName);
            
            wiserDatabaseConnection.AddParameter("encryption_key", customer.EncryptionKey);
            wiserDatabaseConnection.AddParameter("subdomain", customer.SubDomain);

            // Set the ID
            var customerId = await wiserDatabaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(ApiTableNames.WiserCustomers, customer.Id);
            customer.Id = customerId;
        }

        /// <summary>
        /// Get a query from an SQL file (embedded resource).
        /// </summary>
        /// <param name="name">The name of the SQL file (without extension).</param>
        /// <returns>The contents of the SQL file.</returns>
        private static string ReadTextResourceFromAssembly(string name)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Api.Core.Queries.WiserInstallation.{name}.sql");
            if (stream == null)
            {
                return "";
            }

            using var streamReader = new StreamReader(stream);
            return streamReader.ReadToEnd();
        }
        
        #endregion
    }
}