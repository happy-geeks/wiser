using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Models;
using Api.Core.Services;
using Api.Modules.Branches.Interfaces;
using Api.Modules.Queries.Interfaces;
using Api.Modules.Queries.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using IdentityServer4.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Queries.Services
{
    /// <summary>
    /// Service for getting styled output for the Wiser modules.
    /// </summary>
    public class StyledOutputService : IStyledOutputService, IScopedService
    {
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IWiserItemsService wiserItemsService;
        private readonly IStringReplacementsService stringReplacementsService;
        private readonly IReplacementsMediator replacementsMediator;
        private readonly IQueriesService queriesService;
        private readonly IDatabaseHelpersService databaseHelpersService;
        private readonly ILogger<StyledOutputService> logger;
        private readonly StyledOutputSettings apiSettings;
        private readonly IBranchesService branchesService;
        
        // even if the user selects a higher value the results will always be capped to this ( filled in by settings file )
        private int maxResultsPerPage;

        private const string ItemSeparatorString = ", ";
        
		private static readonly string[] AllowedFormats = { "JSON" };
		private static readonly string[] AllowedSubFormats = { "JSON", "RAW" };
		
        /// <summary>
        /// Creates a new instance of <see cref="StyledOutputService"/>.
        /// </summary>
        public StyledOutputService(IOptions<StyledOutputSettings> apiSettings, IDatabaseConnection clientDatabaseConnection, IWiserItemsService wiserItemsService, IStringReplacementsService stringReplacementsService, IReplacementsMediator replacementsMediator, IQueriesService queriesService, IDatabaseHelpersService databaseHelpersService , ILogger<StyledOutputService> logger, IBranchesService branchesService)
        {
            this.clientDatabaseConnection = clientDatabaseConnection;
            this.apiSettings = apiSettings.Value;
            this.wiserItemsService = wiserItemsService;
            this.stringReplacementsService = stringReplacementsService;
            this.queriesService = queriesService;
            this.databaseHelpersService = databaseHelpersService;
            this.replacementsMediator = replacementsMediator;
            this.logger = logger;
            this.branchesService = branchesService;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<JToken>> GetStyledOutputResultJsonAsync(ClaimsIdentity identity, int id, List<KeyValuePair<string, object>> parameters, bool stripNewlinesAndTabs, int resultsPerPage, int page = 0, List<int> inUseStyleIds = null)
        {
            // Fetch max results per page ( can be overwritten by the user ).
            maxResultsPerPage = apiSettings.MaxResultsPerPage;

            var response = await GetStyledOutputResultAsync(identity, AllowedFormats, id, parameters, stripNewlinesAndTabs, resultsPerPage, page, inUseStyleIds);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                logger.LogError($"Non-OK response in GetStyledOutputResultJsonAsync: {response.ErrorMessage}");
                
                return new ServiceResult<JToken>
                {
                    StatusCode = response.StatusCode,
                    ErrorMessage = response.ErrorMessage
                };
            }

            if (response.ModelObject.Length == 0)
            {
                return new ServiceResult<JToken>
                {
                    StatusCode = HttpStatusCode.OK,
                    ModelObject = ""
                };
            }

            try
            {
                return new ServiceResult<JToken>
                {
                    StatusCode = HttpStatusCode.OK,
                    ModelObject = JToken.Parse(response.ModelObject)
                };
            }
            catch (Exception e)
            {
                var errorMsg = $"Wiser styled output with ID '{id}' could not convert to Json with exception: '{e}'";
                
                logger.LogError(errorMsg);

                return new ServiceResult<JToken>
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessage =  errorMsg
                };
            }
        }
		
		/// <summary>
		/// Private function for handling styled output elements.
		/// </summary>
		/// <param name="identity">The identity for the connection.</param>
		/// <param name="allowedFormats">The types that can be processed, for endpoint this is only JSON, sub elements also support RAW.</param>
		/// <param name="id">The ID of the starting point of the requested styled output.</param>
		/// <param name="parameters">The parameters send along to the database connection.</param>
		/// <param name="stripNewlinesAndTabs">If true fetched format strings will have their newlines and tabs removed.</param>
		/// <param name="page">The page number used in pagination-supported styled outputs.</param>
		/// <param name="resultsPerPage">The amount of results per page, will be capped at 500.</param>
		/// <param name="inUseStyleIds">Used for making sure no higher level styles are causing a cyclic reference in recursive calls, this can be left null.</param>
		/// <returns>Returns the updated string with replacements applied.</returns>
		private async Task<ServiceResult<string>> GetStyledOutputResultAsync(ClaimsIdentity identity, string[] allowedFormats, int id, List<KeyValuePair<string, object>> parameters, bool stripNewlinesAndTabs, int resultsPerPage, int page = 0, List<int> inUseStyleIds = null)
		{
            var usedIds = inUseStyleIds == null ? new List<int>() : new List<int>(inUseStyleIds);

            if (usedIds.Contains(id))
            {
                var errorMsg = $"Wiser Styled Output with ID '{id}' is part of a cyclic reference, ids in use: {usedIds}";
                
                logger.LogError(errorMsg);
                
                return new ServiceResult<string>
                {
                    StatusCode = HttpStatusCode.LoopDetected,
                    ErrorMessage = errorMsg
                };  
            }
            
            usedIds.Add(id);
            
            await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> { WiserTableNames.WiserStyledOutput });
            
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", id); 
            
            var formatQuery =  $"SELECT query_Id, format_begin, format_item, format_end, format_empty, return_type, options FROM {WiserTableNames.WiserStyledOutput} WHERE id = ?id";
            
            var dataTable = await clientDatabaseConnection.GetAsync(formatQuery);
            if (dataTable.Rows.Count == 0)
            {
                var errorMsg = $"Wiser Styled Output with ID '{id}' does not exist.";
                
                logger.LogError(errorMsg);
                
                return new ServiceResult<string>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = errorMsg
                };
            }
            
            var formatBeginValue = dataTable.Rows[0].Field<string>("format_begin");
            var formatItemValue = dataTable.Rows[0].Field<string>("format_item");
            var formatEndValue = dataTable.Rows[0].Field<string>("format_end");
            var formatEmptyValue = dataTable.Rows[0].Field<string>("format_empty");
            var formatExpectedReturnJson = dataTable.Rows[0].Field<string>("return_type");
            var formatQueryId = dataTable.Rows[0].Field<int>("query_id");
            var options = dataTable.Rows[0].Field<string>("options");

            if (stripNewlinesAndTabs)
            {
                formatBeginValue = formatBeginValue.Replace("\r\n","").Replace("\n","").Replace("\t","");
                formatItemValue = formatItemValue.Replace("\r\n","").Replace("\n","").Replace("\t","");
                formatEndValue = formatEndValue.Replace("\r\n","").Replace("\n","").Replace("\t","");
                formatEmptyValue = formatEmptyValue.Replace("\r\n","").Replace("\n","").Replace("\t","");
            }
            
            if (formatQueryId < 0)
            {
                var errorMsg = $"Wiser Styled Output with ID '{id}' does not have a valid query setup.";
                
                logger.LogError(errorMsg);
                
                return new ServiceResult<string>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = errorMsg
                };
            }

            if (!allowedFormats.Contains(formatExpectedReturnJson))
            {
                var errorMsg = $"Wiser Styled Output with ID '{id}' is not setup for JSON response";
                
                logger.LogError(errorMsg);
                
                return new ServiceResult<string>
                {
                    StatusCode = HttpStatusCode.NotImplemented,
                    ErrorMessage = errorMsg
                };             
            }

            if ((await wiserItemsService.GetUserQueryPermissionsAsync(formatQueryId, IdentityHelpers.GetWiserUserId(identity)) & AccessRights.Read) == AccessRights.Nothing)
            {
                var errorMsg = $"Wiser user '{IdentityHelpers.GetUserName(identity)}' has no permission to execute query '{formatQueryId}' for styled output '{id}' .";
                
                logger.LogError(errorMsg);
                
                return new ServiceResult<string>
                {
                    StatusCode = HttpStatusCode.Unauthorized,
                    ErrorMessage = errorMsg
                };
            }

            // This styled output has settings to parse.
            if (!String.IsNullOrWhiteSpace(options))
            {
                var optionsObject = JsonConvert.DeserializeObject<StyledOutputOptionModel>(options);
                
                if (optionsObject.MaxResultsPerPage > 0)
                {
                    maxResultsPerPage = optionsObject.MaxResultsPerPage;
                }
            }

            var query = await queriesService.GetAsync(identity, formatQueryId);
            
            clientDatabaseConnection.ClearParameters();

            var pageResultCount = Math.Min(maxResultsPerPage, resultsPerPage);

            var isMainBranch = await branchesService.IsMainBranchAsync(identity);
            
            clientDatabaseConnection.AddParameter(DatabaseHelpers.CreateValidParameterName("page"), page);
            clientDatabaseConnection.AddParameter(DatabaseHelpers.CreateValidParameterName("pageOffset"), page * pageResultCount);
            clientDatabaseConnection.AddParameter(DatabaseHelpers.CreateValidParameterName("resultsPerPage"), pageResultCount);
            clientDatabaseConnection.AddParameter(DatabaseHelpers.CreateValidParameterName("isMainBranch"), isMainBranch.ModelObject);
            
            parameters ??= new List<KeyValuePair<string, object>>();
         
            foreach (var parameter in parameters)
            {
                clientDatabaseConnection.AddParameter(DatabaseHelpers.CreateValidParameterName(parameter.Key), parameter.Value);
            }

            var combinedResult = new StringBuilder("");

            dataTable = await clientDatabaseConnection.GetAsync(query.ModelObject.Query);

            if (dataTable.Rows.Count == 0)
            {
                combinedResult.Append(formatEmptyValue);
            }
            else
            {
                var result = dataTable.ToJsonArray(skipNullValues: true);

                if (!formatBeginValue.IsNullOrEmpty())
                {
                    combinedResult.Append(formatBeginValue);
                }

                foreach (var parsedObject in result.Children<JObject>())
                {
                    // replace simple string info
                    var itemValue = stringReplacementsService.DoReplacements(formatItemValue, parsedObject);

                    // Replace if then else logic.
                    itemValue = replacementsMediator.EvaluateTemplate(itemValue);

                    // Replace recursive inline styles.
                    var inlineResult =  await HandleInlineStyleElementsAsync(identity, itemValue, parameters, stripNewlinesAndTabs, resultsPerPage, page, usedIds);
                    
                    if (inlineResult.StatusCode == HttpStatusCode.OK)
                    {
                        itemValue = inlineResult.ModelObject;
                    }
                    else
                    {
                        // Relate error if something went wrong.
                        return inlineResult;
                    }

                    combinedResult.Append(itemValue);

                    if (parsedObject != result.Children<JObject>().Last())
                    {
                        combinedResult.Append(ItemSeparatorString);
                    }
                }

                if (!formatEndValue.IsNullOrEmpty())
                {
                    combinedResult.Append(formatEndValue);
                }
            }

            return new ServiceResult<string>
            {
                StatusCode = HttpStatusCode.OK,
                ModelObject = combinedResult.ToString()
            };
         }
        
        /// <summary>
        /// Private function for handling inline style element replacements.
        /// </summary>
        /// <param name="identity">The identity for the connection.</param>
        /// <param name="itemValue">The item format value that will get its inline element replaced if present.</param>
        /// <param name="parameters">The parameters send along to the database connection.</param>
        /// <param name="stripNewlinesAndTabs">If true fetched format strings will have their newlines and tabs removed.</param>
        /// <param name="page">The page number used in pagination-supported styled outputs.</param>
        /// <param name="resultsPerPage">The amount of results per page, will be capped at 500.</param>
        /// <param name="inUseStyleIds">Used for making sure no higher level styles are causing a cyclic reference in recursive calls, this can be left null.</param>
        /// <returns>Returns the updated string with replacements applied.</returns>
        private async Task<ServiceResult<string>> HandleInlineStyleElementsAsync(ClaimsIdentity identity, string itemValue, List<KeyValuePair<string, object>> parameters, bool stripNewlinesAndTabs, int resultsPerPage, int page, List<int> inUseStyleIds = null)
        {
            var index = 0;
            
            while (index < itemValue.Length)
            {
                var startIndex = itemValue.IndexOf("{StyledOutput", index, StringComparison.OrdinalIgnoreCase);
                index = startIndex + 1;
                    
                if (index <= 0)
                {
                    // No further replacements needed.
                    break;
                }
                    
                var endIndex = itemValue.IndexOf("}", startIndex, StringComparison.OrdinalIgnoreCase) + 1;
                    
                if (endIndex <= 0)
                {
                    // Error, can't find end of styled object.
                    break;
                }
                    
                var styleString = itemValue.Substring(startIndex, endIndex - startIndex);
                var sections = styleString.Substring(1,styleString.Length - 2).Split('~');
                var subStyleId = sections.Length > 1 ? Int32.Parse(sections[1]) : - 1;

                if (subStyleId < 0)
                {
                    continue;
                }
                
                var subParameters = new List<KeyValuePair<string, object>>();
                subParameters.AddRange(parameters);

                // Note: skip 0,1 since this is the inline element itself and its value then move per 2 'key' and 'value'.
                for (var i = 2; i < sections.Length - 1; i += 2)
                {
                    subParameters.Add(new KeyValuePair<string, object>(sections[i], sections[i + 1]));
                }
                        
                var subResult = await GetStyledOutputResultAsync(identity, AllowedSubFormats, subStyleId, subParameters, stripNewlinesAndTabs, resultsPerPage, page, inUseStyleIds);
                    
                if (subResult.StatusCode == HttpStatusCode.OK)
                {
                    itemValue = itemValue.Replace(styleString, subResult.ModelObject);
                }
                else
                {
                    // Something went wrong, return the error from the sub-query.
                    return subResult;
                }
            }
            
            return new ServiceResult<string>
            {
                StatusCode = HttpStatusCode.OK,
                ModelObject = itemValue
            };          
        }
    }
}
