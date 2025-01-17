using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OpenIddict.Server;

namespace Api.Core.Services;

public class OpenIddictAuthorizationRequestHandler : IOpenIddictServerHandler<OpenIddictServerEvents.HandleAuthorizationRequestContext>
{
    public ValueTask HandleAsync(OpenIddictServerEvents.HandleAuthorizationRequestContext context)
    {
        var authSource = context.Request["auth_source"]?.Value?.ToString();
        if (!String.IsNullOrEmpty(authSource) && authSource.Equals("google", StringComparison.OrdinalIgnoreCase))
        {
            var googleAuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";

            var queryParams = new QueryString()
                .Add("client_id", "your-google-client-id")
                .Add("redirect_uri", "https://your-app.com/auth/callback") // Google's redirect URI
                .Add("response_type", "code")
                .Add("scope", "openid email profile")
                .Add("state", context.Request.State);

            // Build the URL and send it as part of the response
            var redirectUri = googleAuthorizationEndpoint + queryParams;
            
            context.HandleRequest(); // Prevent further processing by OpenIddict
            return default;
        }

        context.SkipRequest(); // For local logins, proceed with the defa
        return default;
    }
}