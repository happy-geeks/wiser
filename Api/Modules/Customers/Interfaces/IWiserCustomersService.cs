using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Customers.Enums;
using Api.Modules.Customers.Models;

namespace Api.Modules.Customers.Interfaces
{
    /// <summary>
    /// Interface for operations related to Wiser users (users that can log in to Wiser).
    /// </summary>
    public interface IWiserCustomersService
    {
        /// <summary>
        /// Get a single customer via <see cref="ClaimsIdentity"/>.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <param name="includeDatabaseInformation">Optional: Whether to also get the connection information for the database of the customer. Default is <see langword="false"/>.</param>
        /// <returns>The <see cref="CustomerModel"/>.</returns>
        Task<ServiceResult<CustomerModel>> GetSingleAsync(ClaimsIdentity identity, bool includeDatabaseInformation = false);
        
        /// <summary>
        /// Get a single customer via ID.
        /// </summary>
        /// <param name="id">The ID of the customer.</param>
        /// <param name="includeDatabaseInformation">Optional: Whether to also get the connection information for the database of the customer. Default is <see langword="false"/>.</param>
        /// <returns>The <see cref="CustomerModel"/>.</returns>
        Task<ServiceResult<CustomerModel>> GetSingleAsync(int id, bool includeDatabaseInformation = false);

        /// <summary>
        /// Get the encryption key for a customer via <see cref="ClaimsIdentity"/>.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user to check for rights.</param>
        /// <param name="forceLiveKey">Always give back the live encryption key, even on the test environment.</param>
        /// <returns>The encryption key as string.</returns>
        Task<ServiceResult<string>> GetEncryptionKey(ClaimsIdentity identity, bool forceLiveKey = false);
        
        /// <summary>
        /// Decrypts a value using the encryption key that is saved for the customer.
        /// This uses the encryption that adds a date, so that these encrypted values expire after a certain amount of time.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="encryptedValue">The input.</param>
        /// <param name="identity">The identity of the authenticated client. The customerId will be retrieved from this.</param>
        /// <returns>The decrypted value in the requested type.</returns>
        Task<T> DecryptValue<T>(string encryptedValue, ClaimsIdentity identity);

        /// <summary>
        /// Decrypts a value using the encryption key that is saved for the customer.
        /// This uses the encryption that adds a date, so that these encrypted values expire after a certain amount of time.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="encryptedValue">The input.</param>
        /// <param name="customer">The <see cref="CustomerModel"/>.</param>
        /// <returns>The decrypted value in the requested type.</returns>
        T DecryptValue<T>(string encryptedValue, CustomerModel customer);

        /// <summary>
        /// Encrypts a value using the encryption key that is saved for the customer.
        /// This uses the encryption that adds a date, so that these encrypted values expire after a certain amount of time.
        /// </summary>
        /// <param name="valueToEncrypt">The input.</param>
        /// <param name="identity">The identity of the authenticated client. The customerId will be retrieved from this.</param>
        /// <returns>The decrypted value in the requested type.</returns>
        Task<string> EncryptValue(object valueToEncrypt, ClaimsIdentity identity);

        /// <summary>
        /// Encrypts a value using the encryption key that is saved for the customer.
        /// This uses the encryption that adds a date, so that these encrypted values expire after a certain amount of time.
        /// </summary>
        /// <param name="valueToEncrypt">The input.</param>
        /// <param name="customer">The <see cref="CustomerModel"/>.</param>
        /// <returns>The decrypted value in the requested type.</returns>
        string EncryptValue(object valueToEncrypt, CustomerModel customer);

        /// <summary>
        /// Check if a customer already exists.
        /// </summary>
        /// <param name="name">The name of the customer.</param>
        /// <param name="subDomain">The sub domain of the customer.</param>
        /// <returns>A <see cref="CustomerExistsResults"/>.</returns>
        Task<ServiceResult<CustomerExistsResults>> CustomerExistsAsync(string name, string subDomain);

        /// <summary>
        /// Creates a new Wiser customer/tenant.
        /// </summary>
        /// <param name="customer">The data for the new customer.</param>
        /// <param name="isWebShop">Optional: Indicate whether or not this customer is getting a webshop. Default is <see langword="false"/>.</param>
        /// <param name="isConfigurator">Optional: Indicate whether or not this customer is getting a configurator. Default is <see langword="false"/>.</param>
        /// <param name="isMultiLanguage">Optional: Indicate whether or not the website is going to support multiple languages.</param>
        /// <returns>The newly created <see cref="CustomerModel"/>.</returns>
        Task<ServiceResult<CustomerModel>> CreateCustomerAsync(CustomerModel customer, bool isWebShop = false, bool isConfigurator = false, bool isMultiLanguage = false);

        /// <summary>
        /// Gets the title for the browser tab for a customer, based on sub domain.
        /// </summary>
        /// <param name="subDomain">The sub domain of the customer.</param>
        /// <returns>The title for the browser tab.</returns>
        Task<ServiceResult<string>> GetTitleAsync(string subDomain);
        
        /// <summary>
        /// Get whether or not a sub domain is empty or the sub domain of the main Wiser database.
        /// </summary>
        bool IsMainDatabase(ClaimsIdentity identity);
        
        /// <summary>
        /// Get whether or not a sub domain is empty or the sub domain of the main Wiser database.
        /// </summary>
        bool IsMainDatabase(string subDomain);

        /// <summary>
        /// Inserts or updates a customer in the database, based on <see cref="CustomerModel.Id"/>.
        /// </summary>
        /// <param name="customer">The customer to add or update.</param>
        Task CreateOrUpdateCustomerAsync(CustomerModel customer);

        /// <summary>
        /// Generates a connection string for a customer.
        /// </summary>
        /// <param name="customer">The customer.</param>
        /// <param name="passwordIsEncrypted">Whether the password is saved encrypted in the <see cref="CustomerModel"/>.</param>
        string GenerateConnectionStringFromCustomer(CustomerModel customer, bool passwordIsEncrypted = true);
    }
}