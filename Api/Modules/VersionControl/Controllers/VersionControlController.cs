using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Modules.VersionControl.Interfaces;
using Api.Modules.VersionControl.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.VersionControl.Controllers;

/// <summary>
/// Controller for getting or doing things with templates and dynamic content from the version control module in Wiser.
/// </summary>
[Route("api/v3/version-control")]
[ApiController]
[Authorize]
public class VersionControlController : Controller
{
    private readonly ICommitService commitService;
    private readonly IVersionControlService versionControlService;

    /// <summary>
    /// Creates a new instance of <see cref="VersionControlController"/>.
    /// </summary>
    public VersionControlController(ICommitService commitService, IVersionControlService versionControlService)
    {
        this.commitService = commitService;
        this.versionControlService = versionControlService;
    }

    /// <summary>
    /// Get all templates that have uncommitted changes.
    /// </summary>
    [HttpGet]
    [Route("templates-to-commit")]
    [ProducesResponseType(typeof(List<TemplateCommitModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTemplatesToCommitAsync(CommitModel commitModel)
    {
        return (await commitService.GetTemplatesToCommitAsync()).GetHttpResponseMessage();
    }

    /// <summary>
    /// Get all dynamic content that have uncommitted changes.
    /// </summary>
    [HttpGet]
    [Route("dynamic-content-to-commit")]
    [ProducesResponseType(typeof(List<DynamicContentCommitModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDynamicContentInTemplate()
    {
        return (await commitService.GetDynamicContentsToCommitAsync()).GetHttpResponseMessage();
    }

    /// <summary>
    /// Creates new commit item in the database and deploys the selected templates and contents to the selected environment, or gets an existing commit and deploy that to another environment.
    /// </summary>
    /// <param name="data">The data of the commit. The ID property should be 0 for creating a new commit, or contain a value for deploying an existing commit.</param>
    /// <returns>Returns a model of the commit.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CommitModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAndOrDeployCommitAsync(CommitModel data)
    {
        return (await commitService.CreateAndOrDeployCommitAsync(data, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Get all commits that haven't been completed yet,
    /// </summary>
    /// <returns>A list of <see cref="CommitModel"/>.</returns>
    [HttpGet]
    [Route("not-completed-commits")]
    [ProducesResponseType(typeof(List<CommitModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotCompletedCommitsAsync()
    {
        return (await commitService.GetCommitHistoryAsync(false, true)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Get all commits that haven't been completed yet,
    /// </summary>
    /// <returns>A list of <see cref="CommitModel"/>.</returns>
    [HttpGet]
    [Route("completed-commits")]
    [ProducesResponseType(typeof(List<CommitModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCompletedCommitsAsync()
    {
        return (await commitService.GetCommitHistoryAsync(true, false)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Deploy one or more commits to a branch.
    /// </summary>
    /// <param name="branchId">The ID of the branch to deploy to.</param>
    /// <param name="commitIds">The IDs of the commits to deploy.</param>
    [HttpPost]
    [Route("deploy-to-branch/{branchId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeployToBranchAsync(int branchId, List<int> commitIds)
    {
        return (await versionControlService.DeployToBranchAsync((ClaimsIdentity) User.Identity, commitIds, branchId)).GetHttpResponseMessage();
    }
}