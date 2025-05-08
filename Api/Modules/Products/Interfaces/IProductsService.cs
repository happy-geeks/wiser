using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Products.Interfaces;

/// <summary>
/// Service for handling the product api related calls.
/// </summary>
public interface IProductsService
{
    /// <summary>
    /// Get Product api result, this function only retrieves the product api result, it does not generate it.
    /// </summary>
    /// <param name="identity">The identity of the user performing this command.</param>
    /// <param name="wiserId">The id of the wiser product we are trying to read.</param>
    /// <returns>The resulting api output or throws an error if not found.</returns>
    public Task<ServiceResult<JToken>> GetProductAsync(ClaimsIdentity identity, ulong wiserId);

    /// <summary>
    /// Gets a Json formatted list of all the products apis in the database that have a product api result generated.
    /// </summary>
    /// <param name="identity">The identity of the user performing this command.</param>
    /// <param name="date">The date for the last changed date. if not provided today will be used.</param>
    /// <param name="page">The page offset for the product result, if not provided page zero will be returned.</param>
    /// <returns>A json formatted list of all the products with pagination.</returns>
    public Task<ServiceResult<JToken>> GetAllProductsAsync(ClaimsIdentity identity, DateTime? date, int page = 0);

    /// <summary>
    /// This function will call the RefreshProductsAsync function for the given ids.
    /// </summary>
    /// <param name="identity">The identity of the user performing this command.</param>
    /// <param name="wiserId">The id of the wiser product we are trying to read.</param>
    /// <param name="ignoreCooldown">Ignore the cooldown check when refreshing.</param>
    /// <returns>Status 200(ok) or an exception if occured.</returns>
    public Task<ServiceResult<JToken>> RefreshProductsAsync(ClaimsIdentity identity, ICollection<ulong> wiserId, bool ignoreCooldown = false);

    /// <summary>
    /// Function used to Refresh products, this will run the query, styled output or static output and hash it.
    /// If the hash is out of date a new version will be created.
    /// </summary>
    /// <param name="identity">The identity of the user performing this command.</param>
    /// <param name="wiserId">The id of the wiser product we are trying to read.</param>
    /// <param name="ignoreCooldown">Ignore the cooldown check when refreshing.</param>
    /// <returns>Status 200(ok) or an exception if occured.</returns>
    public Task<ServiceResult<JToken>> RefreshProductAsync(ClaimsIdentity identity, ulong wiserId, bool ignoreCooldown = false);

    /// <summary>
    /// This function will find 256 products and call the RefreshProductsAsync function on it based on cooldown time and last refresh time.
    /// </summary>
    /// <param name="identity">The identity of the user performing this command.</param>
    /// <param name="ignoreCooldown">Ignore the cooldown check when refreshing.</param>
    /// <returns>Status 200(ok) or an exception if occured.</returns>
    public Task<ServiceResult<JToken>> RefreshProductsAllAsync(ClaimsIdentity identity, bool ignoreCooldown = false);

    /// <summary>
    /// This function is used to overwrite the default settings on each product, this is needed when a new set of default settings is setup and needs to be applied to existing products.
    /// Note: this is a destructive function, it will overwrite all product api settings with the new settings regardless of what matches the current settings or not.
    /// </summary>
    /// <param name="identity">The identity of the user performing this command.</param>
    /// <returns>Status 200(ok) or an exception if occured.</returns>
    public Task<ServiceResult<JToken>> SetDefaultSettingsOnAllProductsAsync(ClaimsIdentity identity);
}