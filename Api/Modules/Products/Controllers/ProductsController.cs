using System;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Modules.Products.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Products.Controllers;

/// <summary>
/// The products API controller.
/// </summary>
[ApiController]
[Authorize]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
[Route("api/v3/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductsService productsService;

    /// <summary>
    /// Creates a new instance of <see cref="ProductsController"/>.
    /// </summary>
    public ProductsController(IProductsService productsService)
    {
        this.productsService = productsService;
    }

    /// <summary>
    /// Get Product api result, this function only retrieves the product api result, it does not generate it.
    /// </summary>
    /// <param name="wiserId">Optional: If given an id it will return that entry for that product, regardless of given date or page offset.</param>
    /// <param name="date">Optional: Since this date we list the changes.</param>
    /// <param name="page">Optional: The page offset.</param>
    /// <returns></returns>
    [HttpGet]
    [Route("{wiserId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(ulong wiserId, [FromQuery] DateTime? date = null, [FromQuery] int page = 0)
    {
        if (wiserId == 0)
        {
            return (await productsService.GetAllProductsAsync((ClaimsIdentity) User.Identity, date, page)).GetHttpResponseMessage();
        }
        return (await productsService.GetProductAsync((ClaimsIdentity) User.Identity, wiserId)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Get all products api result based on the date, if no date is given we just the changes of today., this function only retrieves the product api result, it does not generate it.
    /// </summary>

    /// <summary>
    /// Refresh the product api result, this function updates the product api result but only 1.
    /// </summary>
    /// <param name="wiserId">the id of the wiser product we are trying to read.</param>
    /// <returns></returns>
    [HttpPost]
    [Route("refresh/{wiserId:int:required}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RefreshAsync(ulong wiserId)
    {
        return (await productsService.RefreshProductAsync((ClaimsIdentity) User.Identity, wiserId)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Refreshes the product api results, function will refresh the first 256 products that are not up to date based on the cooldown time and last refresh time.
    /// To update all products call this multiple times.
    /// The intended use for this function is to be called by a cron job at intervals so we throttle the amount of products we refresh at once.
    /// </summary>
    [HttpPost]
    [Route("refresh-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RefreshAllAsync()
    {
        return (await productsService.RefreshProductsAllAsync((ClaimsIdentity) User.Identity)).GetHttpResponseMessage();
    }

    /// <summary>
    /// This function is used to overwrite the default settings on each product, this is needed when a new set of default settings is setup and needs to be applied to existing products.
    /// Note: this is a destructive function, it will overwrite all product api settings with the new settings regardless of what matches the current settings or not.
    /// </summary>
    [HttpPost]
    [Route("set-default-settings-on-all-products")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefaultSettingsOnAllProductsAsyncSetDefaultSettingsOnAllProductsAsync()
    {
        return (await productsService.SetDefaultSettingsOnAllProductsAsync((ClaimsIdentity) User.Identity)).GetHttpResponseMessage();
    }
}