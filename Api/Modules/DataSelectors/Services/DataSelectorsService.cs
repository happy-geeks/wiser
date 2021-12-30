using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Customers.Interfaces;
using Api.Modules.DataSelectors.Interfaces;
using Api.Modules.DataSelectors.Models;
using Api.Modules.Pdfs.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Exports.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Api.Modules.DataSelectors.Services
{
    /// <summary>
    /// Service for the data selector in Wiser.
    /// </summary>
    public class DataSelectorsService : IDataSelectorsService, IScopedService
    {
        private readonly IWiserCustomersService wiserCustomersService;
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly GeeksCoreLibrary.Modules.DataSelector.Interfaces.IDataSelectorsService gclDataSelectorsService;
        private readonly IExcelService excelService;

        /// <summary>
        /// Creates a new instance of <see cref="DataSelectorsService"/>
        /// </summary>
        public DataSelectorsService(IWiserCustomersService wiserCustomersService, IDatabaseConnection clientDatabaseConnection, IHttpContextAccessor httpContextAccessor, GeeksCoreLibrary.Modules.DataSelector.Interfaces.IDataSelectorsService gclDataSelectorsService, IExcelService excelService)
        {
            this.wiserCustomersService = wiserCustomersService;
            this.clientDatabaseConnection = clientDatabaseConnection;
            this.httpContextAccessor = httpContextAccessor;
            this.gclDataSelectorsService = gclDataSelectorsService;
            this.excelService = excelService;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<DataSelectorModel>>> GetAsync(ClaimsIdentity identity)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT id, name
                                                                            FROM {WiserTableNames.WiserDataSelector}
                                                                            WHERE removed = 0 AND show_in_export_module = 1
                                                                            ORDER BY name ASC");

            var results = new List<DataSelectorModel>();
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<List<DataSelectorModel>>(results);
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                results.Add(new DataSelectorModel
                {
                    Id = dataRow.Field<int>("id"),
                    EncryptedId = await wiserCustomersService.EncryptValue(dataRow.Field<int>("id").ToString(), identity),
                    Name = dataRow.Field<string>("name")
                });
            }

            return new ServiceResult<List<DataSelectorModel>>(results);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<DataSelectorSignatureResultModel>> GenerateSignatureAsync(SortedList<string, string> values, ClaimsIdentity identity)
        {
            var customer = await wiserCustomersService.GetSingleAsync(identity);
            var encryptionKey = customer.ModelObject.EncryptionKey;
            var dateString = DateTime.Now.ToString("yyyyMMddHHmmss");

            var stringToHash = new StringBuilder();
            values["trace"] = "false";
            values["datetime"] = dateString;

            stringToHash.Append(String.Join("", values.Select(value => value.Key + value.Value)));
            stringToHash.Append("secret");
            stringToHash.Append(encryptionKey);

            var result = new DataSelectorSignatureResultModel
            {
                Signature = stringToHash.ToString().ToSha512ForPasswords(),
                ExtraQueryString = $"datetime={dateString}&trace=false"
            };

            return new ServiceResult<DataSelectorSignatureResultModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<JArray>> GetResultsAsync(WiserDataSelectorRequestModel data, ClaimsIdentity identity)
        {
            var (jsonResult, statusCode, error) = await GetJsonResponseAsync(data, identity);
            if (statusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<JArray>
                {
                    StatusCode = statusCode,
                    ErrorMessage = error,
                    ReasonPhrase = error
                };
            }

            return new ServiceResult<JArray>(jsonResult);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> GetQueryAsync(WiserDataSelectorRequestModel data, ClaimsIdentity identity)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                throw new Exception("HttpContext.Current is null, can't proceed.");
            }

            // Set the encryption key for the JCL internally. The JCL can't know which key to use otherwise.
            var customer = (await wiserCustomersService.GetSingleAsync(identity)).ModelObject;
            httpContext.Items["QueryTemplatesDecryptionKey"] = customer.EncryptionKey;
            GclSettings.Current.QueryTemplatesDecryptionKey = customer.EncryptionKey;

            var queryId = 0;
            var dataSelectorId = 0;
            if (data != null && !Int32.TryParse(data.QueryId, out queryId))
            {
                queryId = await wiserCustomersService.DecryptValue<int>(data.QueryId, identity);
            }

            if (data != null && !Int32.TryParse(data?.EncryptedDataSelectorId, out dataSelectorId))
            {
                dataSelectorId = await wiserCustomersService.DecryptValue<int>(data.EncryptedDataSelectorId, identity);
                data.DataSelectorId = dataSelectorId;
            }

            if (data == null || (queryId == 0 && data.Settings == null && dataSelectorId == 0 && String.IsNullOrWhiteSpace(data.ContainsPath) && String.IsNullOrWhiteSpace(data.EntityTypes)))
            {
                return new ServiceResult<string>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "No data selector, path AND entity type found! Please make sure you supply either a valid JSON object or an ID of a valid selector, or a path + entity type.",
                    ReasonPhrase = "No data selector, path AND entity type found! Please make sure you supply either a valid JSON object or an ID of a valid selector, or a path + entity type."
                };
            }

            var (itemsRequest, statusCode, error) = await gclDataSelectorsService.InitializeItemsRequestAsync(data);
            if (statusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<string>
                {
                    StatusCode = statusCode,
                    ErrorMessage = error,
                    ReasonPhrase = error
                };
            }

            var query = await gclDataSelectorsService.GetQueryAsync(itemsRequest);
            return new ServiceResult<string>(query);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<byte[]>> ToExcelAsync(WiserDataSelectorRequestModel data, ClaimsIdentity identity)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                throw new Exception("HttpContext.Current is null, can't proceed.");
            }

            // Set the encryption key for the JCL internally. The JCL can't know which key to use otherwise.
            var customer = (await wiserCustomersService.GetSingleAsync(identity)).ModelObject;
            httpContext.Items["QueryTemplatesDecryptionKey"] = customer.EncryptionKey;
            GclSettings.Current.QueryTemplatesDecryptionKey = customer.EncryptionKey;

            var (jsonResult, statusCode, error) = await GetJsonResponseAsync(data, identity);
            if (statusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<byte[]>
                {
                    StatusCode = statusCode,
                    ErrorMessage = error,
                    ReasonPhrase = error
                };
            }
            
            var excelFile = excelService.JsonArrayToExcel(jsonResult);
            return new ServiceResult<byte[]>(excelFile);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> ToHtmlAsync(WiserDataSelectorRequestModel data, ClaimsIdentity identity)
        {
            if (data == null)
            {
                return new ServiceResult<string>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "No data received",
                    ReasonPhrase = "No data received"
                };
            }

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                throw new Exception("HttpContext.Current is null, can't proceed.");
            }

            // Set the encryption key for the JCL internally. The JCL can't know which key to use otherwise.
            var customer = (await wiserCustomersService.GetSingleAsync(identity)).ModelObject;
            httpContext.Items["QueryTemplatesDecryptionKey"] = customer.EncryptionKey;
            GclSettings.Current.QueryTemplatesDecryptionKey = customer.EncryptionKey;

            // This is for backwards compatibility, a lot of queries in wiser_query contain {itemId_decrypt_withdate}, but the GCL expects something like {itemId:decrypt(true)}.
            // To not have to change all queries for all our customers, we made this workaround so that old queries still work. The GCL will replace everything from httpContext.Items automatically.
            if (!String.IsNullOrWhiteSpace(httpContext.Request.Query["itemId"]))
            {
                httpContext.Items["itemId_decrypt_withdate"] = await wiserCustomersService.DecryptValue<ulong>(httpContext.Request.Query["itemId"], identity);
            }

            var (result, statusCode, error) = await gclDataSelectorsService.ToHtmlAsync(data);
            if (statusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<string>
                {
                    StatusCode = statusCode,
                    ErrorMessage = error,
                    ReasonPhrase = error
                };
            }

            return new ServiceResult<string>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<FileContentResult>> ToPdfAsync(WiserDataSelectorRequestModel data, ClaimsIdentity identity)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                throw new Exception("HttpContext.Current is null, can't proceed.");
            }

            // Set the encryption key for the JCL internally. The JCL can't know which key to use otherwise.
            var customer = (await wiserCustomersService.GetSingleAsync(identity)).ModelObject;
            httpContext.Items["QueryTemplatesDecryptionKey"] = customer.EncryptionKey;
            GclSettings.Current.QueryTemplatesDecryptionKey = customer.EncryptionKey;
            
            var (result, statusCode, error) = await gclDataSelectorsService.ToPdfAsync(data);
            if (statusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<FileContentResult>
                {
                    StatusCode = statusCode,
                    ErrorMessage = error,
                    ReasonPhrase = error
                };
            }

            return new ServiceResult<FileContentResult>(result);
        }

        /// <inheritdoc />
        public IActionResult CreateFileResult(WiserDataSelectorRequestModel data, ServiceResult<byte[]> result, string defaultFileName, string extension, string contentType)
        {
            if (String.IsNullOrWhiteSpace(data.FileName))
            {
                data.FileName = defaultFileName;
            }
            data.FileName = Path.ChangeExtension(data.FileName, extension);

            var fileResult = new FileContentResult(result.ModelObject, contentType)
            {
                FileDownloadName = data.FileName
            };

            return fileResult;
        }

        /// <inheritdoc />
        public IActionResult SetFileName(WiserDataSelectorRequestModel data, ServiceResult<FileContentResult> result, string defaultFileName, string extension)
        {
            if (String.IsNullOrWhiteSpace(data.FileName))
            {
                data.FileName = defaultFileName;
            }
            data.FileName = Path.ChangeExtension(data.FileName, extension);

            result.ModelObject.FileDownloadName = data.FileName;
            return result.ModelObject;
        }
        
        private async Task<(JArray Result, HttpStatusCode StatusCode, string Error)> GetJsonResponseAsync(WiserDataSelectorRequestModel data, ClaimsIdentity identity)
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                throw new Exception("HttpContext.Current is null, can't proceed.");
            }

            // Set the encryption key for the JCL internally. The JCL can't know which key to use otherwise.
            var customer = (await wiserCustomersService.GetSingleAsync(identity)).ModelObject;
            httpContext.Items["QueryTemplatesDecryptionKey"] = customer.EncryptionKey;
            GclSettings.Current.QueryTemplatesDecryptionKey = customer.EncryptionKey;

            var queryId = 0;
            var dataSelectorId = 0;
            if (!String.IsNullOrWhiteSpace(data?.QueryId) && !Int32.TryParse(data.QueryId, out queryId))
            {
                queryId = await wiserCustomersService.DecryptValue<int>(data.QueryId, identity);
            }

            if (!String.IsNullOrWhiteSpace(data?.EncryptedDataSelectorId) && !Int32.TryParse(data.EncryptedDataSelectorId, out dataSelectorId))
            {
                dataSelectorId = await wiserCustomersService.DecryptValue<int>(data.EncryptedDataSelectorId, identity);
                data.DataSelectorId = dataSelectorId;
            }

            if (data == null || (queryId == 0 && data.Settings == null && dataSelectorId == 0 && String.IsNullOrWhiteSpace(data.ContainsPath) && String.IsNullOrWhiteSpace(data.EntityTypes)))
            {
                return (null, HttpStatusCode.BadRequest, "No data selector, path AND entity type found! Please make sure you supply either a valid JSON object or an ID of a valid selector, or a path + entity type.");
            }

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

            return await gclDataSelectorsService.GetJsonResponseAsync(data);
        }
    }
}
