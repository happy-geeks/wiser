using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Modules.Grids.Interfaces;
using Api.Modules.Grids.Models;
using Api.Modules.Kendo.Models;
using Api.Modules.Modules.Interfaces;
using Api.Modules.Modules.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Modules.Controllers
{
    /// <summary>
    /// Controller for getting data about the different modules available in Wiser en to get a list of the modules that the authenticated user has access to.
    /// </summary>
    [Route("api/v3/[controller]"), ApiController, Authorize]
    public class ModulesController : ControllerBase
    {
        private readonly IModulesService modulesService;
        private readonly IGridsService gridsService;

        /// <summary>
        /// Creates a new instance of ModulesController.
        /// </summary>
        public ModulesController(IModulesService modulesService, IGridsService gridsService)
        {
            this.modulesService = modulesService;
            this.gridsService = gridsService;
        }

        /// <summary>
        /// Gets all Wiser 2 module that the user is allowed to use.
        /// </summary>
        [HttpGet, ProducesResponseType(typeof(List<ModuleAccessRightsModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAsync()
        {
            return (await modulesService.GetAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets the data and settings for a module with grid view mode enabled.
        /// </summary>
        /// <param name="id">The ID of the module.</param>
        /// <param name="options">The options for the Kendo UI grid.</param>
        [HttpPost, Route("{id:int}/overview-grid"), ProducesResponseType(typeof(GridSettingsAndDataModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> OverviewGridAsync(int id,  GridReadOptionsModel options)
        {
            return (await gridsService.GetOverviewGridDataAsync(id, options, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Get settings from all Wiser 2 modules.
        /// </summary>
        [HttpGet, Route("settings"), ProducesResponseType(typeof(List<ModuleSettingsModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSettingsAsync()
        {
            return (await modulesService.GetSettingsAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets settings for a Wiser 2 module.
        /// </summary>
        /// <param name="id">The ID of the module.</param>
        [HttpGet, Route("{id:int}/settings"), ProducesResponseType(typeof(ModuleSettingsModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSettingsAsync(int id)
        {
            return (await modulesService.GetSettingsAsync(id, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Update settings for a Wiser 2 module.
        /// </summary>
        /// <param name="id">The ID of the module.</param>
        /// <param name="moduleSettingsModel">Module settings data</param>
        [HttpPut, Route("{id:int}/settings")]
        public async Task<IActionResult> UpdateSettings(int id, ModuleSettingsModel moduleSettingsModel)
        {
            return (await modulesService.UpdateSettingsAsync(id, (ClaimsIdentity) User.Identity, moduleSettingsModel)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Creates a new Wiser 2 module.
        /// </summary>
        /// <param name="name">Name of the new module</param>
        [HttpPost, Route("settings")]
        public async Task<IActionResult> CreateAsync([FromBody] string name)
        {
            return (await modulesService.CreateAsync(name, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Exports the data of a Wiser 2 module to Excel. This only works for grid view modules.
        /// </summary>
        /// <param name="id">The ID of the module to export.</param>
        /// <param name="fileName">Optional: The name that the exported file should be.</param>
        /// <returns></returns>
        [HttpGet, Route("{id:int}/export"), ProducesResponseType(typeof(ModuleSettingsModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportAsync(int id, string fileName = null)
        {
            var exportResult = await modulesService.ExportAsync(id, (ClaimsIdentity)User.Identity);
            if (exportResult == null)
            {
                return NotFound(id);
            }

            if (exportResult.StatusCode != HttpStatusCode.OK)
            {
                return exportResult.GetHttpResponseMessage();
            }

            fileName = String.IsNullOrWhiteSpace(fileName) ? "Export.xlsx" : Path.ChangeExtension(fileName, ".xlsx");
            
            return File(exportResult.ModelObject, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
