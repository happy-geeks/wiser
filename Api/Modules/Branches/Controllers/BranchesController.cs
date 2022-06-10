using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Modules.Branches.Interfaces;
using Api.Modules.Customers.Models;
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
        /// Creates a new branch for the authenticated customer.
        /// This will create a new database schema on the same server/cluster and then fill it with part of the data from the original database.
        /// </summary>
        /// <param name="name">The name of the environment</param>
        [HttpPost]
        [ProducesResponseType(typeof(CustomerModel), StatusCodes.Status200OK)]
        [Authorize]
        [Route("{name}")]
        public async Task<IActionResult> CreateBranchAsync(string name)
        {
            return (await branchesService.CreateAsync((ClaimsIdentity)User.Identity, name)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets the environments for the authenticated user.
        /// </summary>
        /// <returns>A list of <see cref="CustomerModel"/>.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<CustomerModel>), StatusCodes.Status200OK)]
        [Authorize]
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
        [Authorize]
        [Route("is-main")]
        public async Task<IActionResult> IsMainBranchAsync()
        {
            return (await branchesService.IsMainBranchAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Merge all changes done to wiser items, from a specific branch, to the main branch.
        /// </summary>
        /// <param name="id">The ID of the environment to copy the changes from.</param>
        [HttpPatch]
        [ProducesResponseType(typeof(SynchroniseChangesToProductionResultModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Authorize]
        [Route("merge/{id:int}")]
        public async Task<IActionResult> MergeBranchAsync(int id)
        {
            return (await branchesService.MergeAsync((ClaimsIdentity)User.Identity, id)).GetHttpResponseMessage();
        }
    }
}