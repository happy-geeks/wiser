using Api.Modules.CloudFlare.Models;
using Api.Modules.CloudFlare.Interfaces;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using System;
using System.Linq;
using RestSharp;
using Newtonsoft.Json;

namespace Api.Modules.CloudFlare.Services
{
    /// <inheritdoc cref="ICloudFlareService" />
    public class CloudFlareService : ICloudFlareService, IScopedService
    {
        private const string ApiPath = "https://api.cloudflare.com/client/v4/accounts/";
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
            var cloudFlareSettings = await GetCloudFlareSettings();

            var restClient = new RestClient($"{ApiPath}{cloudFlareSettings.AccountId}/image/v1");
            var restRequest = new RestRequest("", Method.Post);
            restRequest.AddHeader("X-Auth-Key", cloudFlareSettings.AuthorizationKey);
            restRequest.AddHeader("X-Auth-Email", cloudFlareSettings.AuthorizationEmail);

            restRequest.AddParameter("file", fileName, ParameterType.RequestBody);
            var response = await restClient.ExecuteAsync(restRequest);
            if (response.Content.Contains("ERROR"))
            {
                return String.Empty;
            }
            var uploadImageResponse = JsonConvert.DeserializeObject<UploadImageReponseModel>(response.Content);
            if (!uploadImageResponse.Success)
            {
                return String.Empty;
            }
            var firstResult = uploadImageResponse.Result.First();
            return $"{firstResult.Variants.Original}/{firstResult.Filename}";
            //TODO: Ook ID mee teruggeven..
        }

        /// <inheritdoc cref="ICloudFlareService" />
        public Task<byte[]> GetImage(string url)
            //TODO: Baseren op Id, niet op url...
        {
            throw new System.NotImplementedException();
        }
    }
}
