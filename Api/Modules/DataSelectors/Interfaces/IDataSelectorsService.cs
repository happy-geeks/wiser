using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.DataSelectors.Models;
using GeeksCoreLibrary.Core.Models;
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
        Task<ServiceResult<List<DataSelectorEntityPropertyModel>>> GetEntityProperties(string entityName, bool forExportMode, ClaimsIdentity identity);

        /// <summary>
        /// Get the saved data selectors.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="forExportModule">Optional: Set to true to only get data selectors that can be shown in the export module.</param>
        /// <param name="forRendering">Optional: Set to true to only get data selectors to use with templating rendering.</param>
        /// <param name="forCommunicationModule">Optional: Set to true to only get data selectors that can be shown in the communication module.</param>
        /// <param name="forBranches">Optional: Set to true to only get data selectors that can be used when creating branches.</param>
        /// <returns>A list of <see cref="DataSelectorModel"/>.</returns>
        Task<ServiceResult<List<DataSelectorModel>>> GetAsync(ClaimsIdentity identity, bool forExportModule = false, bool forRendering = false, bool forCommunicationModule = false, bool forBranches = false);

        /// <summary>
        /// Saves a data selector based on name. The ID will be ignored. If a data selector with the given name already exists, it will be overwritten.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="data">The data to save.</param>
        Task<ServiceResult<int>> SaveAsync(ClaimsIdentity identity, DataSelectorModel data);

        /// <summary>
        /// Generate a signature.
        /// </summary>
        /// <param name="values">The values used for the signature.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        Task<ServiceResult<DataSelectorSignatureResultModel>> GenerateSignatureAsync(SortedList<string, string> values, ClaimsIdentity identity);

        /// <summary>
        /// Get the result of the data selector based on the request.
        /// </summary>
        /// <param name="data">The request containing the information for the data selector.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        Task<ServiceResult<JArray>> GetResultsAsync(WiserDataSelectorRequestModel data, ClaimsIdentity identity);

        /// <summary>
        /// Get the query of the data selector based on the request.
        /// </summary>
        /// <param name="data">The request containing the information for the data selector.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        Task<ServiceResult<string>> GetQueryAsync(WiserDataSelectorRequestModel data, ClaimsIdentity identity);

        /// <summary>
        /// Get the result of the data selector based on the request as an Excel file.
        /// </summary>
        /// <param name="data">The request containing the information for the data selector.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        Task<ServiceResult<byte[]>> ToExcelAsync(WiserDataSelectorRequestModel data, ClaimsIdentity identity);

        /// <summary>
        /// Get the result of the data selector based on the request as a HTML page.
        /// </summary>
        /// <param name="data">The request containing the information for the data selector.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        Task<ServiceResult<string>> ToHtmlAsync(WiserDataSelectorRequestModel data, ClaimsIdentity identity);

        /// <summary>
        /// Get the result of the data selector based on the request as a PDF file.
        /// </summary>
        /// <param name="data">The request containing the information for the data selector.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        Task<ServiceResult<FileContentResult>> ToPdfAsync(WiserDataSelectorRequestModel data, ClaimsIdentity identity);
        
        /// <summary>
        /// Get the result of the data selector based on the request as a Csv file.
        /// </summary>
        /// <param name="data">The request containing the information for the data selector.</param>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="separator">The character used to</param>
        Task<ServiceResult<byte[]>> ToCsvAsync(WiserDataSelectorRequestModel data, ClaimsIdentity identity, char separator = ',');

        /// <summary>
        /// Create a file result for the user to download.
        /// </summary>
        /// <param name="data">The request containing the information for the data selector.</param>
        /// <param name="result">The result of the data selector.</param>
        /// <param name="defaultFileName">The default name for the file if no name has been set in the request.</param>
        /// <param name="extension">The extension of the file to save as.</param>
        /// <param name="contentType">The content type of the file.</param>
        IActionResult CreateFileResult(WiserDataSelectorRequestModel data, ServiceResult<byte[]> result, string defaultFileName, string extension, string contentType);

        /// <summary>
        /// Set the correct name for the file result.
        /// </summary>
        /// <param name="data">The request containing the information for the data selector.</param>
        /// <param name="result">The file result of the data selector.</param>
        /// <param name="defaultFileName">The default name for the file if no name has been set in the request.</param>
        /// <param name="extension">The extension of the file to save as.</param>
        IActionResult SetFileName(WiserDataSelectorRequestModel data, ServiceResult<FileContentResult> result, string defaultFileName, string extension);

        /// <summary>
        /// Get templates that can be used with data selectors.
        /// </summary>
        /// <returns>A list of WiserItemModel.</returns>
        Task<ServiceResult<List<WiserItemModel>>> GetTemplatesAsync(ClaimsIdentity identity);

        /// <summary>
        /// Execute a data selector by ID and return the results as JSON.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="id">The ID of the data selector.</param>
        /// <param name="asKeyValuePair">If set to true the result of the query will be converted to a single object. Only columns with the names "key" and "value" are used.</param>
        /// <param name="parameters">The parameters to set before executing the data selector.</param>
        /// <param name="skipPermissionsCheck">Optional: Whether the permissions check should be skipped. This should only ever be set to <see langword="true"/> when calling this function internally.</param>
        /// <returns>The results of the data selector as JSON.</returns>
        Task<ServiceResult<JToken>> GetDataSelectorResultAsJsonAsync(ClaimsIdentity identity, int id, bool asKeyValuePair, List<KeyValuePair<string, object>> parameters, bool skipPermissionsCheck = false);

        /// <summary>
        /// Checks if there is a data selector that already has "show in dashboard" enabled. If so, the name of the
        /// data selector will be returned. Otherwise, <see langword="null">null</see>.
        /// </summary>
        /// <param name="id">The ID of the current data selector, which will be excluded from the check. This will be 0 if it's a new data selector.</param>
        /// <returns>Name of a data selector that has "show in dashboard" enabled, or <see langword="null">null</see> if no data selector has that option enabled.</returns>
        Task<ServiceResult<string>> CheckDashboardConflictAsync(int id);

        /// <summary>
        /// Check whether a data selector with the given name exists.
        /// </summary>
        /// <param name="name">The name of the data selector.</param>
        /// <returns>The ID of the data selector if it exists, or 0 if it doesn't.</returns>
        Task<ServiceResult<int>> ExistsAsync(string name);
    }
}