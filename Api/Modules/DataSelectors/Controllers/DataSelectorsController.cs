using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Api.Modules.DataSelectors.Interfaces;
using Api.Modules.DataSelectors.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Api.Modules.DataSelectors.Controllers
{
    /// <summary>
    /// Controller for the data selector in Wiser.
    /// </summary>
    [Route("api/v3/data-selectors"), ApiController, Authorize]
    public class DataSelectorsController : ControllerBase
    {
        private readonly IDataSelectorsService dataSelectorsService;

        /// <summary>
        /// Creates a new instance of <see cref="DataSelectorsController"/>.
        /// </summary>
        public DataSelectorsController(IDataSelectorsService dataSelectorsService)
        {
            this.dataSelectorsService = dataSelectorsService;
        }

        /// <summary>
        /// Retrieves the entity properties belonging to the given entity name.
        /// </summary>
        /// <param name="entityName">The name of the entity.</param>
        /// <param name="forExportMode">Whether the data selector is in export mode.</param>
        [HttpGet, Route("entity-properties/{entityName}"), ProducesResponseType(typeof(List<DataSelectorEntityPropertyModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEntityPropertiesAsync(string entityName, [FromQuery] bool forExportMode)
        {
            return (await dataSelectorsService.GetEntityProperties(entityName, forExportMode, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Get the saved data selectors.
        /// </summary>
        [HttpGet, ProducesResponseType(typeof(List<DataSelectorModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAsync()
        {
            return (await dataSelectorsService.GetAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Generate a signature.
        /// </summary>
        /// <param name="values">The values used for the signature.</param>
        [HttpPost, Route("signature"), ProducesResponseType(typeof(DataSelectorSignatureResultModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerateSignatureAsync(SortedList<string, string> values)
        {
            return (await dataSelectorsService.GenerateSignatureAsync(values, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Get the result of the data selector based on the request.
        /// </summary>
        /// <param name="dataFromUri">The request containing the information for the data selector in the query.</param>
        [HttpPost, ProducesResponseType(typeof(JArray), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetResultsAsync([FromQuery] WiserDataSelectorRequestModel dataFromUri)
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var dataFromBody = await reader.ReadToEndAsync();
            var bodyModel = String.IsNullOrWhiteSpace(dataFromBody) ? new WiserDataSelectorRequestModel() : JsonConvert.DeserializeObject<WiserDataSelectorRequestModel>(dataFromBody, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
            return (await dataSelectorsService.GetResultsAsync(CombineDataSelectorRequestModels(bodyModel, dataFromUri), (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Get the query of the data selector based on the request.
        /// </summary>
        /// <param name="dataFromUri">The request containing the information for the data selector in the query.</param>
        [HttpPost, Route("query"), ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetQueryAsync([FromQuery] WiserDataSelectorRequestModel dataFromUri)
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var dataFromBody = await reader.ReadToEndAsync();
            var bodyModel = String.IsNullOrWhiteSpace(dataFromBody) ? new WiserDataSelectorRequestModel() : JsonConvert.DeserializeObject<WiserDataSelectorRequestModel>(dataFromBody, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
            return (await dataSelectorsService.GetQueryAsync(CombineDataSelectorRequestModels(bodyModel, dataFromUri), (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Get the result of the data selector based on the request as an Excel file.
        /// </summary>
        /// <param name="dataFromUri">The request containing the information for the data selector in the query.</param>
        [HttpPost, Route("excel"), ProducesResponseType(typeof(List<DataSelectorModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ToExcelAsync([FromQuery] WiserDataSelectorRequestModel dataFromUri)
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var dataFromBody = await reader.ReadToEndAsync();
            var bodyModel = String.IsNullOrWhiteSpace(dataFromBody) ? new WiserDataSelectorRequestModel() : JsonConvert.DeserializeObject<WiserDataSelectorRequestModel>(dataFromBody, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
            var data = CombineDataSelectorRequestModels(bodyModel, dataFromUri);
            var result = await dataSelectorsService.ToExcelAsync(data, (ClaimsIdentity)User.Identity);
            return result.StatusCode != HttpStatusCode.OK ? result.GetHttpResponseMessage() : dataSelectorsService.CreateFileResult(data, result, "Export.xlsx", ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        /// <summary>
        /// Get the result of the data selector based on the request as a HTML page.
        /// </summary>
        /// <param name="dataFromUri">The request containing the information for the data selector in the query.</param>
        [HttpPost, Route("html"), ProducesResponseType(typeof(List<DataSelectorModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ToHtmlAsync([FromQuery] WiserDataSelectorRequestModel dataFromUri)
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var dataFromBody = await reader.ReadToEndAsync();
            var bodyModel = String.IsNullOrWhiteSpace(dataFromBody) ? new WiserDataSelectorRequestModel() : JsonConvert.DeserializeObject<WiserDataSelectorRequestModel>(dataFromBody, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
            return (await dataSelectorsService.ToHtmlAsync(CombineDataSelectorRequestModels(bodyModel, dataFromUri), (ClaimsIdentity)User.Identity)).GetHttpResponseMessage("text/html");
        }

        /// <summary>
        /// Get the result of the data selector based on the request as a PDF file.
        /// </summary>
        /// <param name="dataFromUri">The request containing the information for the data selector in the query.</param>
        [HttpPost, Route("pdf"), ProducesResponseType(typeof(List<DataSelectorModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ToPdfAsync([FromQuery] WiserDataSelectorRequestModel dataFromUri)
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var dataFromBody = await reader.ReadToEndAsync();
            var bodyModel = String.IsNullOrWhiteSpace(dataFromBody) ? new WiserDataSelectorRequestModel() : JsonConvert.DeserializeObject<WiserDataSelectorRequestModel>(dataFromBody, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
            var data = CombineDataSelectorRequestModels(bodyModel, dataFromUri);
            var result = await dataSelectorsService.ToPdfAsync(data, (ClaimsIdentity)User.Identity);
            return result.StatusCode != HttpStatusCode.OK ? result.GetHttpResponseMessage() : dataSelectorsService.SetFileName(data, result, $"{Guid.NewGuid():N}.pdf", ".pdf");
        }

        /// <summary>
        /// Combine two <see cref="WiserDataSelectorRequestModel"/>s. Used to combine information from the body with the information from the query.
        /// </summary>
        /// <param name="model1">The first <see cref="WiserDataSelectorRequestModel"/>.</param>
        /// <param name="model2">The second <see cref="WiserDataSelectorRequestModel"/>.</param>
        private WiserDataSelectorRequestModel CombineDataSelectorRequestModels(WiserDataSelectorRequestModel model1, WiserDataSelectorRequestModel model2)
        {
            if (model1 == null)
            {
                return model2;
            }

            if (model2 == null)
            {
                return model1;
            }

            model1.Settings ??= model2.Settings;
            model1.Descendants ??= model2.Descendants;
            model1.Environment ??= model2.Environment;
            model1.ExtraData ??= model2.ExtraData;
            model1.DataSelectorId ??= model2.DataSelectorId;
            if (String.IsNullOrWhiteSpace(model1.EncryptedDataSelectorId)) model1.EncryptedDataSelectorId = model2.EncryptedDataSelectorId;
            if (String.IsNullOrWhiteSpace(model1.QueryId)) model1.QueryId = model2.QueryId;
            if (String.IsNullOrWhiteSpace(model1.ModuleId)) model1.ModuleId = model2.ModuleId;
            model1.NumberOfLevels ??= model2.NumberOfLevels;
            if (String.IsNullOrWhiteSpace(model1.LanguageCode)) model1.LanguageCode = model2.LanguageCode;
            if (String.IsNullOrWhiteSpace(model1.NumberOfItems)) model1.NumberOfItems = model2.NumberOfItems;
            model1.PageNumber ??= model2.PageNumber;
            if (String.IsNullOrWhiteSpace(model1.ContainsPath)) model1.ContainsPath = model2.ContainsPath;
            if (String.IsNullOrWhiteSpace(model1.ContainsPath)) model1.ContainsPath = model2.ContainsPath;
            if (String.IsNullOrWhiteSpace(model1.ParentId)) model1.ParentId = model2.ParentId;
            if (String.IsNullOrWhiteSpace(model1.EntityTypes)) model1.EntityTypes = model2.EntityTypes;
            model1.LinkType ??= model2.LinkType;
            if (String.IsNullOrWhiteSpace(model1.QueryAddition)) model1.QueryAddition = model2.QueryAddition;
            if (String.IsNullOrWhiteSpace(model1.OrderPart)) model1.OrderPart = model2.OrderPart;
            if (String.IsNullOrWhiteSpace(model1.Fields)) model1.Fields = model2.Fields;
            if (String.IsNullOrWhiteSpace(model1.FileTypes)) model1.FileTypes = model2.FileTypes;
            if (String.IsNullOrWhiteSpace(model1.FileName)) model1.FileName = model2.FileName;
            if (String.IsNullOrWhiteSpace(model1.OutputTemplate)) model1.OutputTemplate = model2.OutputTemplate;
            if (String.IsNullOrWhiteSpace(model1.ContentItemId)) model1.ContentItemId = model2.ContentItemId;
            if (String.IsNullOrWhiteSpace(model1.ContentPropertyName)) model1.ContentPropertyName = model2.ContentPropertyName;
            return model1;
        }
    }
}
