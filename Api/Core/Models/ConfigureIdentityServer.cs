using System.Collections.Generic;
using IdentityServer4;
using IdentityServer4.Models;

namespace Api.Core.Models
{
    public class ConfigureIdentityServer
    {
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email()
            };
        }

        public static IEnumerable<ApiResource> GetApiResources(string clientSecret)
        {
            return new List<ApiResource>
            {
                new("wiser-api", "Wiser API", new List<string> { "role", "admin", "user" })
                {
                    ApiSecrets =
                    {
                        new Secret(clientSecret.Sha256())
                    },
                    Scopes =
                    {
                        "wiser-api",
                    }
                }
            };
        }

        public static IEnumerable<ApiScope> GetApiScopes()
        {
            return new List<ApiScope>
            {
                new()
                {
                    Name = "wiser-api",
                    DisplayName = "Scope for the Wiser API",
                    UserClaims = { "role", "admin", "user" }
                }
            };
        }

        // clients want to access resources (aka scopes)
        public static IEnumerable<Client> GetClients(string clientSecret)
        {
            return new List<Client>
            {
                new()
                {
                    ClientId = "wiser",

                    AllowedGrantTypes = GrantTypes.ResourceOwnerPasswordAndClientCredentials,
                    AccessTokenType = AccessTokenType.Jwt,
                    AccessTokenLifetime = 3600,
                    IdentityTokenLifetime = 3600,
                    UpdateAccessTokenClaimsOnRefresh = true,
                    AllowOfflineAccess = true,
                    RefreshTokenExpiration = TokenExpiration.Absolute,
                    RefreshTokenUsage = TokenUsage.OneTimeOnly,
                    AlwaysSendClientClaims = true,
                    Enabled = true,
                    ClientSecrets = new List<Secret> { new(clientSecret.Sha256()) },
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        IdentityServerConstants.StandardScopes.OfflineAccess
                    }
                }
            };
        }
    }
}