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
        /// <param name="fileName">Filename of the original file to upload.</param>
        /// <returns>string with url from CloudFlare</returns>
        Task<string> UploadImage(string fileName);

        /// <summary>
        /// Gets an image from CloudFlarw
        /// </summary>
        /// <param name="url">url on CloudFlare to get</param>
        /// <returns></returns>
        Task<byte[]> GetImage(string url);

    }
}
