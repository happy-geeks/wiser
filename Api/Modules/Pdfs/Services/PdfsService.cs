using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Files.Interfaces;
using Api.Modules.Files.Models;
using Api.Modules.Pdfs.Interfaces;
using Api.Modules.Tenants.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclConverters.Interfaces;
using GeeksCoreLibrary.Modules.GclConverters.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace Api.Modules.Pdfs.Services;

/// <inheritdoc cref="IPdfService" />
public class PdfsService : IPdfService, IScopedService
{
    private readonly IHtmlToPdfConverterService htmlToPdfConverterService;
    private readonly IDatabaseConnection clientDatabaseConnection;
    private readonly IWiserTenantsService wiserTenantsService;
    private readonly IFilesService filesService;
    private readonly IWebHostEnvironment webHostEnvironment;
    private readonly IHttpClientService httpClientService;

    /// <summary>
    /// Creates a new instance of <see cref="PdfsService"/>.
    /// </summary>
    public PdfsService(IHtmlToPdfConverterService htmlToPdfConverterService, IDatabaseConnection clientDatabaseConnection, IWiserTenantsService wiserTenantsService, IFilesService filesService, IWebHostEnvironment webHostEnvironment, IHttpClientService httpClientService)
    {
        this.htmlToPdfConverterService = htmlToPdfConverterService;
        this.clientDatabaseConnection = clientDatabaseConnection;
        this.wiserTenantsService = wiserTenantsService;
        this.filesService = filesService;
        this.webHostEnvironment = webHostEnvironment;
        this.httpClientService = httpClientService;
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
    public async Task<ServiceResult<byte[]>> MergePdfFilesAsync(ClaimsIdentity identity, string[] encryptedItemIdsList, string[] propertyNames, string entityType, SelectionOptions selectionOptions = SelectionOptions.None)
    {
        using var mergeResultPdfDocument = new PdfDocument();

        // Load the documents and add them to the merged file.
        foreach (var encryptedId in encryptedItemIdsList)
        {
            var itemId = await wiserTenantsService.DecryptValue<ulong>(encryptedId, identity);

            foreach (var propertyName in propertyNames)
            {
                var pdfFile = (await filesService.GetAsync(itemId, 0, identity, 0, entityType, propertyName:propertyName, selectionOption: selectionOptions)).ModelObject;
                using var pdfStream = new MemoryStream();

                // Check if the PDF must be downloaded first.
                if (!String.IsNullOrWhiteSpace(pdfFile.Url))
                {
                    using var response = await httpClientService.Client.GetAsync(pdfFile.Url, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                    await using var downloadStream = await response.Content.ReadAsStreamAsync();
                    await downloadStream.CopyToAsync(pdfStream);
                }
                else if (pdfFile.Data is {Length: > 0})
                {
                    await pdfStream.WriteAsync(pdfFile.Data.AsMemory(0, pdfFile.Data.Length));
                }

                // If the pdf file is empty (no file at the URL and no file in the blob field) then skip to next file
                if (pdfStream.Length == 0)
                {
                    continue;
                }

                using var inputDocument = PdfReader.Open(pdfStream, PdfDocumentOpenMode.Import);

                // Copy all pages from the input document to the output document.
                for (var i = inputDocument.PageCount - 1; i >= 0; i--)
                {
                    mergeResultPdfDocument.AddPage(inputDocument.Pages[i]);
                }
            }
        }

        using var saveStream = new MemoryStream();
        mergeResultPdfDocument?.Save(saveStream);
        return new ServiceResult<byte[]>(saveStream.ToArray());
    }
}