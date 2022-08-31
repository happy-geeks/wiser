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
    private readonly IVersionControlService versionControlService;
    
    /// <summary>
    ///     ctor
    /// </summary>
    /// <param name="commitService"></param>
    /// <param name="versionControlService"></param>
    public VersionControlController(ICommitService commitService, IVersionControlService versionControlService)
    {
        this.commitService = commitService;
        this.versionControlService = versionControlService;
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
    
    /// <summary>
    /// Gets the dynamic content that are part of the given template
    /// </summary>
    /// <param name="templateId">the ID of the template</param>
    /// <returns>Returns all the dynamic content that are linked to the given template in a list of dynamic content</returns>
    [HttpGet]
    [Route("dynamic-content-in-template/{templateId:int}")]
    [ProducesResponseType(typeof(List<DynamicContentModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDynamicContentInTemplate(int templateId)
    {
        return (await versionControlService.GetDynamicContentInTemplateAsync(templateId)).GetHttpResponseMessage();
    }
}