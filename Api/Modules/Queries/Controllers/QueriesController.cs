using System.Collections.Generic;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Modules.Queries.Interfaces;
using Api.Modules.Queries.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Queries.Controllers;

/// <summary>
/// Controller for all CRUD functions for wiser query.
/// </summary>
[Route("api/v3/[controller]")]
[ApiController]
[Authorize]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class QueriesController : ControllerBase
{
    private readonly IQueriesService queriesService;

    /// <summary>
    /// Creates a new instance of <see cref="QueriesController"/>.
    /// </summary>
    public QueriesController(IQueriesService queriesService)
    {
        this.queriesService = queriesService;
    }

    /// <summary>
    /// Gets the queries that can be used for an export in the export module.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("export-module")]
    [ProducesResponseType(typeof(List<QueryModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForExportModuleAsync()
    {
        return (await queriesService.GetForExportModuleAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Gets the queries that can be used for setting up automatic communications via the communication module.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("communication-module")]
    [ProducesResponseType(typeof(List<QueryModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForCommunicationModuleAsync()
    {
        return (await queriesService.GetForCommunicationModuleAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Get all wiser queries.
    /// </summary>
    /// <returns>List of queries from wiser_query table</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<QueryModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForAdminModuleAsync()
    {
        return (await queriesService.GetAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Gets query data by ID.
    /// </summary>
    /// <returns>Query data from wiser_query</returns>
    [HttpGet]
    [Route("{id:int}")]
    [ProducesResponseType(typeof(QueryModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForAdminModuleAsync(int id)
    {
        return (await queriesService.GetAsync((ClaimsIdentity)User.Identity, id)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Creates new wiser query.
    /// </summary>
    /// <param name="description">The description of the new query.</param>
    /// <returns>The created query data</returns>
    [HttpPost]
    [ProducesResponseType(typeof(QueryModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] string description)
    {
        return (await queriesService.CreateAsync((ClaimsIdentity)User.Identity, description)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Updates existing wiser query by ID.
    /// </summary>
    /// <param name="queryModel">The new query data to save.</param>
    [HttpPut]
    [Route("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(QueryModel queryModel)
    {
        return (await queriesService.UpdateAsync((ClaimsIdentity)User.Identity,queryModel)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Deletes wiser query by ID.
    /// </summary>
    /// <param name="id">The ID from wiser_query.</param>
    [HttpDelete]
    [Route("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        return (await queriesService.DeleteAsync((ClaimsIdentity)User.Identity, id)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Execute a wiser_query by ID and return the results as JSON.
    /// </summary>
    /// <param name="id">The ID from wiser_query.</param>
    /// <param name="asKeyValuePair">If set to true the result of the query will be converted to a single object. Only columns with the names "key" and "value" are used.</param>
    /// <param name="parameters">The parameters to set before executing the query.</param>
    /// <returns>The results of the query as JSON.</returns>
    [HttpPost]
    [Route("{id:int}/json-result")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQueryResultsAsJson(int id, [FromQuery] bool asKeyValuePair = false, [FromBody] List<KeyValuePair<string, object>> parameters = null)
    {
        return (await queriesService.GetQueryResultAsJsonAsync((ClaimsIdentity) User.Identity, id, asKeyValuePair, parameters)).GetHttpResponseMessage();
    }
}