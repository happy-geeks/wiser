using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using GeeksCoreLibrary.Modules.GclConverters.Models;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Pdfs.Interfaces
{
    /// <summary>
    /// Service for PDF functions, such as converting HTML to PDF.
    /// </summary>
    public interface IPdfService
    {
        /// <summary>
        /// Convert HTML to a PDF.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="data">The HTML and PDF settings.</param>
        /// <returns>A byte array of the generated PDF.</returns>
        Task<FileContentResult> ConvertHtmlToPdfAsync(ClaimsIdentity identity, HtmlToPdfRequestModel data);

        /// <summary>
        /// Convert HTML to a PDF and then saved is to the disc on the server. It will return the location of the new file on disc.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="data">The HTML and PDF settings.</param>
        /// <returns>A byte array of the generated PDF.</returns>
        Task<ServiceResult<string>> SaveHtmlAsPdfAsync(ClaimsIdentity identity, HtmlToPdfRequestModel data);

        /// <summary>
        /// Download and merge all pdf files
        /// </summary>
        /// <param name="encryptedItemIdsList">comma separted list of encrypted item-ids</param>
        /// <param name="propertyNames">the property name of te files that must be merged </param>
        /// <param name="entityType">the entitytype of the entity of the ID's</param>
        /// <returns>The location of the HTML file on the server.</returns>
        Task<ServiceResult<byte[]>> MergePdfFilesAsync(ClaimsIdentity identity, string[] encryptedItemIdsList, string propertyNames, string entityType);
    }
}
