using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Models;
using Api.Core.Services;
using Api.Modules.Branches.Interfaces;
using Api.Modules.Queries.Exceptions;
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Queries.Services;

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

    private bool performanceLogging = false;
    private const string ItemSeparatorString = ", ";

    private static readonly string[] AllowedFormats = ["JSON"];
    private static readonly string[] AllowedSubFormats = ["JSON", "RAW"];

    private readonly Dictionary<int, StyledOutputModel> cachedStyles = new Dictionary<int, StyledOutputModel>();
    private readonly Dictionary<int, string> cachedQueries = new Dictionary<int, string>();
    private readonly List<int> cachedQueryPermission = [];

    private readonly Dictionary<int, List<Stopwatch>> timings = new Dictionary<int, List<Stopwatch>>();

    private readonly Dictionary<StyledOutputBuildIn, Func<string[], StyledOutputBuildIn, Task<string>>> styleParsers =
        new Dictionary<StyledOutputBuildIn, Func<string[], StyledOutputBuildIn, Task<string>>>();

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

        // Initialize the style parsers.
        styleParsers.Add(StyledOutputConstants.SingleDetail,ParseSingleResult);
        styleParsers.Add(StyledOutputConstants.SingleDetailArrayElm, ParseSingleResult);
        styleParsers.Add(StyledOutputConstants.LanguageDetail, ParseSingleResult);
        styleParsers.Add(StyledOutputConstants.MultiDetail, ParseMutliresults);
        styleParsers.Add(StyledOutputConstants.Singlelinked, ParseSingleResult);
        styleParsers.Add(StyledOutputConstants.SinglelinkedArrayElm, ParseSingleResult);
    }

    /// <inheritdoc />
    public async Task<int> GetStyledOutputIdFromNameAsync(string name)
    {
        var formatQuery = $"SELECT id FROM `wiser_styled_output` WHERE `name` = {name.ToMySqlSafeValue(true)} LIMIT 1";

        var dataTable = await clientDatabaseConnection.GetAsync(formatQuery);

        if (dataTable.Rows.Count == 0)
        {
            return -1;
        }

        return dataTable.Rows[0].Field<int>("id");
    }

    /// <inheritdoc />
    public async Task<ServiceResult<JToken>> GetStyledOutputResultJsonAsync(ClaimsIdentity identity, int id, List<KeyValuePair<string, object>> parameters, bool stripNewlinesAndTabs, int resultsPerPage, int page = 0, List<int> inUseStyleIds = null)
    {
        // Fetch max results per page ( can be overwritten by the user ).
        maxResultsPerPage = apiSettings.MaxResultsPerPage;

        await databaseHelpersService.CheckAndUpdateTablesAsync([WiserTableNames.WiserStyledOutput]);

        ClearCachedStyles();

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

    private bool CheckForInUseIds(List<int> inUseStyleIds, int id, out string errorMsg)
    {
        errorMsg = "";

        if (inUseStyleIds == null)
        {
            return false;
        }

        if (!inUseStyleIds.Contains(id)) return false;

        errorMsg = $"Wiser Styled Output with ID '{id}' is part of a cyclic reference, ids in use: {inUseStyleIds}";
        logger.LogError(errorMsg);

        return true;
    }

    private StyledOutputModel StripNewlinesAndTabsOnStyle(StyledOutputModel input)
    {
        return new StyledOutputModel
        {
            FormatBegin = input.FormatBegin.Replace("\r\n","").Replace("\n","").Replace("\t",""),
            FormatItem = input.FormatItem.Replace("\r\n","").Replace("\n","").Replace("\t",""),
            FormatEnd = input.FormatEnd.Replace("\r\n","").Replace("\n","").Replace("\t",""),
            FormatEmpty = input.FormatEmpty.Replace("\r\n","").Replace("\n","").Replace("\t","")
        };
    }

    /// <summary>
    /// Private function for handling styled output elements.
    /// </summary>
    /// <param name="identity">The identity for the connection.</param>
    /// <param name="allowedFormats">The types that can be processed, for endpoint this is only JSON, sub elements also support RAW.</param>
    /// <param name="id">The ID of the starting point of the requested styled output.</param>
    /// <param name="parameters">The parameters send along to the database connection.</param>
    /// <param name="stripNewlinesAndTabs">If true fetched format strings will have their newlines and tabs removed.</param>
    /// <param name="resultsPerPage">The amount of results per page, will be capped at 500.</param>
    /// <param name="page">The page number used in pagination-supported styled outputs.</param>
    /// <param name="inUseStyleIds">Used for making sure no higher level styles are causing a cyclic reference in recursive calls, this can be left null.</param>
    /// <param name="callingParentId">calling parent is the id of styledoutput calling the styledoutput we are calling now, for users this can always be -1 indicating it has no parent.</param>
    /// <returns>Returns the updated string with replacements applied.</returns>
    private async Task<ServiceResult<string>> GetStyledOutputResultAsync(ClaimsIdentity identity, string[] allowedFormats, int id, List<KeyValuePair<string, object>> parameters, bool stripNewlinesAndTabs, int resultsPerPage, int page = 0, List<int> inUseStyleIds = null, int callingParentId = -1)
    {
        var usedIds = inUseStyleIds == null ? new List<int>() : new List<int>(inUseStyleIds);

        if (CheckForInUseIds(usedIds, id, out var idErrorMsg))
        {
            return new ServiceResult<string>
            {
                StatusCode = HttpStatusCode.LoopDetected,
                ErrorMessage = idErrorMsg
            };
        }

        usedIds.Add(id);

        StyledOutputModel style;

        try
        {
            style = await GetCachedStyleAsync(id);
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occurred while getting a cached style.");

            return new ServiceResult<string>
            {
                StatusCode = HttpStatusCode.NotFound,
                ErrorMessage = e.Message
            };
        }

        if (stripNewlinesAndTabs)
        {
            style = StripNewlinesAndTabsOnStyle(style);
        }

        if (!allowedFormats.Contains(style.ReturnType))
        {
            var errorMsg = $"Wiser Styled Output with ID '{id}' is not setup for JSON response";

            logger.LogError(errorMsg);

            return new ServiceResult<string>
            {
                StatusCode = HttpStatusCode.NotImplemented,
                ErrorMessage = errorMsg
            };
        }

        try
        {
            var isAllowed = await QueryIsAllowedAsync(style.QueryId, identity);
            if (!isAllowed)
            {
                var errorMsg =
                    $"Wiser user '{IdentityHelpers.GetUserName(identity)}' has no permission to execute query '{style.QueryId}'";
                logger.LogError(errorMsg);

                return new ServiceResult<string>
                {
                    StatusCode = HttpStatusCode.Unauthorized,
                    ErrorMessage = errorMsg
                };
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occurred while checking if a query is allowed to be executed.");

            return new ServiceResult<string>
            {
                StatusCode = HttpStatusCode.Unauthorized,
                ErrorMessage = e.Message
            };
        }

        // This styled output has settings to parse.
        if (!String.IsNullOrWhiteSpace(style.Options))
        {
            var optionsObject = JsonConvert.DeserializeObject<StyledOutputOptionModel>(style.Options);

            if (optionsObject.MaxResultsPerPage > 0)
            {
                maxResultsPerPage = optionsObject.MaxResultsPerPage;
            }

            performanceLogging = optionsObject.LogTiming;
        }

        var query = await GetCachedQueryAsync(style.QueryId, identity);
        var combinedResult = new StringBuilder("");
        JArray result = null;

        if (!String.IsNullOrEmpty(query))
        {
            clientDatabaseConnection.ClearParameters();

            var pageResultCount = Math.Min(maxResultsPerPage, resultsPerPage);

            var isMainBranch = await branchesService.IsMainBranchAsync(identity);

            clientDatabaseConnection.AddParameter(DatabaseHelpers.CreateValidParameterName("page"), page);
            clientDatabaseConnection.AddParameter(DatabaseHelpers.CreateValidParameterName("pageOffset"),
                page * pageResultCount);
            clientDatabaseConnection.AddParameter(DatabaseHelpers.CreateValidParameterName("resultsPerPage"),
                pageResultCount);
            clientDatabaseConnection.AddParameter(DatabaseHelpers.CreateValidParameterName("isMainBranch"),
                isMainBranch.ModelObject);

            parameters ??= [];

            foreach (var parameter in parameters)
            {
                clientDatabaseConnection.AddParameter(DatabaseHelpers.CreateValidParameterName(parameter.Key),
                    parameter.Value);
            }

            if (performanceLogging)
            {
                if (callingParentId < 0)
                {
                    timings.Clear();
                }

                if (!timings.ContainsKey(id))
                {
                    timings.Add(id, []);
                }

                timings[id].Add(new Stopwatch());
                timings[id].Last().Start();
            }

            var dataTable = await clientDatabaseConnection.GetAsync(query);
            result = dataTable.ToJsonArray(skipNullValues: true);
        }
        else
        {
            result = new JArray(
                new JObject
                {
                    ["defaultEntry"] = 1,
                });
        }

        if (result.Count == 0)
        {
            combinedResult.Append(style.FormatEmpty);
        }
        else
        {
            if (!String.IsNullOrEmpty(style.FormatBegin))
            {
                var formattedBegin = stringReplacementsService.DoReplacements(style.FormatBegin, result);
                combinedResult.Append(formattedBegin);
            }

            foreach (var parsedObject in result.Children<JObject>())
            {
                // replace simple string info
                var itemValue = stringReplacementsService.DoReplacements(style.FormatItem, parsedObject);

                // Replace if then else logic.
                itemValue = replacementsMediator.EvaluateTemplate(itemValue);

                // Replace recursive inline styles.
                var inlineResult =  await HandleInlineStyleElementsAsync(identity, itemValue, parameters, stripNewlinesAndTabs, resultsPerPage, page, usedIds, id);

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

            if (!String.IsNullOrWhiteSpace(style.FormatEnd))
            {
                var formattedEnd = stringReplacementsService.DoReplacements(style.FormatEnd, result);
                combinedResult.Append(formattedEnd);
            }
        }

        if (performanceLogging)
        {
            timings[id].Last().Stop();

            if (callingParentId < 0)
            {
                for (int i = 0; i < timings.Keys.Count; ++i)
                {
                    var key = timings.Keys.ElementAt(i);
                    var runCount = timings[key].Count;
                    var totalTime = 0.0;

                    foreach (var entry in timings[key])
                    {
                        totalTime += entry.ElapsedMilliseconds / 1000.0;
                    }

                    var avarageTime = totalTime / runCount;

                    var timingStoryQuery =
                        $"""UPDATE wiser_styled_output SET log_average_runtime = "{avarageTime.ToString("0.00000", CultureInfo.InvariantCulture)}", log_run_count = "{runCount}" WHERE id="{key}";""";

                    await clientDatabaseConnection.ExecuteAsync(timingStoryQuery);
                }
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
    /// <param name="resultsPerPage">The amount of results per page, will be capped at 500.</param>
    /// <param name="page">The page number used in pagination-supported styled outputs.</param>
    /// <param name="inUseStyleIds">Used for making sure no higher level styles are causing a cyclic reference in recursive calls, this can be left null.</param>
    /// <param name="callingParentId">calling parent is the id of styledoutput calling the styledoutput we are calling now, for users this can always be -1 indicating it has no parent.</param>
    /// <returns>Returns the updated string with replacements applied.</returns>
        private async Task<ServiceResult<string>> HandleInlineStyleElementsAsync(ClaimsIdentity identity, string itemValue, List<KeyValuePair<string, object>> parameters, bool stripNewlinesAndTabs, int resultsPerPage, int page, List<int> inUseStyleIds = null, int callingParentId = -1)
    {
        var index = 0;

        while (index < itemValue.Length)
        {
            var startIndex = itemValue.IndexOf(StyledOutputConstants.DefaultStartKeyWord, index, StringComparison.OrdinalIgnoreCase);
            index = startIndex + 1;

            if (index <= 0)
            {
                // No further replacements needed.
                break;
            }

            var endIndex = itemValue.IndexOf(StyledOutputConstants.DefaultEndKeyWord, startIndex, StringComparison.OrdinalIgnoreCase) + 1;

            if (endIndex <= 0)
            {
                // Error, can't find end of styled object.
                break;
            }

            var styleString = itemValue.Substring(startIndex, endIndex - startIndex);
            var sections = styleString.Substring(1,styleString.Length - 2).Split('~');

            var parser = styleParsers.FirstOrDefault(item => item.Key.Key == sections[0]);

            if (parser.Key != null)
            {
                 itemValue = await parser.Value(sections, parser.Key);
            }
            else
            {
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

                var subResult = await GetStyledOutputResultAsync(identity, AllowedSubFormats, subStyleId, subParameters, stripNewlinesAndTabs, resultsPerPage, page, inUseStyleIds, callingParentId);
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
        }

        return new ServiceResult<string>
        {
            StatusCode = HttpStatusCode.OK,
            ModelObject = itemValue
        };
    }

    /// <summary>
    /// Private function for clearing the various caches.
    /// </summary>
    private void ClearCachedStyles()
    {
        cachedStyles.Clear();
        cachedQueries.Clear();
        cachedQueryPermission.Clear();
    }

    /// <summary>
    /// Private function for fetching a styled output from the cache or when not cached adding it to the cache.
    /// </summary>
    /// <param name="id">The ID of the styled sheet in question.</param>
    /// <returns>Returns <see cref="StyledOutputModel"/> of the requested id.</returns>
    private async Task<StyledOutputModel> GetCachedStyleAsync(int id)
    {
        if (cachedStyles.TryGetValue(id, out var style))
        {
            return style;
        }

        return await AddCachedStyleAsync(id);
    }

    /// <summary>
    /// Private function adding a styled output to the cache.
    /// </summary>
    /// <param name="id">The ID of the styled sheet in question.</param>
    /// <returns>Returns <see cref="StyledOutputModel"/> of the added id.</returns>
    private async Task<StyledOutputModel> AddCachedStyleAsync(int id)
    {
        cachedStyles.Remove(id);

        StyledOutputModel style = new StyledOutputModel();

        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
        clientDatabaseConnection.ClearParameters();
        clientDatabaseConnection.AddParameter("id", id);

        var formatQuery =  $"SELECT query_Id, format_begin, format_item, format_end, format_empty, return_type, options FROM {WiserTableNames.WiserStyledOutput} WHERE id = ?id";

        var dataTable = await clientDatabaseConnection.GetAsync(formatQuery);
        if (dataTable.Rows.Count == 0)
        {
            var errorMsg = $"Wiser Styled Output with ID '{id}' does not exist.";
            logger.LogError(errorMsg);
            throw new KeyNotFoundException(errorMsg);
        }

        style.FormatBegin = dataTable.Rows[0].Field<string>("format_begin");
        style.FormatItem =  dataTable.Rows[0].Field<string>("format_item");
        style.FormatEnd = dataTable.Rows[0].Field<string>("format_end");
        style.FormatEmpty = dataTable.Rows[0].Field<string>("format_empty");
        style.ReturnType = dataTable.Rows[0].Field<string>("return_type");
        style.QueryId = dataTable.Rows[0].Field<int?>("query_id") ?? -1;
        style.Options = dataTable.Rows[0].Field<string>("options");

        cachedStyles.Add(id,style);

        return style;
    }

    /// <summary>
    /// Private function to check fi a query is allowed to be run, uses a cache to optimize requests.
    /// </summary>
    /// <param name="queryId">The ID of the query.</param>
    /// <param name="identity">The identity for the connection.</param>
    /// <returns>Returns true or throws an exception when not allowed.</returns>
    private async Task<bool> QueryIsAllowedAsync(int queryId, ClaimsIdentity identity)
    {
        if (queryId < 0)
        {
            // No query needed, return true.
            return true;
        }

        if (cachedQueryPermission.Contains(queryId))
        {
            return true;
        }

        if ((await wiserItemsService.GetUserQueryPermissionsAsync(queryId, IdentityHelpers.GetWiserUserId(identity)) & AccessRights.Read) == AccessRights.Nothing)
        {

            var errorMsg = $"Wiser user '{IdentityHelpers.GetUserName(identity)}' has no permission to execute query '{queryId}'";

            logger.LogError(errorMsg);
            throw new UnauthorizedAccessException(errorMsg);
        }

        cachedQueryPermission.Add(queryId);

        return true;
    }

    /// <summary>
    /// Private function for fetching a cached query or add it when missing.
    /// </summary>
    /// <param name="queryId">The ID of the query.</param>
    /// <param name="identity">The identity for the connection.</param>
    /// <returns>Returns the query string.</returns>
    private async Task<string> GetCachedQueryAsync(int queryId, ClaimsIdentity identity)
    {
        if ( queryId < 0)
        {
            // No query needed, return empty string.
            return "";
        }

        if (cachedQueries.TryGetValue(queryId, out var query))
        {
            return query;
        }

        return await AddCachedQueryAsync(queryId, identity);
    }

    /// <summary>
    /// Private function for addin a query to the cache.
    /// </summary>
    /// <param name="queryId">The ID of the query.</param>
    /// <param name="identity">The identity for the connection.</param>
    /// <returns>Returns the added query string.</returns>
    private async Task<string> AddCachedQueryAsync(int queryId, ClaimsIdentity identity)
    {
        cachedQueries.Remove(queryId);

        var query = await queriesService.GetAsync(identity, queryId);
        cachedQueries.Add(queryId, query.ModelObject.Query);

        return query.ModelObject.Query;
    }

    ///<inheritdoc />
    public async Task<Dictionary<ulong,string>> GetMultiStyledOutputResultsAsync(ClaimsIdentity identity, string[] allowedFormats, int id, List<ulong> itemIds, List<KeyValuePair<string, object>> parameters)
    {
        var usedIds = new List<int>();

        if (usedIds.Contains(id))
        {
            throw new StyledOutputCyclicReferenceException($"Wiser Styled Output with ID '{id}' is part of a cyclic reference, ids in use: {usedIds}");
        }

        usedIds.Add(id);

        StyledOutputModel style;

        try
        {
            style = await GetCachedStyleAsync(id);
        }
        catch (KeyNotFoundException e)
        {
            throw new KeyNotFoundException($"Wiser Styled Output with ID '{id}' does not exist.", e);
        }

        if (!allowedFormats.Contains(style.ReturnType))
        {
            throw new StyledOutputNoValidJsonSetupException($"Wiser Styled Output with ID '{id}' is not setup for JSON response.");
        }

        try
        {
            var isAllowed = await QueryIsAllowedAsync(style.QueryId, identity);
            if (!isAllowed)
            {
                var errorMsg = $"Wiser user '{IdentityHelpers.GetUserName(identity)}' has no permission to execute query '{style.QueryId}'";
                logger.LogError(errorMsg);
                throw new StyledOutputNotAllowedException(errorMsg);
            }
        }
        catch (UnauthorizedAccessException e)
        {
            var errorMsg = $"Wiser user '{IdentityHelpers.GetUserName(identity)}' has no permission to execute query '{style.QueryId}'";
            logger.LogError(errorMsg,e);
            throw;
        }

        // This styled output has settings to parse.
        if (!String.IsNullOrWhiteSpace(style.Options))
        {
            var optionsObject = JsonConvert.DeserializeObject<StyledOutputOptionModel>(style.Options);

            if (optionsObject.MaxResultsPerPage > 0)
            {
                maxResultsPerPage = optionsObject.MaxResultsPerPage;
            }

            performanceLogging = optionsObject.LogTiming;
        }

        var query = await GetCachedQueryAsync(style.QueryId, identity);

        clientDatabaseConnection.ClearParameters();

        var isMainBranch = await branchesService.IsMainBranchAsync(identity);
        clientDatabaseConnection.AddParameter("isMainBranch", isMainBranch.ModelObject);

        parameters ??= [];

        foreach (var parameter in parameters)
        {
            clientDatabaseConnection.AddParameter(DatabaseHelpers.CreateValidParameterName(parameter.Key), parameter.Value);
        }

        var results = new Dictionary<ulong, string>();

        query = query.Replace("{ids}", "(" + string.Join(",", itemIds) + ")");

        var dataTable = await clientDatabaseConnection.GetAsync(query);
        var result = dataTable.ToJsonArray(skipNullValues: true);

        for (int i = 0; i < dataTable.Rows.Count; i++)
        {
            var builder = new StringBuilder();
            if (!String.IsNullOrEmpty(style.FormatBegin))
            {
                var formattedBegin = stringReplacementsService.DoReplacements(style.FormatBegin, result[i]);
                builder.Append(formattedBegin);
            }

            // replace simple string info
            var itemValue = stringReplacementsService.DoReplacements(style.FormatItem, result[i]);

            // Replace if then else logic.
            itemValue = replacementsMediator.EvaluateTemplate(itemValue);

            // Replace recursive inline styles.
            var inlineResult = await HandleInlineStyleElementsAsync(identity, itemValue, parameters, false, 500, 0, usedIds, id);

            if (inlineResult.StatusCode == HttpStatusCode.OK)
            {
                itemValue = inlineResult.ModelObject;
            }

            builder.Append(itemValue);

            if (i != dataTable.Rows.Count - 1)
            {
                builder.Append(ItemSeparatorString);
            }


            if (!String.IsNullOrWhiteSpace(style.FormatEnd))
            {
                var formattedEnd = stringReplacementsService.DoReplacements(style.FormatEnd, result[i]);
                builder.Append(formattedEnd);
            }

            results.Add(dataTable.Rows[i].Field<ulong>("inputId"), builder.ToString());
        }

        return results;
    }

    /// <summary>
    /// Private function for handling results that only have a single row (so no need for stringing values together).
    /// </summary>
    /// <param name="inputSections">The ID of the query.</param>
    /// <param name="buildIn">The identity for the connection.</param>
    /// <returns>Returns the formatted string according to the results.</returns>
    private async Task<string> ParseSingleResult(string[] inputSections, StyledOutputBuildIn buildIn)
    {
        if (inputSections.Length != 4)
        {
            throw new StyledOutputInvalidStyleOutputpException($"Wiser Styled Output has an invalid {buildIn.Key} element");
        }

        clientDatabaseConnection.ClearParameters();

        clientDatabaseConnection.AddParameter("styled_name", inputSections[3]);
        clientDatabaseConnection.AddParameter("styled_key", inputSections[2]);
        clientDatabaseConnection.AddParameter("styled_id", inputSections[1]);

        var dataTable = await clientDatabaseConnection.GetAsync(buildIn.Query);

        if (dataTable.Rows.Count == 0)
        {
           return "";
        }

        var result = dataTable.ToJsonArray(skipNullValues: true);
        return stringReplacementsService.DoReplacements(buildIn.UnitLayout, result);
    }

    /// <summary>
    /// Private function for handling results that might have multiple rows to process.
    /// </summary>
    /// <param name="inputSections">The ID of the query.</param>
    /// <param name="buildIn">The identity for the connection.</param>
    /// <returns>Returns the formatted string according to the results.</returns>
    private async Task<string> ParseMutliresults(string[] inputSections, StyledOutputBuildIn buildIn)
    {
        if (inputSections.Length != 4)
        {
            throw new StyledOutputInvalidStyleOutputpException($"Wiser Styled Output has an invalid {buildIn.Key} element");
        }

        clientDatabaseConnection.ClearParameters();

        clientDatabaseConnection.AddParameter("styled_name", inputSections[3]);
        clientDatabaseConnection.AddParameter("styled_key", inputSections[2]);
        clientDatabaseConnection.AddParameter("styled_id", inputSections[1]);

        var dataTable = await clientDatabaseConnection.GetAsync(buildIn.Query);

        if (dataTable.Rows.Count == 0)
        {
            return "";
        }

        var result = dataTable.ToJsonArray(skipNullValues: true);

        var returnValue = stringReplacementsService.DoReplacements(buildIn.BeginLayout, result);

        for (int i = 0; i < result.Count; i++)
        {
            var itemValue = stringReplacementsService.DoReplacements(buildIn.UnitLayout, result[i]);
            returnValue += itemValue;

            if (i != result.Count - 1)
            {
                returnValue += ItemSeparatorString;
            }
        }

        returnValue += stringReplacementsService.DoReplacements(buildIn.EndLayout, result);

        return returnValue;
    }
}