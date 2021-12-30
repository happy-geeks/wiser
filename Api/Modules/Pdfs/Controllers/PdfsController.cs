using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Modules.Pdfs.Interfaces;
using GeeksCoreLibrary.Modules.GclConverters.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Pdfs.Controllers
{
    /// <summary>
    /// Controller for PDF functions, such as converting HTML to PDF.
    /// </summary>
    [Route("api/v3/[controller]")]
    [Route("api/v3/pdf")]
    [ApiController, Authorize]
    public class PdfsController : ControllerBase
    {
        private readonly IPdfService pdfsService;

        /// <summary>
        /// Creates a new instance of PdfsController.
        /// </summary>
        public PdfsController(IPdfService pdfsService)
        {
            this.pdfsService = pdfsService;
        }
        
        /// <summary>
        /// Convert HTML to a PDF.
        /// </summary>
        /// <param name="data">The HTML and PDF settings.</param>
        /// <returns>The generated PDF.</returns>
        [HttpPost, Route("from-html")]
        public async Task<IActionResult> ConvertHtmlToPdfAsync(HtmlToPdfRequestModel data)
        {
            data.Html = WebUtility.HtmlDecode(data.Html);
            return await pdfsService.ConvertHtmlToPdfAsync((ClaimsIdentity)User.Identity, data);
        }
        
        /// <summary>
        /// Convert HTML to a PDF and then saved is to the disc on the server. It will return the location of the new file on disc.
        /// </summary>
        /// <param name="data">The HTML and PDF settings.</param>
        /// <param name="isTest">Optional: Whether or not this is a test environment. Default is <see langword="false"/></param>
        [HttpPost, Route("save-html-as-pdf")]
        public async Task<IActionResult> SaveHtmlAsPdfAsync(HtmlToPdfRequestModel data)
        {
            data.Html = WebUtility.HtmlDecode(data.Html);
            return (await pdfsService.SaveHtmlAsPdfAsync((ClaimsIdentity)User.Identity, data)).GetHttpResponseMessage();
        }
    }
}
