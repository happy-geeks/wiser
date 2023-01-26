using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Core.Interfaces;

/// <summary>
/// Service for making calls to external APIs, this is mostly meant as a proxy to prevent CORS errors if we were to make the calls directly from javascript.
/// </summary>
public interface IExternalApisService
{
    /// <summary>
    /// Pass the current request to an external API, based on some custom headers.
    /// </summary>
    /// <returns>The response of the external API.</returns>
    Task<ContentResult> ProxyAsync();
}