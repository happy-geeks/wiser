using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.VersionControl.Interfaces;
using Api.Modules.VersionControl.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.VersionControl.Controllers;

/// <summary>
///     Controller for getting or doing things with templates and dynamic content from the version control module in Wiser.
/// </summary>
[Route("api/v3/version-control")]
[ApiController]
[Authorize]
public class VersionControlController : Controller
{
    private readonly ICommitService commitService;

    public VersionControlController(ICommitService commitService)
    {
        this.commitService = commitService;
    }
    
    /// <summary>
    ///     Get all templates that have uncommitted changes.
    /// </summary>
    [HttpGet]
    [Route("templates-to-commit")]
    [ProducesResponseType(typeof(List<TemplateCommitModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTemplatesToCommitAsync(CreateCommitModel commitModel)
    {
        return (await commitService.GetTemplatesToCommitAsync()).GetHttpResponseMessage();
    }
}