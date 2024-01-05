using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Cache.Interfaces;
using Api.Modules.Cache.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using RestSharp;

namespace Api.Modules.Cache.Services;

/// <inheritdoc cref="IExternalCacheService" />
public class ExternalCacheService : IExternalCacheService, IScopedService
{
    /// <inheritdoc />
    public async Task<ServiceResult<bool>> ClearCacheAsync(ClearCacheSettingsModel settings)
    {
        // Check if we have all the settings we need.
        if (String.IsNullOrWhiteSpace(settings?.Url) || !settings.Areas.Any())
        {
            return new ServiceResult<bool>
            {
                StatusCode = HttpStatusCode.BadRequest,
                ErrorMessage = "No or invalid settings found."
            };
        }

        // Add https if the user didn't add it.
        if (!settings.Url.StartsWith("http"))
        {
            settings.Url = $"{(settings.Url.StartsWith("//") ? "https:" : "https://")}{settings.Url}";
        }

        // Check if it's a valid URI.
        if (!Uri.IsWellFormedUriString(settings.Url, UriKind.Absolute))
        {
            return new ServiceResult<bool>
            {
                StatusCode = HttpStatusCode.BadRequest,
                ErrorMessage = "Invalid URL given."
            };
        }

        var client = new RestClient();
        var uriBuilder = new UriBuilder(settings.Url);

        // If the area "all" is in the list, then just call "clearallcache.gcl" on the website.
        if (settings.Areas.Any(x => String.Equals(x, "all", StringComparison.OrdinalIgnoreCase)))
        {
            uriBuilder.Path = "clearallcache.gcl";
            var request = new RestRequest(uriBuilder.Uri);
            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful)
            {
                // if gcl cache clear wasn't successful
                // try to clear jcl cache
                return await ClearJCLCacheAsync(client, uriBuilder);
            }

            return new ServiceResult<bool>(true)
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }

        // Clear specific cache areas.
        foreach (var area in settings.Areas)
        {
            if (String.Equals(area, "content", StringComparison.OrdinalIgnoreCase) || String.Equals(area, "files"))
            {
                uriBuilder.Path = $"clear{area}cache.gcl";
            }
            else if (Enum.TryParse(typeof(CacheAreas), area, true, out var parsedValue))
            {
                uriBuilder.Path = $"clear{parsedValue}cache.gcl";
            }
            else
            {
                continue;
            }
            
            var request = new RestRequest(uriBuilder.Uri);
            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful)
            {
                // if gcl cache clear wasn't successful
                // try to clear jcl cache
                return await ClearJCLCacheAsync(client, uriBuilder);
            }
        }

        return new ServiceResult<bool>(true)
        {
            StatusCode = HttpStatusCode.NoContent
        };
    }

    private async Task<ServiceResult<bool>> ClearJCLCacheAsync(RestClient client, UriBuilder uriBuilder)
    {
        uriBuilder.Path = "clearallcache.jcl";
        
        var request = new RestRequest(uriBuilder.Uri);
        var response = await client.ExecuteAsync(request);
        
        if (!response.IsSuccessful)
        {
            return new ServiceResult<bool>
            {
                StatusCode = HttpStatusCode.BadRequest,
                ErrorMessage = $"De website gaf een HTTP {(int)response.StatusCode} fout. Heeft u de juiste URL ingevuld?"
            };
        }
        
        return new ServiceResult<bool>(true)
        {
            StatusCode = HttpStatusCode.NoContent
        };
    }
}