using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Queries.Interfaces;

/// <summary>
/// Service for getting styled output from Wiser
/// </summary>
public interface IStyledOutputService
{
    /// <summary>
    /// Find a styled output id based on its name.
    /// </summary>
    /// <param name="name">The name from wiser_styled_output.</param>
    /// <returns>The results the id of the found styled output or instead returns -1 when not found .</returns>
    Task<int> GetStyledOutputIdFromNameAsync(string name);

    /// <summary>
    /// Find a styled output in the wiser_styled_output table and returns the result
    /// </summary>
    /// <param name="identity">The <see cref="ClaimsIdentity"/> of the authenticated user.</param>
    /// <param name="id">The ID from wiser_styled_output.</param>
    /// <param name="parameters">The parameters to set before executing the styled output.</param>
    /// <param name="stripNewlinesAndTabs">replaces \r\n \n and \t when encountered in the format.</param>
    /// <param name="resultsPerPage"> the amount of results per page, will be capped at 500 </param>
    /// <param name="page">the page number used in pagination-supported styled outputs.</param>
    /// <param name="inUseStyleIds">used for making sure no higher level styles are causing a cyclic reference in recursive calls, this can be left null</param>
    /// <returns>The results of the query .</returns>
    Task<ServiceResult<JToken>> GetStyledOutputResultJsonAsync(ClaimsIdentity identity, int id, List<KeyValuePair<string, object>> parameters, bool stripNewlinesAndTabs, int resultsPerPage, int page = 0, List<int> inUseStyleIds = null);

    /// <summary>
    /// Handles multiple styled output elements at once (needed for the products api).
    /// </summary>
    /// <param name="identity">The identity for the connection.</param>
    /// <param name="allowedFormats">The types that can be processed, for endpoint this is only JSON, sub elements also support RAW.</param>
    /// <param name="id">The ID of the starting point of the requested styled output.</param>
    /// <param name="itemIds">A list of item/product ids that need to be used for the multi request.</param>
    /// <param name="parameters">The parameters send along to the database connection.</param>
    /// <returns>Returns a dictionary with the ids and outputs.</returns>
    Task<Dictionary<ulong,string>> GetMultiStyledOutputResultsAsync(ClaimsIdentity identity, string[] allowedFormats, int id, List<ulong> itemIds, List<KeyValuePair<string, object>> parameters);
}