using System.Net.Mime;
using System.Threading.Tasks;
using Api.Modules.Cache.Interfaces;
using Api.Modules.Cache.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Cache.Controllers;

/// <summary>
/// Controller for manipulating the cache of an GCL website.
/// </summary>
[Route("api/v3/external-cache")]
[ApiController]
[Authorize]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class ExternalCacheController : Controller
{
    private readonly IExternalCacheService externalCacheService;

    /// <summary>
    /// Creates a new instance of <see cref="ExternalCacheController"/>.
    /// </summary>
    public ExternalCacheController(IExternalCacheService externalCacheService)
    {
        this.externalCacheService = externalCacheService;
    }
    
    /// <summary>
    /// Clears the cache of a GCL website. You can chose which cache area(s) to clear.
    /// </summary>
    /// <param name="settings">The settings for clearing the cache.</param>
    [HttpPost]
    [Route("clear")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearCacheAsync(ClearCacheSettingsModel settings)
    {
        return (await externalCacheService.ClearCacheAsync(settings)).GetHttpResponseMessage();
    }
}