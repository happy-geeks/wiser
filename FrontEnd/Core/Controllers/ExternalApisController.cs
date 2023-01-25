using System.Threading.Tasks;
using FrontEnd.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Core.Controllers;

/// <summary>
/// Controller for making calls to external APIs, this is mostly meant as a proxy to prevent CORS errors if we were to make the calls directly from javascript.
/// </summary>
public class ExternalApisController : Controller
{
    private readonly IExternalApisService externalApisService;

    /// <summary>
    /// Creates a new instance of <see cref="ExternalApisController"/>.
    /// </summary>
    public ExternalApisController(IExternalApisService externalApisService)
    {
        this.externalApisService = externalApisService;
    }

    /// <summary>
    /// Pass the current request to an external API, based on some custom headers.
    /// </summary>
    /// <returns>The response of the external API.</returns>
    [HttpGet, HttpPost, HttpPut, HttpDelete, HttpPatch]
    public async Task<IActionResult> ProxyAsync()
    {
        return await externalApisService.ProxyAsync();
    }
}