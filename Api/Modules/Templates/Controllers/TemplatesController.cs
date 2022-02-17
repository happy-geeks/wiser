﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Models;
using Api.Modules.Customers.Models;
using Api.Modules.Kendo.Enums;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Models.History;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Templates.Models.Preview;
using Api.Modules.Templates.Models.Template;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Templates.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Templates.Controllers
{
    /// <summary>
    /// Controller for getting or doing things with templates from the templates module in Wiser.
    /// </summary>
    [Route("api/v3/[controller]"), ApiController, Authorize]
    public class TemplatesController : ControllerBase
    {
        private readonly ITemplatesService templatesService;
        private readonly IPreviewService previewService;
        private readonly IHistoryService historyService;
        private readonly GclSettings gclSettings;

        /// <summary>
        /// Creates a new instance of TemplatesController.
        /// </summary>
        public TemplatesController(ITemplatesService templatesService, IOptions<GclSettings> gclSettings, IPreviewService previewService, IHistoryService historyService)
        {
            this.templatesService = templatesService;
            this.previewService = previewService;
            this.historyService = historyService;
            this.gclSettings = gclSettings.Value;
        }

        /// <summary>
        /// Gets the CSS that should be used for HTML editors, so that their content will look more like how it would look on the customer's website.
        /// </summary>
        /// <returns>A string that contains the CSS that should be loaded in the HTML editor.</returns>
        [HttpGet, Route("css-for-html-editors"), ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK), AllowAnonymous]
        public async Task<IActionResult> GetCssForHtmlEditorsAsync([FromQuery] CustomerInformationModel customerInformation)
        {
            // Create a ClaimsIdentity based on query parameters instead the Identity from the bearer token due to being called from an image source where no headers can be set.
            var userId = String.IsNullOrWhiteSpace(customerInformation.encryptedUserId) ? 0 : Int32.Parse(customerInformation.encryptedUserId.Replace(" ", "+").DecryptWithAesWithSalt(gclSettings.DefaultEncryptionKey, true));
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.GroupSid, customerInformation.subDomain ?? "")
            };
            var dummyClaimsIdentity = new ClaimsIdentity(claims);
            //Set the sub domain for the database connection.
            HttpContext.Items[HttpContextConstants.SubDomainKey] = customerInformation.subDomain;

            return (await templatesService.GetCssForHtmlEditorsAsync(dummyClaimsIdentity)).GetHttpResponseMessage("text/css");
        }
        
        /// <summary>
        /// Gets a query from the wiser database and executes it in the customer database.
        /// </summary>
        /// <param name="templateName">The encrypted name of the wiser template.</param>
        [HttpGet, HttpPost, Route("get-and-execute-query/{templateName}"), ProducesResponseType(typeof(JToken), StatusCodes.Status200OK)]
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
        [HttpGet, Route("{parentId:int}/tree-view"), ProducesResponseType(typeof(List<TemplateTreeViewModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> TreeViewAsync(int parentId = 0)
        {
            return (await templatesService.GetTreeViewSectionAsync(parentId)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Retrieve the history of the template. This will include changes made to dynamic content between the releases of templates and the publishes to different environments from this template. This data is collected and combined in a TemnplateHistoryOverviewModel
        /// </summary>
        /// <param name="templateId">The id of the template to retrieve the history from.</param>
        /// <returns>A TemplateHistoryOverviewModel containing a list of templatehistorymodels and a list of publishlogmodels. The model contains base info and a list of changes made within the version and its sub components (e.g. dynamic content, publishes).</returns>
        [HttpGet, Route("{templateId:int}/history"), ProducesResponseType(typeof(TemplateHistoryOverviewModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHistoryAsync(int templateId)
        {
            return (await templatesService.GetTemplateHistoryAsync(templateId)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Retrieve al the preview profiles for an item.
        /// </summary>
        /// <param name="templateId">the id of the item to retrieve the preview items of.</param>
        /// <returns>A list of PreviewProfileModel containing the profiles that are available for the given template</returns>
        [HttpGet, Route("{templateId:int}/preview"), ProducesResponseType(typeof(List<PreviewProfileModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> PreviewTabAsync(int templateId)
        {
            return (await previewService.GetAsync(templateId)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Get the meta data (name, changedOn, changedBy etc) from a template.
        /// </summary>
        /// <param name="templateId">The id of the template to retrieve the data from.</param>
        /// <returns>A <see cref="TemplateSettingsModel"/> containing the current template data of the template with the given id.</returns>
        [HttpGet, Route("{templateId:int}/meta"), ProducesResponseType(typeof(TemplateSettingsModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMetaDataAsync(int templateId)
        {
            return (await templatesService.GetTemplateMetaDataAsync(templateId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Retrieve the latest version of the template. 
        /// </summary>
        /// <param name="templateId">The id of the template to retrieve.</param>
        /// <returns>A template model containing the data of the templateversion.</returns>
        [HttpGet, Route("{templateId:int}/settings"), ProducesResponseType(typeof(TemplateSettingsModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSettingsAsync(int templateId)
        {
            return (await templatesService.GetTemplateSettingsAsync(templateId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Retrieve the published environments of a template, this includes a list of all version options.
        /// </summary>
        /// <param name="templateId">The id of the template from which to retrieve the published environments.</param>
        /// <returns>A published environment model including the versions numbers of the Live, accept and test environment and a list of all possible versions.</returns>
        [HttpGet, Route("{templateId:int}/published-environments"), ProducesResponseType(typeof(PublishedEnvironmentModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPublishedEnvironmentsAsync(int templateId)
        {
            return (await templatesService.GetTemplateEnvironmentsAsync(templateId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Retrieve the templates that are linked to the given templates.
        /// </summary>
        /// <param name="templateId">The id of the template of which to get the linked templates.</param>
        /// <returns>A Linked Templates model containing lists of linked templates separated into lists of certain types (e.g. scss, javascript).</returns>
        [HttpGet, Route("{templateId:int}/linked-templates"), ProducesResponseType(typeof(LinkedTemplatesModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLinkedTemplatesAsync(int templateId)
        {
            return (await templatesService.GetLinkedTemplatesAsync(templateId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Retrieve the dynamic content that is linked to the given template.
        /// </summary>
        /// <param name="templateId">The id of the template of which the linked dynamic content should be retrieved.</param>
        /// <returns>List of dynamic content overview models. This is a condensed version of dynamic content data for creating a overview of linked content.</returns>
        [HttpGet, Route("{templateId:int}/linked-dynamic-content"), ProducesResponseType(typeof(LinkedTemplatesModel), StatusCodes.Status200OK)]
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
        [HttpPost, Route("{templateId:int}/publish/{environment}/{version:int}"), ProducesResponseType(typeof(LinkedTemplatesModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> PublishToEnvironmentAsync(int templateId, string environment, int version)
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
        /// <param name="name">The name to give the template that will be created.</param>
        /// <param name="type">The type of the new template that will be created.</param>
        /// <returns>The id of the newly created template. This can be used to update the interface accordingly.</returns>
        [HttpPut, Route("{parentId:int}"), ProducesResponseType(typeof(TemplateTreeViewModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateAsync(int parentId, [FromQuery]string name, [FromQuery]TemplateTypes type)
        {
            return (await templatesService.CreateAsync((ClaimsIdentity)User.Identity, name, parentId, type)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Save the template as a new version and save the linked templates if necessary. This method will calculate if links are to be added or removed from the current situation.
        /// </summary>
        /// <param name="templateId">The ID of the template to update.</param>
        /// <param name="templateData">A <see cref="TemplateSettingsModel"/> containing the data of the template that is to be saved as a new version</param>
        /// <param name="skipCompilation">Optional: Whether or not to skip the compilations of SCSS templates. Default value is false.</param>
        [HttpPost, Route("{templateId:int}"), ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> SaveAsync(int templateId, TemplateSettingsModel templateData, bool skipCompilation = false)
        {
            templateData.TemplateId = templateId;
            return (await templatesService.SaveTemplateVersionAsync((ClaimsIdentity)User.Identity, templateData, skipCompilation)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Renames a template. This will create a new version of the template with the name, so that we can always see in the history that the name has been changed.
        /// </summary>
        /// <param name="templateId">The ID of the template to rename.</param>
        /// <param name="newName">The new name.</param>
        [HttpPost, Route("{templateId:int}/rename"), ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> RenameAsync(int templateId, [FromQuery]string newName)
        {
            return (await templatesService.RenameAsync((ClaimsIdentity)User.Identity, templateId, newName)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Deletes a template. This will not actually delete it from the database, but add a new version with removed = 1 instead.
        /// </summary>
        /// <param name="templateId">The ID of the template to delete.</param>
        /// <param name="alsoDeleteChildren">Optional: Whether or not to also delete all children of this template. Default value is <see langword="true"/>.</param>
        [HttpDelete, Route("{templateId:int}"), ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteAsync(int templateId, [FromQuery]bool alsoDeleteChildren = true)
        {
            return (await templatesService.DeleteAsync((ClaimsIdentity)User.Identity, templateId, alsoDeleteChildren)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Search for a template.
        /// </summary>
        /// <param name="searchSettings">The search parameters.</param>
        [HttpPost, Route("search"), ProducesResponseType(typeof(List<SearchResultModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchAsync(SearchSettingsModel searchSettings)
        {
            return (await templatesService.SearchAsync(searchSettings)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Moves a template to a new position.
        /// </summary>
        /// <param name="sourceId">The ID of the template that is being moved.</param>
        /// <param name="destinationId">The ID of the template or directory where it's being moved to.</param>
        /// <param name="dropPosition">The drop position, can be either <see cref="TreeViewDropPositions.Over"/>, <see cref="TreeViewDropPositions.Before"/> or <see cref="TreeViewDropPositions.After"/>.</param>
        [HttpPut, Route("{sourceId:int}/move/{destinationId:int}"), ProducesResponseType(typeof(List<SearchResultModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> MoveAsync(int sourceId, int destinationId, [FromQuery]TreeViewDropPositions dropPosition)
        {
            return (await templatesService.MoveAsync((ClaimsIdentity)User.Identity, sourceId, destinationId, dropPosition)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Retrieve al the preview profiles for an item.
        /// </summary>
        /// <param name="templateId">the id of the item to retrieve the preview items of.</param>
        /// <returns>A list of PreviewProfileModel containing the profiles that are available for the given template</returns>
        [HttpGet, Route("{templateId:int}/profiles"), ProducesResponseType(typeof(List<PreviewProfileModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPreviewProfilesAsync(int templateId)
        {
            return (await previewService.GetAsync(templateId)).GetHttpResponseMessage();
        }
        
        /// <summary>
        /// Save a preview profile bound to the current template.
        /// </summary>
        /// <param name="profile">A Json that meets the standards of a PreviewProfileModel</param>
        /// <param name="templateId">The id of the template that is bound to the profile</param>
        /// <returns>An int confirming the affected rows of the query.</returns>
        [HttpPut, Route("{templateId:int}/profiles"), ProducesResponseType(typeof(PreviewProfileModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreatePreviewProfileAsync(int templateId, PreviewProfileModel profile)
        {
            return (await previewService.CreateAsync(profile, templateId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Edit an existing profile. The existing profile with the given Id in the profile will be overwritten.
        /// </summary>
        /// <param name="templateId">The id of the template that is bound to the profile</param>
        /// <param name="profileId">The ID of the profile to update.</param>
        /// <param name="profile">A Json that meets the standards of a PreviewProfileModel</param>
        /// <returns>An int confirming the affected rows of the query.</returns>
        [HttpPost, Route("{templateId:int}/profiles/{profileId:int}"), ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> EditPreviewProfileAsync(int templateId, int profileId, PreviewProfileModel profile)
        {
            profile.Id = profileId;
            return (await previewService.UpdateAsync(profile, templateId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Delete a profile from the database. 
        /// </summary>
        /// <param name="templateId">The id of the template bound to the profile. This is added a an extra security for the deletion.</param>
        /// <param name="profileId">The id of the profile that is to be deleted</param>
        /// <returns>An int confirming the affected rows of the query.</returns>
        [HttpDelete, Route("{templateId:int}/profiles/{profileId:int}"), ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeletePreviewProfilesAsync(int templateId, int profileId)
        {
            return (await previewService.DeleteAsync(templateId, profileId)).GetHttpResponseMessage();
        }

        /// <summary>
        /// Gets the tree view including template settings of all templates.
        /// </summary>
        /// <param name="startFrom">Set the place from which to start the tree view, folders separated by comma.</param>
        /// <returns></returns>
        [HttpGet, Route("entire-tree-view"), ProducesResponseType(typeof(List<TemplateTreeViewModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEntireTreeViewStructureAsync(string startFrom = "")
        {
            return (await templatesService.GetEntireTreeViewStructureAsync(0, startFrom)).GetHttpResponseMessage();
        }
    }
}
