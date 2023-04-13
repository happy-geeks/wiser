using System.IO;
using System.Net;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Customers.Interfaces;
using Api.Modules.Files.Interfaces;
using Api.Modules.Pdfs.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclConverters.Interfaces;
using GeeksCoreLibrary.Modules.GclConverters.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Pdfs.Services
{
    /// <inheritdoc cref="IPdfService" />
    public class PdfsService : IPdfService, IScopedService
    {
        private readonly IHtmlToPdfConverterService htmlToPdfConverterService;
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IWiserCustomersService wiserCustomersService;
        private readonly IFilesService filesService;
        private readonly IWebHostEnvironment webHostEnvironment;

        /// <summary>
        /// Creates a new instance of <see cref="PdfsService"/>.
        /// </summary>
        public PdfsService(IHtmlToPdfConverterService htmlToPdfConverterService, IDatabaseConnection clientDatabaseConnection, IWiserCustomersService wiserCustomersService, IFilesService filesService, IWebHostEnvironment webHostEnvironment)
        {
            this.htmlToPdfConverterService = htmlToPdfConverterService;
            this.clientDatabaseConnection = clientDatabaseConnection;
            this.wiserCustomersService = wiserCustomersService;
            this.filesService = filesService;
            this.webHostEnvironment = webHostEnvironment;
        }

        /// <inheritdoc />
        public async Task<FileContentResult> ConvertHtmlToPdfAsync(ClaimsIdentity identity, HtmlToPdfRequestModel data)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            return await htmlToPdfConverterService.ConvertHtmlStringToPdfAsync(data);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> SaveHtmlAsPdfAsync(ClaimsIdentity identity, HtmlToPdfRequestModel data)
        {
            var customer = await wiserCustomersService.GetSingleAsync(identity);
            var pdfResult = await ConvertHtmlToPdfAsync(identity, data);

            if (data.SaveInDatabase)
            {
                var saveResult = await filesService.SaveAsync(identity, pdfResult.FileContents, MediaTypeNames.Application.Pdf, pdfResult.FileDownloadName, "TEMPORARY_FILE_FROM_WISER");
                if (saveResult.StatusCode != HttpStatusCode.OK)
                {
                    return new ServiceResult<string>
                    {
                        StatusCode = saveResult.StatusCode,
                        ErrorMessage = saveResult.ErrorMessage
                    };
                }

                return new ServiceResult<string>(saveResult.ModelObject.FileId.ToString());
            }

            // Create temporary directory if it doesn't exist yet.
            var pdfDirectory = Path.Combine(webHostEnvironment.WebRootPath, "/App_Data/temp/", customer.ModelObject.Id.ToString());
            if (!Directory.Exists(pdfDirectory))
            {
                Directory.CreateDirectory(pdfDirectory);
            }

            // Save the file to disc.
            var fileLocation = Path.Combine(pdfDirectory, data.FileName);
            await using (var fileStream = new FileStream(fileLocation, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await fileStream.WriteAsync(pdfResult.FileContents, 0, pdfResult.FileContents.Length);
            }

            return new ServiceResult<string>(fileLocation);
        }
    }
}