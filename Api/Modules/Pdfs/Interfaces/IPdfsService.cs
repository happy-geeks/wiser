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
        /// <param name="isTest">Optional: Whether or not this is a test environment. Default is <see langword="false"/></param>
        /// <returns>A byte array of the generated PDF.</returns>
        Task<ServiceResult<string>> SaveHtmlAsPdfAsync(ClaimsIdentity identity, HtmlToPdfRequestModel data);
    }
}
