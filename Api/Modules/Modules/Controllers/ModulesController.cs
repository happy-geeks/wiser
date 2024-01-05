using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mime;
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
    [Route("api/v3/[controller]")]
    [ApiController]
    [Authorize]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
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
        /// Gets all Wiser module that the user is allowed to use.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<ModuleAccessRightsModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAsync()
        {
            return (await modulesService.GetAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets the data and settings for a module with grid view mode enabled.
        /// </summary>
        /// <param name="id">The ID of the module.</param>
        /// <param name="options">The options for the Kendo UI grid.</param>
        [HttpPost]
        [Route("{id:int}/overview-grid")]
        [ProducesResponseType(typeof(GridSettingsAndDataModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> OverviewGridAsync(int id,  GridReadOptionsModel options)
        {
            return (await gridsService.GetOverviewGridDataAsync(id, options, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Get settings from all Wiser modules.
        /// </summary>
        [HttpGet]
        [Route("settings")]
        [ProducesResponseType(typeof(List<ModuleSettingsModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSettingsAsync()
        {
            return (await modulesService.GetSettingsAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets settings for a Wiser module.
        /// </summary>
        /// <param name="id">The ID of the module.</param>
        /// <param name="encryptValues">Optional: Whether to encrypt values in the JSON settings, such as queryId. Default is true. Set to false when getting the settings for the admin module.</param>
        [HttpGet]
        [Route("{id:int}/settings")]
        [ProducesResponseType(typeof(ModuleSettingsModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSettingsAsync(int id, bool encryptValues = true)
        {
            return (await modulesService.GetSettingsAsync(id, (ClaimsIdentity)User.Identity, encryptValues)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Update settings for a Wiser module.
        /// </summary>
        /// <param name="id">The ID of the module.</param>
        /// <param name="moduleSettingsModel">Module settings data</param>
        [HttpPut]
        [Route("{id:int}/settings")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> UpdateSettings(int id, ModuleSettingsModel moduleSettingsModel)
        {
            return (await modulesService.UpdateSettingsAsync(id, (ClaimsIdentity) User.Identity, moduleSettingsModel)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Creates a new Wiser module.
        /// </summary>
        /// <param name="name">Name of the new module</param>
        /// <returns>The ID of the newly created settings.</returns>
        [HttpPost]
        [Route("settings")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateAsync([FromBody] string name)
        {
            return (await modulesService.CreateAsync(name, (ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Exports the data of a Wiser module to the specified file format. This only works for grid view modules.
        /// </summary>
        /// <param name="id">The ID of the module to export.</param>
        /// <param name="fileFormat">The format the file should be in</param>
        /// <param name="fileName">Optional: The name that the exported file should be.</param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id:int}/export")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "text/csv")]
        public async Task<IActionResult> ExportAsync(int id, ExportFileFormats fileFormat, string fileName = null)
        {
            var (exportResult, contentType,  extension) = fileFormat switch
            {
                ExportFileFormats.Excel => (await modulesService.ExportToExcelAsync(id, (ClaimsIdentity)User.Identity), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx"),
                ExportFileFormats.Csv => (await modulesService.ExportToCsvAsync(id, (ClaimsIdentity)User.Identity, ';'), "text/csv", ".csv"),
                _ => throw new NotImplementedException($"Error in {nameof(ModulesController)}:Export to fileformat {fileFormat} is not implemented")
            };
            if (exportResult == null)
            {
                return NotFound(id);
            }

            if (exportResult.StatusCode != HttpStatusCode.OK)
            {
                return exportResult.GetHttpResponseMessage();
            }

            fileName = String.IsNullOrWhiteSpace(fileName) ? $"Export{extension}" : Path.ChangeExtension(fileName, extension);

            return File(exportResult.ModelObject, contentType, fileName);
        }

        /// <summary>
        /// Gets a list of all currently used module groups.
        /// </summary>
        /// <returns>A list with all group names.</returns>
        [HttpGet]
        [Route("groups")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetModuleGroupsAsync()
        {
            return (await modulesService.GetModuleGroupsAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Deletes a module.
        /// </summary>
        /// <param name="id">The ID of the module.</param>
        [HttpDelete]
        [Route("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            return (await modulesService.DeleteAsync((ClaimsIdentity)User.Identity, id)).GetHttpResponseMessage();
        }
    }
}