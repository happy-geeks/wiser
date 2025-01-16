using System;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Api.Modules.DigitalOcean.Interfaces;
using Api.Modules.DigitalOcean.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace Api.Modules.DigitalOcean.Controllers;

/// <summary>
/// A controller for doing things with DigitalOcean.
/// </summary>
[Route("api/v3/digital-ocean")]
[ApiController]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class DigitalOceanController : ControllerBase
{
    private readonly IDigitalOceanService digitalOceanService;

    /// <summary>
    /// Controller for calling the Digital Ocean API.
    /// </summary>
    public DigitalOceanController(IDigitalOceanService digitalOceanService)
    {
        this.digitalOceanService = digitalOceanService;
    }

    /// <summary>
    /// Redirect to the Digital Ocean authorization page.
    /// </summary>
    [HttpGet("authorize")]
    [EnableCors("AllowAllOrigins")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public RedirectResult Authorize()
    {
        return Redirect(digitalOceanService.AuthorizationRedirect());
    }

    /// <summary>
    /// Processes a call back from Digital Ocean's OAUTH2 authentication.
    /// </summary>
    /// <param name="code">The authentication code from Digital Ocean.</param>
    [HttpGet("callback")]
    [EnableCors("AllowAllOrigins")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<ActionResult<string>> CallbackAsync([FromQuery] string code)
    {
        return await digitalOceanService.ProcessCallbackAsync(code);
    }

    /// <summary>
    /// Gets information about a database cluster.
    /// </summary>
    [HttpGet("databases")]
    [EnableCors("AllowAllOrigins")]
    [ProducesResponseType(typeof(GetDatabasesResponseModel), StatusCodes.Status200OK)]
    public async Task<JsonResult> DatabasesAsync()
    {
        var accessToken = AccessTokenFromHeaders();
        return new JsonResult(await digitalOceanService.DatabaseListAsync(accessToken));
    }

    /// <summary>
    /// Create a new database in a cluster.
    /// </summary>
    /// <param name="body">The data of the new database to create.</param>
    [HttpPost("databases")]
    [EnableCors("AllowAllOrigins")]
    [ProducesResponseType(typeof(CreateDatabaseApiResponseModel), StatusCodes.Status200OK)]
    public async Task<JsonResult> CreateDatabaseAsync([FromBody]CreateDatabaseRequestModel body)
    {
        var accessToken = AccessTokenFromHeaders();
        try
        {
            var databaseInfo = await digitalOceanService.CreateDatabaseAsync(body.DatabaseCluster, body.Database, body.User, accessToken);
            await digitalOceanService.RestrictMysqlUserToDbAsync(databaseInfo, accessToken);
            return new JsonResult(databaseInfo);
        }
        catch (Exception e)
        {
            return new JsonResult(new { Error = e.ToString() });
        }
    }

    private string AccessTokenFromHeaders()
    {
        StringValues stringValues;
        Request.Headers.TryGetValue("x-digital-ocean", out stringValues);
        return stringValues.FirstOrDefault();
    }
}