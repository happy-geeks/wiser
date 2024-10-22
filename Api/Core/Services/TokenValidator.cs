using System.Threading.Tasks;
using OpenIddict.Server;

namespace Api.Core.Services;

public class TokenValidator : IOpenIddictServerHandler<OpenIddictServerEvents.ValidateTokenRequestContext>
{
    public ValueTask HandleAsync(OpenIddictServerEvents.ValidateTokenRequestContext context)
    {
        return ValueTask.CompletedTask;
    }
}