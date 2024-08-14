using System;
using System.Collections.Generic;
using System.Linq;

namespace Api.Core.Helpers;

/// <summary>
/// Helper functions for the HttpContext.
/// </summary>
public class HttpContextHelpers
{
    /// <summary>
    /// Get the subdomain from the request URL.
    /// </summary>
    /// <param name="requestUrl">The <see cref="Uri"/> of the URL to get the sub domain from.</param>
    /// <param name="wiserHostNames">A list with the host names that Wiser can use.</param>
    /// <param name="defaultValue">The default value to use when subdomain could not be found in the Uri.</param>
    /// <returns>The subdomain.</returns>
    public static string GetSubdomainFromUrl(Uri requestUrl, List<string> wiserHostNames, string defaultValue)
    {
        var result = "";
        if (requestUrl.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            // E.g.: tenantname.localhost
            var lastDotIndex = requestUrl.Host.LastIndexOf('.');
            result = requestUrl.Host[..lastDotIndex];
        }
        else if (requestUrl.Host.Contains('.'))
        {
            result = wiserHostNames.Where(host => !String.IsNullOrWhiteSpace(host)).Aggregate(requestUrl.Host, (current, host) => current.Replace(host, ""));
        }
        else if (requestUrl.Port != 443 && requestUrl.Port != 80 && !requestUrl.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            result = requestUrl.Host;
        }

        if (String.IsNullOrWhiteSpace(result))
        {
            result = defaultValue;
        }

        return result;
    }
}