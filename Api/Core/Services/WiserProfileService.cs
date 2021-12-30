using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Services;

namespace Api.Core.Services
{
    /// <inheritdoc />
    public class WiserProfileService : IProfileService
    {
        /// <inheritdoc />
        public Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            context.IssuedClaims = context.Subject.Claims.ToList();

            return Task.FromResult(0);
        }
        
        /// <inheritdoc />
        public Task IsActiveAsync(IsActiveContext context)
        {
            context.IsActive = true;
            return Task.FromResult(0);
        }
    }
}