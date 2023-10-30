using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Tenants.Enums;
using Api.Modules.Tenants.Models;

namespace Api.Modules.Tenants.Interfaces
{
    /// <summary>
    /// Interface for operations related to Wiser users (users that can log in to Wiser).
    /// </summary>
    public interface IWiserTenantsService
    {
        /// <summary>
        /// Get a single tenant via <see cref="ClaimsIdentity"/>.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <param name="includeDatabaseInformation">Optional: Whether to also get the connection information for the database of the tenant. Default is <see langword="false"/>.</param>
        /// <returns>The <see cref="TenantModel"/>.</returns>
        Task<ServiceResult<TenantModel>> GetSingleAsync(ClaimsIdentity identity, bool includeDatabaseInformation = false);
        
        /// <summary>
        /// Get a single tenant via ID.
        /// </summary>
        /// <param name="id">The ID of the tenant.</param>
        /// <param name="includeDatabaseInformation">Optional: Whether to also get the connection information for the database of the tenant. Default is <see langword="false"/>.</param>
        /// <returns>The <see cref="TenantModel"/>.</returns>
        Task<ServiceResult<TenantModel>> GetSingleAsync(int id, bool includeDatabaseInformation = false);

        /// <summary>
        /// Get the encryption key for a tenant via <see cref="ClaimsIdentity"/>.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user to check for rights.</param>
        /// <param name="forceLiveKey">Always give back the live encryption key, even on the test environment.</param>
        /// <returns>The encryption key as string.</returns>
        Task<ServiceResult<string>> GetEncryptionKey(ClaimsIdentity identity, bool forceLiveKey = false);
        
        /// <summary>
        /// Decrypts a value using the encryption key that is saved for the tenant.
        /// This uses the encryption that adds a date, so that these encrypted values expire after a certain amount of time.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="encryptedValue">The input.</param>
        /// <param name="identity">The identity of the authenticated client. The tenantId will be retrieved from this.</param>
        /// <returns>The decrypted value in the requested type.</returns>
        Task<T> DecryptValue<T>(string encryptedValue, ClaimsIdentity identity);

        /// <summary>
        /// Decrypts a value using the encryption key that is saved for the tenant.
        /// This uses the encryption that adds a date, so that these encrypted values expire after a certain amount of time.
        /// </summary>
        /// <typeparam name="T">The return type.</typeparam>
        /// <param name="encryptedValue">The input.</param>
        /// <param name="tenant">The <see cref="TenantModel"/>.</param>
        /// <returns>The decrypted value in the requested type.</returns>
        T DecryptValue<T>(string encryptedValue, TenantModel tenant);

        /// <summary>
        /// Encrypts a value using the encryption key that is saved for the tenant.
        /// This uses the encryption that adds a date, so that these encrypted values expire after a certain amount of time.
        /// </summary>
        /// <param name="valueToEncrypt">The input.</param>
        /// <param name="identity">The identity of the authenticated client. The tenantId will be retrieved from this.</param>
        /// <returns>The decrypted value in the requested type.</returns>
        Task<string> EncryptValue(object valueToEncrypt, ClaimsIdentity identity);

        /// <summary>
        /// Encrypts a value using the encryption key that is saved for the tenant.
        /// This uses the encryption that adds a date, so that these encrypted values expire after a certain amount of time.
        /// </summary>
        /// <param name="valueToEncrypt">The input.</param>
        /// <param name="tenant">The <see cref="TenantModel"/>.</param>
        /// <returns>The decrypted value in the requested type.</returns>
        string EncryptValue(object valueToEncrypt, TenantModel tenant);

        /// <summary>
        /// Check if a tenant already exists.
        /// </summary>
        /// <param name="name">The name of the tenant.</param>
        /// <param name="subDomain">The sub domain of the tenant.</param>
        /// <returns>A <see cref="TenantExistsResults"/>.</returns>
        Task<ServiceResult<TenantExistsResults>> TenantExistsAsync(string name, string subDomain);

        /// <summary>
        /// Creates a new Wiser tenant/tenant.
        /// </summary>
        /// <param name="tenant">The data for the new tenant.</param>
        /// <param name="isWebShop">Optional: Indicate whether or not this tenant is getting a webshop. Default is <see langword="false"/>.</param>
        /// <param name="isConfigurator">Optional: Indicate whether or not this tenant is getting a configurator. Default is <see langword="false"/>.</param>
        /// <param name="isMultiLanguage">Optional: Indicate whether or not the website is going to support multiple languages.</param>
        /// <returns>The newly created <see cref="TenantModel"/>.</returns>
        Task<ServiceResult<TenantModel>> CreateTenantAsync(TenantModel tenant, bool isWebShop = false, bool isConfigurator = false, bool isMultiLanguage = false);

        /// <summary>
        /// Gets the title for the browser tab for a tenant, based on sub domain.
        /// </summary>
        /// <param name="subDomain">The sub domain of the tenant.</param>
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
        /// Inserts or updates a tenant in the database, based on <see cref="TenantModel.Id"/>.
        /// </summary>
        /// <param name="tenant">The tenant to add or update.</param>
        Task CreateOrUpdateTenantAsync(TenantModel tenant);

        /// <summary>
        /// Generates a connection string for a tenant.
        /// </summary>
        /// <param name="tenant">The tenant.</param>
        /// <param name="passwordIsEncrypted">Whether the password is saved encrypted in the <see cref="TenantModel"/>.</param>
        string GenerateConnectionStringFromTenant(TenantModel tenant, bool passwordIsEncrypted = true);
    }
}