using System.Threading.Tasks;
using OpenIddict.Server;

namespace Api.Core.Services;

public class AuthorizationValidator : IOpenIddictServerHandler<OpenIddictServerEvents.ValidateAuthorizationRequestContext>
{
    public ValueTask HandleAsync(OpenIddictServerEvents.ValidateAuthorizationRequestContext context)
    {
        return ValueTask.CompletedTask;
    }
}