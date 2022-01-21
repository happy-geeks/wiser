using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.DataSelectors.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Api.Modules.DataSelectors.Interfaces
{
    /// <summary>
    /// Service for the data selector in Wiser.
    /// </summary>
    public interface IDataSelectorsService
    {
        /// <summary>
        /// Retrieves the entity properties belonging to the given entity name.
        /// </summary>
        /// <param name="entityName">The name of the entity.</param>
        /// <param name="forExportMode">Whether the data selector is in export mode.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns></returns>
        Task<ServiceResult<List<DataSelectorEntityPropertyModel>>> GetEntityProperties(string entityName, bool forExportMode, ClaimsIdentity identity);

        /// <summary>
        /// Get the saved data selectors.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns></returns>
        Task<ServiceResult<List<DataSelectorModel>>> GetAsync(ClaimsIdentity identity);

        /// <summary>
        /// Generate a signature.
        /// </summary>
        /// <param name="values">The values used for the signature.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns></returns>
        Task<ServiceResult<DataSelectorSignatureResultModel>> GenerateSignatureAsync(SortedList<string, string> values, ClaimsIdentity identity);

        /// <summary>
        /// Get the result of the data selector based on the request.
        /// </summary>
        /// <param name="data">The request containing the information for the data selector.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns></returns>
        Task<ServiceResult<JArray>> GetResultsAsync(WiserDataSelectorRequestModel data, ClaimsIdentity identity);

        /// <summary>
        /// Get the query of the data selector based on the request.
        /// </summary>
        /// <param name="data">The request containing the information for the data selector.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns></returns>
        Task<ServiceResult<string>> GetQueryAsync(WiserDataSelectorRequestModel data, ClaimsIdentity identity);

        /// <summary>
        /// Get the result of the data selector based on the request as an Excel file.
        /// </summary>
        /// <param name="data">The request containing the information for the data selector.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns></returns>
        Task<ServiceResult<byte[]>> ToExcelAsync(WiserDataSelectorRequestModel data, ClaimsIdentity identity);

        /// <summary>
        /// Get the result of the data selector based on the request as a HTML page.
        /// </summary>
        /// <param name="data">The request containing the information for the data selector.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns></returns>
        Task<ServiceResult<string>> ToHtmlAsync(WiserDataSelectorRequestModel data, ClaimsIdentity identity);

        /// <summary>
        /// Get the result of the data selector based on the request as a PDF file.
        /// </summary>
        /// <param name="data">The request containing the information for the data selector.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns></returns>
        Task<ServiceResult<FileContentResult>> ToPdfAsync(WiserDataSelectorRequestModel data, ClaimsIdentity identity);

        /// <summary>
        /// Create a file result for the user to download.
        /// </summary>
        /// <param name="data">The request containing the information for the data selector.</param>
        /// <param name="result">The result of the data selector.</param>
        /// <param name="defaultFileName">The default name for the file if no name has been set in the request.</param>
        /// <param name="extension">The extension of the file to save as.</param>
        /// <param name="contentType">The content type of the file.</param>
        /// <returns></returns>
        IActionResult CreateFileResult(WiserDataSelectorRequestModel data, ServiceResult<byte[]> result, string defaultFileName, string extension, string contentType);

        /// <summary>
        /// Set the correct name for the file result.
        /// </summary>
        /// <param name="data">The request containing the information for the data selector.</param>
        /// <param name="result">The file result of the data selector.</param>
        /// <param name="defaultFileName">The default name for the file if no name has been set in the request.</param>
        /// <param name="extension">The extension of the file to save as.</param>
        /// <returns></returns>
        IActionResult SetFileName(WiserDataSelectorRequestModel data, ServiceResult<FileContentResult> result, string defaultFileName, string extension);
    }
}
