using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Queries.Interfaces
{
    /// <summary>
    /// Service for getting styled output from Wiser
    /// </summary>
    public interface IStyledOutputService
    {
        /// <summary>
        /// Find a styled output in the wiser_styled_output table and returns the result
        /// </summary>
        /// <param name="id">The ID from wiser_styled_output.</param>
        /// <param name="parameters">The parameters to set before executing the styled output.</param>
        /// <param name="stripNewlinesAndTabs">replaces \r\n \n and \t when encountered in the format.</param>
        /// <param name="page">the page number used in pagination-supported styled outputs.</param>
        /// <param name="inUseStyleIds">used for making sure no higher level styles are causing a cyclic reference in recursive calls, this can be left null/param>
        /// <returns>The results of the query .</returns>
        /// 
        Task<ServiceResult<JToken>> GetStyledOutputResultJsonAsync(ClaimsIdentity identity, int id, List<KeyValuePair<string, object>> parameters, bool stripNewlinesAndTabs, int page = 0, List<int> inUseStyleIds = null);
    }
}
