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
        private async Task<CloudFlareSettingsModel> GetCloudFlareSettingsAsync()
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
        public async Task<string> UploadImageAsync(string fileName, byte[] fileBytes)
        {
            var cloudFlareSettings = await GetCloudFlareSettingsAsync();
            var accountId = cloudFlareSettings.AccountId;

            var route = $"{ApiPath}{cloudFlareSettings.AccountId}/images/v1/direct_upload";
            var restClient = new RestClient(route);
            var restRequest = new RestRequest("", Method.Post);
            restRequest.AddHeader("X-Auth-Key", cloudFlareSettings.AuthorizationKey);
            restRequest.AddHeader("X-Auth-Email", cloudFlareSettings.AuthorizationEmail);

            var response = await restClient.ExecuteAsync(restRequest);
            var directUploadResponse = JsonConvert.DeserializeObject<DirectUploadResponseModel>(response.Content);
            if (!directUploadResponse.Success)
            {
                return String.Empty;
            }
            var upLoadUrl = directUploadResponse.Result.UploadURL;
            var uploadClient = new RestClient(upLoadUrl);
            var uploadRequest = new RestRequest("", Method.Post);
            uploadRequest.AddFile("file", fileBytes, fileName);
            var uploadResponse = await uploadClient.ExecuteAsync(uploadRequest);
            var uploadResult = JsonConvert.DeserializeObject<UploadImageResponseModel>(uploadResponse.Content);
            if (!uploadResult.Success)
            {
                return String.Empty;
            }
            return uploadResult.Result.Variants.First();

        }

        /// <inheritdoc cref="ICloudFlareService" />
        public async Task DeleteImageAsync(string url)
        {
            var urlParts = url.Split('/');
            var imageId = urlParts[4];
            var cloudFlareSettings = await GetCloudFlareSettingsAsync();
            var accountId = cloudFlareSettings.AccountId;

            var route = $"{ApiPath}{cloudFlareSettings.AccountId}/images/v1/{imageId}";
            var restClient = new RestClient(route);
            var restRequest = new RestRequest("", Method.Delete);
            restRequest.AddHeader("X-Auth-Key", cloudFlareSettings.AuthorizationKey);
            restRequest.AddHeader("X-Auth-Email", cloudFlareSettings.AuthorizationEmail);

            var response = await restClient.ExecuteAsync(restRequest);
        }

    }
}
