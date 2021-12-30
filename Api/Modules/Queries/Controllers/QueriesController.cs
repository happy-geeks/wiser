using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Api.Modules.Queries.Interfaces;
using Api.Modules.Queries.Models;
using Microsoft.AspNetCore.Http;

namespace Api.Modules.Queries.Controllers
{
    [Route("api/v3/[controller]"), ApiController, Authorize]
    public class QueriesController : ControllerBase
    {
        private readonly IQueriesService queriesService;

        public QueriesController(IQueriesService queriesService)
        {
            this.queriesService = queriesService;
        }

        /// <summary>
        /// Gets the queries that can be used for an export in the export module.
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("export-module"), ProducesResponseType(typeof(List<QueryModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetForExportModuleAsync()
        {
            return (await queriesService.GetForExportModuleAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }
    }
}
