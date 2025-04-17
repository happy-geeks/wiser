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
    /// <param name="wiserId"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("api/v3/[controller]/get/{wiserId:int:required}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(ulong wiserId)
    {
        return (await productsService.GetProduct((ClaimsIdentity) User.Identity, wiserId)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Get all products api result based on the date, if no date is given we just list todays changes., this function only retrieves the product api result, it does not generate it.
    /// </summary>
    /// <param name="date">Since this date we list the changes.</param>
    /// <param name="page">The page offset.</param>
    /// <returns></returns>
    [HttpGet]
    [Route("api/v3/[controller]/getAll")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll([FromQuery] string date = "",[FromQuery] int page = 0)
    {
        var callingUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path}";
        
        return (await productsService.GetAllProducts((ClaimsIdentity) User.Identity, callingUrl, date, page)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Refresh the product api result, this function updates the product api result but only 1.
    /// </summary>
    /// <param name="wiserId">the id of the wiser product we are trying to read.</param>
    /// <returns></returns>
    [HttpPost]
    [Route("api/v3/[controller]/refresh/{wiserId:int:required}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Refresh(ulong wiserId)
    {
        return (await productsService.RefreshProductAsync((ClaimsIdentity) User.Identity, wiserId)).GetHttpResponseMessage();
    }
    
    /// <summary>
    /// Refreshes all the product api results, this function updates the product api result with a max of 256 each time its called.
    /// </summary>
    [HttpPost]
    [Route("api/v3/[controller]/refreshAll")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RefreshAll()
    {
        return (await productsService.RefreshProductsAlAsync((ClaimsIdentity) User.Identity)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Overwrite that overwrites api product settings for all products.
    /// </summary>
    /// <param name="identity"></param>
    [HttpPost]
    [Route("api/v3/[controller]/OverwriteApiProductSettings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> OverwriteSettingsForAllProducts()
    {
        return (await productsService.OverwriteApiProductSettingsForAllProductAsync((ClaimsIdentity) User.Identity)).GetHttpResponseMessage();
    }
}