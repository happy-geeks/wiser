using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Services;
using Api.Modules.Customers.Interfaces;
using Api.Modules.Queries.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using IdentityServer4.Extensions;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json.Linq;
using Task = DocumentFormat.OpenXml.Office2021.DocumentTasks.Task;

namespace Api.Modules.Queries.Services
{
    /// <summary>
    /// Service for getting styled output for the Wiser modules.
    /// </summary>
    public class StyledOutputService : IStyledOutputService, IScopedService
    {
        private readonly IWiserCustomersService wiserCustomersService;
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IWiserItemsService wiserItemsService;
        private readonly IStringReplacementsService stringReplacementsService;
        private readonly IReplacementsMediator replacementsMediator;
        private readonly IQueriesService queriesService;
        private readonly IDatabaseHelpersService databaseHelpersService;

        // results per page when a styled output supports pagination 
        private const int resultsDefaultPerPage = 500;
        
        // even if the user selects a higher value the results will always be capped to this
        private const int maxResultsPerPage = 500; 

        /// <summary>
        /// Creates a new instance of <see cref="StyledOutputService"/>.
        /// </summary>
        public StyledOutputService(IWiserCustomersService wiserCustomersService,
            IDatabaseConnection clientDatabaseConnection, IWiserItemsService wiserItemsService, IStringReplacementsService stringReplacementsService, IReplacementsMediator replacementsMediator, IQueriesService queriesService, IDatabaseHelpersService databaseHelpersService)
        {
            this.wiserCustomersService = wiserCustomersService;
            this.clientDatabaseConnection = clientDatabaseConnection;
            this.wiserItemsService = wiserItemsService;
            this.stringReplacementsService = stringReplacementsService;
            this.queriesService = queriesService;
            this.databaseHelpersService = databaseHelpersService;
            this.replacementsMediator = replacementsMediator;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<JToken>> GetStyledOutputResultJsonAsync(ClaimsIdentity identity, int id, List<KeyValuePair<string, object>> parameters, bool stripNewlinesAndTabs, int page = 0, int resultsPerPage = 0, List<int> inUseStyleIds = null)
        {
            var usedIds = inUseStyleIds == null ? new List<int>() : new List<int>(inUseStyleIds);

            if (usedIds.Contains(id))
            {
                return new ServiceResult<JToken>
                {
                    StatusCode = HttpStatusCode.LoopDetected,
                    ErrorMessage = $"Wiser Styled Output with ID '{id}' is part of a cyclic reference, ids in use: {usedIds}"
                };  
            }
            
            usedIds.Add(id);
            
            await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> { WiserTableNames.WiserStyledOutput });
            
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", id); 
            
            var formatQuery =  $"SELECT query_Id, format_begin, format_item, format_end, return_type FROM {WiserTableNames.WiserStyledOutput} WHERE id = ?id";
            
            var dataTable = await clientDatabaseConnection.GetAsync(formatQuery);
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<JToken>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = $"Wiser Styled Output with ID '{id}' does not exist."
                };
            }
            
            var formatBeginValue = "";
            var formatItemValue = "";
            var formatEndValue = "";
            var formatExpectedReturnJson = "";
            int formatQueryId = -1;

            if (dataTable.Rows.Count != 0)
            {
                formatBeginValue = dataTable.Rows[0].Field<string>("format_begin");
                formatItemValue = dataTable.Rows[0].Field<string>("format_item");
                formatEndValue = dataTable.Rows[0].Field<string>("format_end");
                formatExpectedReturnJson = dataTable.Rows[0].Field<string>("return_type");

                if (stripNewlinesAndTabs)
                {
                    formatBeginValue = formatBeginValue.Replace("\r\n","").Replace("\n","").Replace("\t","");
                    formatItemValue = formatItemValue.Replace("\r\n","").Replace("\n","").Replace("\t","");
                    formatEndValue = formatEndValue.Replace("\r\n","").Replace("\n","").Replace("\t","");
                }
                
                formatQueryId = dataTable.Rows[0].Field<int>("query_id");
            }
            
            if (formatQueryId < 0)
            {
                return new ServiceResult<JToken>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = $"Wiser Styled Output with ID '{id}' does not have a valid query setup."
                };
            }

            if (formatExpectedReturnJson != "JSON")
            {
                return new ServiceResult<JToken>
                {
                    StatusCode = HttpStatusCode.NotImplemented,
                    ErrorMessage = $"Wiser Styled Output with ID '{id}' is not setup for JSON response"
                };             
            }

            if ((await wiserItemsService.GetUserQueryPermissionsAsync(formatQueryId, IdentityHelpers.GetWiserUserId(identity)) &
                 AccessRights.Read) == AccessRights.Nothing)
            {
                return new ServiceResult<JToken>
                {
                    StatusCode = HttpStatusCode.Unauthorized,
                    ErrorMessage =
                        $"Wiser user '{IdentityHelpers.GetUserName(identity)}' has no permission to execute query '{formatQueryId}' for styled output '{id}' ."
                };
            }

            var query = await queriesService.GetAsync(identity, formatQueryId);
            
            clientDatabaseConnection.ClearParameters();

            int pageResultCount = resultsPerPage > 0 ? resultsPerPage : resultsDefaultPerPage;
            pageResultCount = pageResultCount > maxResultsPerPage ? maxResultsPerPage : pageResultCount;
            
            clientDatabaseConnection.AddParameter(DatabaseHelpers.CreateValidParameterName("page"), page);
            clientDatabaseConnection.AddParameter(DatabaseHelpers.CreateValidParameterName("pageOffset"), page * pageResultCount);
            clientDatabaseConnection.AddParameter(DatabaseHelpers.CreateValidParameterName("resultsPerPage"), pageResultCount);
            
            parameters ??= new List<KeyValuePair<string, object>>(parameters);
         
            foreach (var parameter in parameters)
            {
                clientDatabaseConnection.AddParameter(DatabaseHelpers.CreateValidParameterName(parameter.Key),
                    parameter.Value);
            }
            
            dataTable = await clientDatabaseConnection.GetAsync(query.ModelObject.Query);
            var result = dataTable.Rows.Count == 0 ? new JArray() : dataTable.ToJsonArray(skipNullValues: true);

            string combinedResult = "";
            
            if (!formatBeginValue.IsNullOrEmpty())
            {
                combinedResult += formatBeginValue;
            }

            foreach (JObject parsedObject in result.Children<JObject>())
            {
                // replace simple string info
                var itemValue = stringReplacementsService.DoReplacements(formatItemValue, parsedObject);

                // replace if then else logic
                itemValue = replacementsMediator.EvaluateTemplate(itemValue);
                
                // replace recursive inline styles
                itemValue = await HandleInlineStyleElements(identity, itemValue, parameters, stripNewlinesAndTabs, page, resultsPerPage, usedIds);
  
                combinedResult += itemValue;

                if (parsedObject != result.Children<JObject>().Last())
                {
                    combinedResult += ", ";
                }
            }
            
            if (!formatEndValue.IsNullOrEmpty())
            {
                combinedResult += formatEndValue;
            }

            JToken parsedJson = "";
            
            try
            {
                parsedJson = JToken.Parse(combinedResult);
            }
            catch (Exception e)
            {
                return new ServiceResult<JToken>
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessage = $"Wiser styledoutput with ID '{formatQueryId}' could not convert to Json with exception: '{e.ToString()}'"
                };
            }

            return new ServiceResult<JToken>(parsedJson);
        }
        
        /// <summary>
        /// private function for handling inline style element replacements
        /// </summary>
        /// <param name="identity">The identity for the connection </param>
        /// <param name="itemValue">The item format value that will get its inline element replaced if present.</param>
        /// <param name="parameters">The parameters send along to the database connection .</param>
        /// <param name="stripNewlinesAndTabs">if true fetched format strings will have their newlines and tabs removed</param>
        /// <param name="inUseStyleIds">used for making sure no higher level styles are causing a cyclic reference in recursive calls, this can be left null/param>
        /// <returns>Returns the updated string with replacements applied</returns>
        private async Task<string> HandleInlineStyleElements(ClaimsIdentity identity, string itemValue, List<KeyValuePair<string, object>> parameters, bool stripNewlinesAndTabs, int page, int resultsPerPage, List<int> inUseStyleIds = null)
        {
            var index = 0;
            
            while (index < itemValue.Length && index >= 0)
            {
                var startIndex = itemValue.IndexOf("{StyledOutput", index);
                index = startIndex + 1;
                    
                if (index <= 0)
                {
                    // no further replacements needed
                    break;
                }
                    
                var endIndex = itemValue.IndexOf("}", startIndex) + 1;
                    
                if (endIndex <= 0)
                {
                    // error, can't find end of styled object
                    break;
                }
                    
                var styleString = itemValue.Substring(startIndex, endIndex - startIndex);
                var sections = styleString.Substring(1,styleString.Length - 2).Split('~');
                var subStyleId = sections.Length > 1 ? int.Parse(sections[1]) : - 1;

                if (subStyleId >= 0)
                {
                    var subParameters = new List<KeyValuePair<string, object>>();
                    subParameters.AddRange(parameters);

                    // note: skip 0,1 since this is the inline element itself and its value then move per 2 'key' and 'value'
                    for (int i = 2; i < sections.Length - 1; i += 2)
                    {
                        subParameters.Add(new KeyValuePair<string, object>(
                            sections[i],
                            sections[i + 1]
                        ));
                    }
                        
                    var subResult = await GetStyledOutputResultJsonAsync(identity, subStyleId, subParameters, stripNewlinesAndTabs, page, resultsPerPage, inUseStyleIds);
                    
                    if (subResult.StatusCode == HttpStatusCode.OK)
                    {
                        itemValue = itemValue.Replace(styleString, subResult.ModelObject.ToString());
                    }
                }
            }
            return itemValue;
        }
    }
}
