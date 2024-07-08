using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Files.Interfaces;
using Api.Modules.Pdfs.Interfaces;
using Api.Modules.Tenants.Interfaces;
using EvoPdf;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclConverters.Interfaces;
using GeeksCoreLibrary.Modules.GclConverters.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Api.Modules.Pdfs.Services
{
    /// <inheritdoc cref="IPdfService" />
    public class PdfsService : IPdfService, IScopedService
    {
        private readonly IHtmlToPdfConverterService htmlToPdfConverterService;
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IWiserTenantsService wiserTenantsService;
        private readonly IFilesService filesService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly GclSettings gclSettings;

        /// <summary>
        /// Creates a new instance of <see cref="PdfsService"/>.
        /// </summary>
        public PdfsService(IHtmlToPdfConverterService htmlToPdfConverterService, IDatabaseConnection clientDatabaseConnection, IWiserTenantsService wiserTenantsService, IFilesService filesService, IWebHostEnvironment webHostEnvironment, IOptions<GclSettings> gclSettings)
        {
            this.htmlToPdfConverterService = htmlToPdfConverterService;
            this.clientDatabaseConnection = clientDatabaseConnection;
            this.wiserTenantsService = wiserTenantsService;
            this.filesService = filesService;
            this.webHostEnvironment = webHostEnvironment;
            this.gclSettings = gclSettings.Value;
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
            var tenant = await wiserTenantsService.GetSingleAsync(identity);
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
            var pdfDirectory = Path.Combine(webHostEnvironment.WebRootPath, "/App_Data/temp/", tenant.ModelObject.Id.ToString());
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

        /// <inheritdoc />
        public async Task<ServiceResult<byte[]>> MergePdfFilesAsync(ClaimsIdentity identity, string[] encryptedItemIdsList, string[] propertyNames, string entityType)
        {
            var tenant = await wiserTenantsService.GetSingleAsync(identity);

            Document mergeResultPdfDocument = null;
            //Load the documents and add them to the merged file
            foreach (var encryptedId in encryptedItemIdsList)
            {
                foreach (var propertyName in propertyNames)
                {
                    var pdfFile = await filesService.GetAsync(encryptedId, 0, identity, 0, entityType, propertyName:propertyName);
                    MemoryStream pdfStream = new MemoryStream();
                    // Check if the PDF must be downloaded first
                    if (!String.IsNullOrWhiteSpace(pdfFile.ModelObject.Url))
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            using (HttpResponseMessage response = await client.GetAsync(pdfFile.ModelObject.Url, HttpCompletionOption.ResponseHeadersRead))
                            {
                                response.EnsureSuccessStatusCode();
                                using (Stream downloadStream = await response.Content.ReadAsStreamAsync())
                                {
                                    await downloadStream.CopyToAsync(pdfStream);
                                }
                            }
                        }
                    }
                    else if (!pdfFile.ModelObject.Data.IsNullOrEmpty())
                    {
                        pdfStream = new MemoryStream(pdfFile.ModelObject.Data);
                    }

                    // If the pdf file is empty (no file at the URL and no file in the blob field) then skip to next file
                    if (pdfStream.Length == 0)
                    {
                        continue;
                    }
                    if (mergeResultPdfDocument == null)
                    {
                        mergeResultPdfDocument = new Document(pdfStream);
                        mergeResultPdfDocument.LicenseKey = gclSettings.EvoPdfLicenseKey;
                    }
                    else
                    {
                        mergeResultPdfDocument.AppendDocument(new Document(pdfStream));
                    }
                }
            }

            using var saveStream = new MemoryStream();
            mergeResultPdfDocument.Save(saveStream);
            return new ServiceResult<byte[]>(saveStream.ToArray());
        }
    }
}