using System.Collections.Generic;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Modules.Branches.Interfaces;
using Api.Modules.Branches.Models;
using Api.Modules.Tenants.Models;
using GeeksCoreLibrary.Modules.Branches.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Branches.Controllers
{
    /// <summary>
    /// A controller for doing things with Wiser branches. Branches in Wiser are copies of their original database where they can make changes in before putting them in production.
    /// </summary>
    [Route("api/v3/branches")]
    [ApiController]
    [Authorize]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    public class BranchesController : Controller
    {
        private readonly IBranchesService branchesService;

        /// <summary>
        /// Creates a new instance of <see cref="BranchesController"/>.
        /// </summary>
        public BranchesController(IBranchesService branchesService)
        {
            this.branchesService = branchesService;
        }

        /// <summary>
        /// Gets the environments for the authenticated user.
        /// </summary>
        /// <returns>A list of <see cref="TenantModel"/>.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<TenantModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBranchesAsync()
        {
            return (await branchesService.GetAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets whether the current branch is the main branch.
        /// </summary>
        /// <returns>A boolean indicating whether the current branch is the main branch.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [Route("is-main")]
        public async Task<IActionResult> IsMainBranchAsync()
        {
            return (await branchesService.IsMainBranchAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Creates a new branch for the authenticated tenant.
        /// This will create a new database schema on the same server/cluster and then fill it with part of the data from the original database.
        /// </summary>
        /// <param name="settings">The settings for the new environment</param>
        [HttpPost]
        [ProducesResponseType(typeof(TenantModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateBranchAsync(CreateBranchSettingsModel settings)
        {
            return (await branchesService.CreateAsync((ClaimsIdentity)User.Identity, settings)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Get the changes of a branch.
        /// </summary>
        /// <param name="id">The ID of the branch to get the changes of.</param>
        /// <returns>A list of changes per entity type / Wiser setting type.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ChangesAvailableForMergingModel), StatusCodes.Status200OK)]
        [Route("changes/{id:int}")]
        public async Task<IActionResult> GetChangesAsync(int id)
        {
            return (await branchesService.GetChangesAsync((ClaimsIdentity)User.Identity, id)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Merge all changes done to wiser items, from a specific branch, to the main branch.
        /// </summary>
        /// <param name="settings">The settings of what exactly to merge.</param>
        [HttpPatch]
        [ProducesResponseType(typeof(MergeBranchResultModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Route("merge")]
        public async Task<IActionResult> MergeBranchAsync(MergeBranchSettingsModel settings)
        {
            return (await branchesService.MergeAsync((ClaimsIdentity)User.Identity, settings)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Marks a branch to be deleted by the WTS.
        /// </summary>
        /// <param name="id">The ID of the branch that should be deleted.</param>
        [HttpDelete]
        [Route("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteBranchAsync(int id)
        {
            return (await branchesService.DeleteAsync((ClaimsIdentity)User.Identity, id)).GetHttpResponseMessage();
        }
    }
}