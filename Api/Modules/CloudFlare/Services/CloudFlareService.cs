using Api.Modules.CloudFlare.Models;
using Api.Modules.CloudFlare.Interfaces;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Api.Modules.CloudFlare.Services
{
    /// <inheritdoc cref="ICloudFlareService" />
    public class CloudFlareService : ICloudFlareService, IScopedService
    {
        private const string ApiPath = "https://api.cloudflare.com/client/v4/accounts";
        private static readonly HttpClient client = new HttpClient();
        private readonly IObjectsService objectsService;

        /// <summary>
        /// Creates an instance of <see cref="CloudFlareService"/>
        /// </summary>
        public CloudFlareService(IObjectsService objectsService)
        {
            this.objectsService = objectsService;
        }
        /// <summary>
        /// Get the proper settings for CloudFlare communication
        /// </summary>
        /// <returns>A <see cref="CloudFlareSettingsModel"/> with System Object Values</returns>
        private async Task<CloudFlareSettingsModel> GetCloudFlareSettings()
        {
            var authorizationKey = await objectsService.GetSystemObjectValueAsync("CLOUDFLARE_AuthorizationKey");
            if (String.IsNullOrEmpty(authorizationKey))
            {
                return null;
            }
            return new CloudFlareSettingsModel
            {
                AuthorizationKey = authorizationKey,
                AuthorizationEmail = await objectsService.GetSystemObjectValueAsync("CLOUDFLARE_AuthorizationEmail"),
                AccountId = await objectsService.GetSystemObjectValueAsync("CLOUDFLARE_AccountId")
            };
        }

        /// <inheritdoc cref="ICloudFlareService" />
        public async Task<string> UploadImage(string fileName)
        {
            client.BaseAddress = new Uri(ApiPath);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            //todo: add other headers and make call..

            return string.Empty;

        }

        /// <inheritdoc cref="ICloudFlareService" />
        public Task<byte[]> GetImage(string url)
        {
            throw new System.NotImplementedException();
        }
    }
}
