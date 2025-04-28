using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Products.Interfaces;

/// <summary>
/// 
/// </summary>
public interface IProductsService
{
    /// <summary>
    /// Get Product api result, this function only retrieves the product api result, it does not generate it.
    /// </summary>
    /// <param name="identity">the identity of the user performing this command</param>
    /// <param name="wiserId">the id of the wiser product we are trying to read</param>
    /// <returns> The resulting api output or throws an error if not found.</returns>
    public Task<ServiceResult<JToken>> GetProduct(ClaimsIdentity identity, ulong wiserId);

    /// <summary>
    /// Gets a Json formatted list of all the products api's in the database that have a product api result generated.
    /// </summary>
    /// <param name="identity"></param>
    /// <param name="callingUrl"></param>
    /// <param name="date"></param>
    /// <param name="page"></param>
    /// <returns>a json formatted list of all the products with pagination</returns>
    public Task<ServiceResult<JToken>> GetAllProducts(ClaimsIdentity identity, string callingUrl, DateTime? date, int page = 0);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="identity"></param>
    /// <param name="wiserId"></param>
    /// <param name="ignoreCooldown"></param>
    /// <returns></returns>
    public Task<ServiceResult<JToken>> RefreshProductsAsync(ClaimsIdentity identity, ICollection<ulong> wiserId, bool ignoreCooldown = false);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="identity"></param>
    /// <param name="wiserId"></param>
    /// <param name="ignoreCooldown"></param>
    /// <returns></returns>
    public Task<ServiceResult<JToken>> RefreshProductAsync(ClaimsIdentity identity, ulong wiserId, bool ignoreCooldown = false);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="identity"></param>
    /// <param name="ignoreCooldown"></param>
    /// <returns></returns>
    public Task<ServiceResult<JToken>> RefreshProductsAlAsync(ClaimsIdentity identity, bool ignoreCooldown = false);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="identity"></param>
    /// <returns></returns>
    public Task<ServiceResult<JToken>> OverwriteApiProductSettingsForAllProductAsync(ClaimsIdentity identity);
}