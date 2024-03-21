using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Models;
using Api.Modules.Kendo.Enums;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Models.History;
using Api.Modules.Templates.Models.Measurements;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Templates.Models.Template;
using Api.Modules.Tenants.Models;
using Api.Modules.Templates.Models.Template.WtsModels;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Templates.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Templates.Controllers
{
    /// <summary>
    /// Controller for getting or doing things with templates from the templates module in Wiser.
    /// </summary>
    [Route("api/v3/[controller]")]
    [ApiController]
    [Authorize]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    public class TemplatesController : ControllerBase
    {
        private readonly ITemplatesService templatesService;
        private readonly IHistoryService historyService;
        private readonly GclSettings gclSettings;

        /// <summary>
        /// Creates a new instance of TemplatesController.
        /// </summary>
        public TemplatesController(ITemplatesService templatesService, IOptions<GclSettings> gclSettings, IHistoryService historyService)
        {
            this.templatesService = templatesService;
            this.historyService = historyService;
            this.gclSettings = gclSettings.Value;
        }

        /// <summary>
        /// Gets the CSS that should be used for HTML editors, so that their content will look more like how it would look on the tenant's website.
        /// </summary>
        /// <returns>A string that contains the CSS that should be loaded in the HTML editor.</returns>
        [HttpGet]
        [Route("css-for-html-editors")]
        [ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK)]
        [AllowAnonymous]
        public async Task<IActionResult> GetCssForHtmlEditorsAsync([FromQuery] TenantInformationModel tenantInformation)
        {
            // Create a ClaimsIdentity based on query parameters instead the Identity from the bearer token due to being called from an image source where no headers can be set.
            var userId = String.IsNullOrWhiteSpace(tenantInformation.encryptedUserId) ? 0 : Int32.Parse(tenantInformation.encryptedUserId.Replace(" ", "+").DecryptWithAesWithSalt(gclSettings.DefaultEncryptionKey, true));
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.GroupSid, tenantInformation.subDomain ?? "")
            };
            var dummyClaimsIdentity = new ClaimsIdentity(claims);
            //Set the sub domain for the database connection.
            HttpContext.Items[HttpContextConstants.SubDomainKey] = tenantInformation.subDomain;

            return (await templatesService.GetCssForHtmlEditorsAsync(dummyClaimsIdentity)).GetHttpResponseMessage("text/css");
        }

        /// <summary>
        /// Gets a query from the wiser database and executes it in the tenant database.
        /// </summary>
        /// <param name="templateName">The encrypted name of the wiser template.</param>f
        [HttpGet]
        [HttpPost]
        [Route("get-and-execute-query/{templateName}")]
        [Consumes(MediaTypeNames.Application.Json, "application/x-www-form-urlencoded")]
        [ProducesResponseType(typeof(JToken), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAndExecuteQueryAsync(string templateName)
        {
            IFormCollection requestPostData = null;
            if (Request.HasFormContentType)
            {
                requestPostData = await Request.ReadFormAsync();
            }

            return (await templatesService.GetAndExecuteQueryAsync((ClaimsIdentity)User.Identity, templateName, requestPostData)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Retrieve the tree view section underlying the parentId. Transforms the tree view section into a list of TemplateTreeViewModels.
        /// </summary>
        /// <param name="parentId">The id of the template whose child nodes are to be retrieved.</param>
        /// <returns>A List of TemplateTreeViewModels containing the id, names and types of the templates included in the requested section.</returns>
        [HttpGet]
        [Route("{parentId:int}/tree-view")]
        [ProducesResponseType(typeof(List<TemplateTreeViewModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> TreeViewAsync(int parentId = 0)
        {
            return (await templatesService.GetTreeViewSectionAsync((ClaimsIdentity)User.Identity, parentId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Retrieve the history of the template. This will include changes made to dynamic content between the releases of templates and the publishes to different environments from this template. This data is collected and combined in a TemnplateHistoryOverviewModel
        /// </summary>
        /// <param name="templateId">The id of the template to retrieve the history from.</param>
        /// <param name="pageNumber">page that needs to be loaded in</param>
        /// <param name="itemsPerPage">amount of items per page</param>
        /// <returns>A TemplateHistoryOverviewModel containing a list of templatehistorymodels and a list of publishlogmodels. The model contains base info and a list of changes made within the version and its sub components (e.g. dynamic content, publishes).</returns>
        [HttpGet]
        [Route("{templateId:int}/history")]
        [ProducesResponseType(typeof(TemplateHistoryOverviewModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHistoryAsync(int templateId, int pageNumber = 1, int itemsPerPage = 50)
        {
            return (await templatesService.GetTemplateHistoryAsync((ClaimsIdentity)User.Identity, templateId, pageNumber, itemsPerPage)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Get the meta data (name, changedOn, changedBy etc) from a template.
        /// </summary>
        /// <param name="templateId">The id of the template to retrieve the data from.</param>
        /// <returns>A <see cref="TemplateSettingsModel"/> containing the current template data of the template with the given id.</returns>
        [HttpGet]
        [Route("{templateId:int}/meta")]
        [ProducesResponseType(typeof(TemplateSettingsModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMetaDataAsync(int templateId)
        {
            return (await templatesService.GetTemplateMetaDataAsync(templateId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Retrieve the latest version of the template.
        /// </summary>
        /// <param name="templateId">The id of the template to retrieve.</param>
        /// <returns>A template model containing the data of the templateversion.</returns>
        [HttpGet]
        [Route("{templateId:int}/settings")]
        [ProducesResponseType(typeof(TemplateSettingsModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSettingsAsync(int templateId)
        {
            return (await templatesService.GetTemplateSettingsAsync((ClaimsIdentity)User.Identity, templateId)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Retrieve the latest version of the editor value of the template in an object instead of raw XML.
        /// Also add the options for the available fields.
        /// </summary>
        /// <param name="templateId">The id of the template to retrieve the editor value of.</param>
        /// <returns>A object model containing the data of the template editor value.</returns>
        [HttpGet]
        [Route("{templateId:int}/wtsconfiguration")]
        [ProducesResponseType(typeof(TemplateWtsConfigurationModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWtsConfigurationAsync(int templateId)
        {
            return (await templatesService.GetTemplateWtsConfigurationAsync((ClaimsIdentity)User.Identity, templateId)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Save the configuration for the given template.
        /// </summary>
        /// <param name="templateId">The id of the template to save the configuration to.</param>
        /// <param name="data"> A <see cref="TemplateWtsConfigurationModel"/> containing the data of the template editor value.</param>
        [HttpPost]
        [Route("{templateId:int}/wtsconfiguration")]
        [ProducesResponseType(typeof(TemplateWtsConfigurationModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> SaveConfigurationAsync(int templateId, TemplateWtsConfigurationModel data)
        {
            return (await templatesService.SaveAsync((ClaimsIdentity)User.Identity, templateId, data)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Retrieve the published environments of a template, this includes a list of all version options.
        /// </summary>
        /// <param name="templateId">The id of the template from which to retrieve the published environments.</param>
        /// <returns>A published environment model including the versions numbers of the Live, accept and test environment and a list of all possible versions.</returns>
        [HttpGet]
        [Route("{templateId:int}/published-environments")]
        [ProducesResponseType(typeof(PublishedEnvironmentModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPublishedEnvironmentsAsync(int templateId)
        {
            return (await templatesService.GetTemplateEnvironmentsAsync(templateId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Retrieve the templates that are linked to the given templates.
        /// </summary>
        /// <param name="templateId">The id of the template of which to get the linked templates.</param>
        /// <returns>A Linked Templates model containing lists of linked templates separated into lists of certain types (e.g. scss, javascript).</returns>
        [HttpGet]
        [Route("{templateId:int}/linked-templates")]
        [ProducesResponseType(typeof(LinkedTemplatesModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLinkedTemplatesAsync(int templateId)
        {
            return (await templatesService.GetLinkedTemplatesAsync(templateId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Retrieve the dynamic content that is linked to the given template.
        /// </summary>
        /// <param name="templateId">The id of the template of which the linked dynamic content should be retrieved.</param>
        /// <returns>List of dynamic content overview models. This is a condensed version of dynamic content data for creating a overview of linked content.</returns>
        [HttpGet]
        [Route("{templateId:int}/linked-dynamic-content")]
        [ProducesResponseType(typeof(LinkedTemplatesModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLinkedDynamicContentAsync(int templateId)
        {
            var resultOverview = await templatesService.GetLinkedDynamicContentAsync(templateId);
            resultOverview.ModelObject = await historyService.GetPublishedEnvironmentsOfOverviewModels(resultOverview.ModelObject);

            return resultOverview.GetHttpResponseMessage();
        }

        /// <summary>
        /// Publish a template to a new environment. If moved forward the lower environments will also be moved.
        /// </summary>
        /// <param name="templateId">The id of the template to publish.</param>
        /// <param name="version">The version of the template to publish.</param>
        /// <param name="environment">The environment to push the template version to. This will be converted to a PublishedEnvironmentEnum.</param>
        /// <returns>The number of affected rows.</returns>
        [HttpPost]
        [Route("{templateId:int}/publish/{environment}/{version:int}")]
        [ProducesResponseType(typeof(LinkedTemplatesModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> PublishToEnvironmentAsync(int templateId, Environments environment, int version)
        {
            var currentPublished = await templatesService.GetTemplateEnvironmentsAsync(templateId);
            return currentPublished.StatusCode != HttpStatusCode.OK
                ? currentPublished.GetHttpResponseMessage()
                : (await templatesService.PublishToEnvironmentAsync((ClaimsIdentity)User.Identity, templateId, version, environment, currentPublished.ModelObject)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Creates an empty template with the given name, type and parent template.
        /// </summary>
        /// <param name="parentId">The id of the parent template of the template that will be created.</param>
        /// <param name="newTemplate">Model class with the needed info to make the template</param>
        /// <returns>The id of the newly created template. This can be used to update the interface accordingly.</returns>
        [HttpPut]
        [Route("{parentId:int}")]
        [ProducesResponseType(typeof(TemplateTreeViewModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateAsync(int parentId, NewTemplateModel newTemplate)
        {
            return (await templatesService.CreateAsync((ClaimsIdentity)User.Identity, newTemplate.Name, parentId, newTemplate.Type, newTemplate.EditorValue)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Save the template as a new version and save the linked templates if necessary. This method will calculate if links are to be added or removed from the current situation.
        /// </summary>
        /// <param name="templateId">The ID of the template to update.</param>
        /// <param name="templateData">A <see cref="TemplateSettingsModel"/> containing the data of the template that is to be saved as a new version</param>
        /// <param name="skipCompilation">Optional: Whether or not to skip the compilations of SCSS templates. Default value is false.</param>
        [HttpPost]
        [Route("{templateId:int}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> SaveAsync(int templateId, TemplateSettingsModel templateData, bool skipCompilation = false)
        {
            templateData.TemplateId = templateId;
            return (await templatesService.SaveAsync((ClaimsIdentity)User.Identity, templateData, skipCompilation)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Renames a template. This will create a new version of the template with the name, so that we can always see in the history that the name has been changed.
        /// </summary>
        /// <param name="templateId">The ID of the template to rename.</param>
        /// <param name="newName">The new name.</param>
        [HttpPost]
        [Route("{templateId:int}/rename")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> RenameAsync(int templateId, [FromQuery]string newName)
        {
            return (await templatesService.RenameAsync((ClaimsIdentity)User.Identity, templateId, newName)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Deletes a template. This will not actually delete it from the database, but add a new version with removed = 1 instead.
        /// </summary>
        /// <param name="templateId">The ID of the template to delete.</param>
        /// <param name="alsoDeleteChildren">Optional: Whether or not to also delete all children of this template. Default value is <see langword="true"/>.</param>
        [HttpDelete]
        [Route("{templateId:int}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteAsync(int templateId, [FromQuery]bool alsoDeleteChildren = true)
        {
            return (await templatesService.DeleteAsync((ClaimsIdentity)User.Identity, templateId, alsoDeleteChildren)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Search for a template.
        /// </summary>
        /// <param name="searchValue">The value to search for.</param>
        [HttpGet]
        [Route("search")]
        [ProducesResponseType(typeof(List<SearchResultModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchAsync(string searchValue)
        {
            return (await templatesService.SearchAsync((ClaimsIdentity)User.Identity, searchValue)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Moves a template to a new position.
        /// </summary>
        /// <param name="sourceId">The ID of the template that is being moved.</param>
        /// <param name="destinationId">The ID of the template or directory where it's being moved to.</param>
        /// <param name="dropPosition">The drop position, can be either <see cref="TreeViewDropPositions.Over"/>, <see cref="TreeViewDropPositions.Before"/> or <see cref="TreeViewDropPositions.After"/>.</param>
        [HttpPut]
        [Route("{sourceId:int}/move/{destinationId:int}")]
        [ProducesResponseType(typeof(List<SearchResultModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> MoveAsync(int sourceId, int destinationId, [FromQuery]TreeViewDropPositions dropPosition)
        {
            return (await templatesService.MoveAsync((ClaimsIdentity)User.Identity, sourceId, destinationId, dropPosition)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets the tree view including template settings of all templates.
        /// </summary>
        /// <param name="startFrom">Set the place from which to start the tree view, folders separated by comma.</param>
        /// <param name="environment">The environment the template needs to be active on.</param>
        /// <returns></returns>
        [HttpGet]
        [Route("entire-tree-view")]
        [ProducesResponseType(typeof(List<TemplateTreeViewModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEntireTreeViewStructureAsync(string startFrom = "", Environments? environment = null)
        {
            return (await templatesService.GetEntireTreeViewStructureAsync((ClaimsIdentity)User.Identity, 0, startFrom, environment)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Checks if there's already a template marked as a default header with the given regex.
        /// </summary>
        /// <param name="templateId">ID of the current template.</param>
        /// <param name="regexString">The regex string of the template.</param>
        /// <returns>A string with the name of the template that this template conflicts with, or an empty string if there's no conflict.</returns>
        [HttpGet]
        [Route("{templateId:int}/check-default-header-conflict")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckDefaultHeaderConflict(int templateId, string regexString)
        {
            return (await templatesService.CheckDefaultHeaderConflict(templateId, regexString)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Checks if there's already a template marked as a default footer with the given regex.
        /// </summary>
        /// <param name="templateId">ID of the current template.</param>
        /// <param name="regexString">The regex string of the template.</param>
        /// <returns>A string with the name of the template that this template conflicts with, or an empty string if there's no conflict.</returns>
        [HttpGet]
        [Route("{templateId:int}/check-default-footer-conflict")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckDefaultFooterConflict(int templateId, string regexString)
        {
            return (await templatesService.CheckDefaultFooterConflict(templateId, regexString)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Retrieve a virtual item, which is a view, routine, or trigger that isn't yet managed by Wiser.
        /// </summary>
        /// <param name="objectName">The name of the view, routine, or trigger.</param>
        /// <param name="templateType">The type that determines what kind of item should be retrieved (view, routine, or trigger).</param>
        /// <returns>A <see cref="TemplateSettingsModel"/> with information about the virtual template.</returns>
        [HttpGet]
        [Route("get-virtual-item")]
        [ProducesResponseType(typeof(TemplateSettingsModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetVirtualItem(string objectName, TemplateTypes templateType)
        {
            return (await templatesService.GetVirtualTemplateAsync(objectName, templateType)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Retrieves a list of table names that can be used to populate the table name select element for a trigger template.
        /// </summary>
        /// <returns>An <see cref="IList{T}"/> containing strings.</returns>
        [HttpGet]
        [Route("get-trigger-table-names")]
        [ProducesResponseType(typeof(IList<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTriggerTableNames()
        {
            return (await templatesService.GetTableNamesForTriggerTemplatesAsync()).GetHttpResponseMessage();
        }

        /// <summary>
        /// Deploy one or more templates to a branch.
        /// </summary>
        /// <param name="templateId">The ID of the template to deploy.</param>
        /// <param name="branchId">The ID of the branch to deploy the template to.</param>
        [HttpPost]
        [Route("{templateId:int}/deploy-to-branch/{branchId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeployToBranchAsync(int templateId, int branchId)
        {
            return (await templatesService.DeployToBranchAsync((ClaimsIdentity) User.Identity, new List<int> { templateId }, branchId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Get the settings for measurements of a template.
        /// </summary>
        /// <param name="templateId">The ID of the template to get the settings of.</param>
        /// <returns>The measurement settings of the template.</returns>
        [HttpGet]
        [Route("{templateId:int}/measurement-settings")]
        [ProducesResponseType(typeof(MeasurementSettings), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMeasurementSettingsAsync(int templateId)
        {
            return (await templatesService.GetMeasurementSettingsAsync(templateId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Save the settings for measurements of this template.
        /// </summary>
        /// <param name="templateId">The ID of the template to save the settings for.</param>
        /// <param name="settings">The new settings.</param>
        [HttpPut]
        [Route("{templateId:int}/measurement-settings")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> SaveMeasurementSettingsAsync(int templateId, [FromBody]MeasurementSettings settings)
        {
            return (await templatesService.SaveMeasurementSettingsAsync(settings, templateId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Get rendering logs from database, filtered by the parameters.
        /// </summary>
        /// <param name="templateId">The ID of the template to get the render logs for.</param>
        /// <param name="version">The version of the template or component.</param>
        /// <param name="urlRegex">A regex for filtering logs on certain URLs/pages.</param>
        /// <param name="environment">The environment to get the logs for. Set to null to get the logs for all environments. Default value is null.</param>
        /// <param name="userId">The ID of the website user, if you want to get the logs for a specific user only.</param>
        /// <param name="languageCode">The language code that is used on the website, if you want to get the logs for a specific language only.</param>
        /// <param name="pageSize">The amount of logs to get. Set to 0 to get all of then. Default value is 500.</param>
        /// <param name="pageNumber">The page number. Default value is 1. Only applicable if pageSize is greater than zero.</param>
        /// <param name="getDailyAverage">Set this to true to get the average render time per day, instead of getting every single render log separately. Default is false.</param>
        /// <param name="start">Only get results from this start date and later.</param>
        /// <param name="end">Only get results from this end date and earlier.</param>
        /// <returns>A list of <see cref="RenderLogModel"/> with the results.</returns>
        [HttpGet]
        [Route("{templateId:int}/render-logs")]
        [ProducesResponseType(typeof(MeasurementSettings), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRenderLogsAsync(int templateId, int version = 0,
            string urlRegex = null, Environments? environment = null, ulong userId = 0,
            string languageCode = null, int pageSize = 500, int pageNumber = 1,
            bool getDailyAverage = false, DateTime? start = null, DateTime? end = null)
        {
            return (await templatesService.GetRenderLogsAsync(templateId, version, urlRegex, environment, userId, languageCode, pageSize, pageNumber, getDailyAverage, start, end)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Converts a JCL template to a GCL template.
        /// </summary>
        [HttpPost, Route("import-legacy"), ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public async Task<IActionResult> ConvertLegacyTemplatesToNewTemplatesAsync()
        {
            return (await templatesService.ConvertLegacyTemplatesToNewTemplatesAsync((ClaimsIdentity)User.Identity)).GetHttpResponseMessage();
        }
    }
}