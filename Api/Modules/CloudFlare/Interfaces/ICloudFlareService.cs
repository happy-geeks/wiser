using System.Threading.Tasks;

namespace Api.Modules.CloudFlare.Interfaces
{
    /// <summary>
    /// Serice for handling CloudFlare services (images)
    /// </summary>
    public interface ICloudFlareService
    {

        /// <summary>
        /// Uploads an image to CloudFlare
        /// </summary>
        /// <param name="fileName">Name of the file to upload.</param>
        /// <param name="fileBytes">Contents of the file to upload.</param>
        /// <returns>string with url from CloudFlare</returns>
        Task<string> UploadImageAsync(string fileName, byte[] fileBytes);

        /// <summary>
        /// Deletes an image based on the image id encapsulated in the url
        /// </summary>
        /// <param name="url">Url from Cloudflare</param>
        /// <returns></returns>
        Task DeleteImageAsync(string url);

    }
}
