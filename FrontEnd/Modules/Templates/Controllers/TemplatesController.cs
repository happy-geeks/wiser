using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.History;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Templates.Models.Preview;
using Api.Modules.Templates.Models.Template;
using FrontEnd.Core.Interfaces;
using FrontEnd.Modules.Templates.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace FrontEnd.Modules.Templates.Controllers
{
    [Area("Templates"), Route("Modules/Templates")]
    public class TemplatesController : Controller
    {
        private readonly IBaseService baseService;
        private readonly ITemplatesService templatesService;
        private readonly IPreviewService previewService;
        private readonly IHistoryService historyService;

        public TemplatesController(IBaseService baseService, ITemplatesService templatesService, IPreviewService previewService, IHistoryService historyService)
        {
            this.baseService = baseService;
            this.templatesService = templatesService;
            this.previewService = previewService;
            this.historyService = historyService;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = baseService.CreateBaseViewModel<TemplateOverviewViewModel>();
            viewModel.TreeView = await templatesService.GetTreeViewSectionAsync(0);
            return View(viewModel);
        }

        public async Task<IActionResult> TreeView()
        {
            return PartialView("Partials/TreeView", await templatesService.GetTreeViewSectionAsync(0));
        }

        /// <summary>
        /// Load The development tab.
        /// </summary>
        /// <returns>The partial view of the tab.</returns>
        public async Task<IActionResult> DevelopmentTab(int templateId)
        {
            var model = new DevelopmentTemplateModel
            {
                templateData = await templatesService.GetLatestTemplateVersionAsync(templateId),
                linkedTemplates = await templatesService.GetLinkedTemplatesAsync(templateId)
            };
            return PartialView("Tabs/DevelopmentTab", model);
        }

        /// <summary>
        /// Load the history tab.
        /// </summary>
        /// <returns>The partial view of the tab.</returns>
        public async Task<IActionResult> HistoryTab(int templateId)
        {
            return PartialView("Tabs/HistoryTab", await GetTemplateHistory(templateId));
        }

        public async Task<IActionResult> PublishedEnvironments(int templateId)
        {
            return PartialView("Partials/PublishedEnvironments", await GetLatestTemplateVersion(templateId));
        }

        public async Task<IActionResult> PreviewTab(int templateId)
        {
            return PartialView("Tabs/PreviewTab", await previewService.GetPreviewProfiles(templateId));
        }

        /// <summary>
        /// Retrieve the latest version of the template. 
        /// </summary>
        /// <param name="templateId">The id of the template to retrieve.</param>
        /// <returns>A template model containing the data of the templateversion.</returns>
        [HttpGet]
        public async Task<TemplateDataModel> GetLatestTemplateVersion(int templateId)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }
            return await templatesService.GetLatestTemplateVersionAsync(templateId);
        }

        /// <summary>
        /// Retrieve the published environments of a template, this includes a list of all version options.
        /// </summary>
        /// <param name="templateId">The id of the template from which to retrieve the published environments.</param>
        /// <returns>A published environment model including the versions numbers of the Live, accept and test environment and a list of all possible versions.</returns>
        [HttpGet]
        public async Task<PublishedEnvironmentModel> GetPublishedEnvironments(int templateId)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }
            return await templatesService.GetTemplateEnvironmentsAsync(templateId);
        }

        /// <summary>
        /// Retrieve the templates that are linked to the given templates.
        /// </summary>
        /// <param name="templateId">The id of the template of which to get the linked templates.</param>
        /// <returns>A Linked Templates model containing lists of linked templates seperated into lists of certain types (e.g. scss, javascript).</returns>
        [HttpGet]
        public async Task<LinkedTemplatesModel> GetLinkedTemplates(int templateId)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }
            return await templatesService.GetLinkedTemplatesAsync(templateId);
        }

        /// <summary>
        /// Retrieve the dynamic content that is linked to the given template.
        /// </summary>
        /// <param name="templateId">The id of the template of which the linked dynamic content should be retrieved.</param>
        /// <returns>List of dynamic content overview models. This is a condensed version of dynamic content data for creating a overview of linked content.</returns>
        [HttpGet]
        public async Task<List<DynamicContentOverviewModel>> GetLinkedDynamicContent(int templateId)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }
            List<DynamicContentOverviewModel> resultOverview = await templatesService.GetLinkedDynamicContentAsync(templateId);

            List<DynamicContentOverviewModel> resultList = await historyService.GetPublishedEnvoirementsOfOverviewModels(resultOverview);
            return resultList;
        }

        /// <summary>
        /// Publish a template to a new environment. If moved forward the lower environments will also be moved.
        /// </summary>
        /// <param name="templateId">The id of the template to publish.</param>
        /// <param name="version">The version of the template to publish.</param>
        /// <param name="environment">The environment to push the template version to. This will be converted to a PublishedEnvironmentEnum.</param>
        /// <returns>The number of affected rows.</returns>
        [HttpGet]
        public async Task<int> PublishToEnvironment(int templateId, int version, string environment)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id is invalid");
            }
            if (version <= 0)
            {
                throw new ArgumentException("The version is invalid");
            }
            PublishedEnvironmentModel currentPublished = await templatesService.GetTemplateEnvironmentsAsync(templateId);

            return await templatesService.PublishEnvironmentOfTemplateAsync(templateId, version, environment, currentPublished);
        }

        /// <summary>
        /// Retrieve the history of the template. This will include changes made to dynamic content between the releases of templates and the publishes to different environments from this template. This data is collected and combined in a TemnplateHistoryOverviewModel
        /// </summary>
        /// <param name="templateId">The id of the template to retrieve the history from.</param>
        /// <returns>Retruns a TemplateHistoryOverviewModel containing a list of templatehistorymodels and a list of publishlogmodels. The model contains base info and a list of changes made within the version and its sub components (e.g. dynamic content, publishes).</returns>
        [HttpGet]
        public async Task<TemplateHistoryOverviewModel> GetTemplateHistory(int templateId)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }
            List<DynamicContentOverviewModel> dynamicContentOverview = await templatesService.GetLinkedDynamicContentAsync(templateId);

            Dictionary<DynamicContentOverviewModel, List<HistoryVersionModel>> dynamicContentHistory = new Dictionary<DynamicContentOverviewModel, List<HistoryVersionModel>>();
            foreach (DynamicContentOverviewModel dc in dynamicContentOverview)
            {
                dynamicContentHistory.Add(dc, await historyService.GetChangesInComponent(dc.id));
            }

            TemplateHistoryOverviewModel overview = new TemplateHistoryOverviewModel(templateId, await historyService.GetVersionHistoryFromTemplate(templateId, dynamicContentHistory), await historyService.GetPublishHistoryFromTemplate(templateId))
            {
                publishedEnvironment = await templatesService.GetTemplateEnvironmentsAsync(templateId)
            };

            return overview;
        }

        /// <summary>
        /// Save the template data. This includes the editor value linked templates and advanced settings.
        /// </summary>
        /// <param name="templateData">A JSON string containing a templatedatamodel with the new data to save.</param>
        /// <param name="scssLinks"></param>
        /// <param name="jsLinks"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<int> SaveTemplate(string templateData, List<int> scssLinks, List<int> jsLinks)
        {
            if (String.IsNullOrEmpty(templateData))
            {
                throw new ArgumentException("TemplateData cannot be empty.");
            }
            return await templatesService.SaveTemplateVersionAsync(JsonConvert.DeserializeObject<TemplateDataModel>(templateData), scssLinks, jsLinks);
        }

        /// <summary>
        /// Retrieve a list containing the previewprofiles for a template.
        /// </summary>
        /// <param name="templateId">The id of the template bound to the preview profiles.</param>
        /// <returns>A list of PreviewProfileModels containing the preview profiles for the current template.</returns>
        [HttpGet]
        public async Task<List<PreviewProfileModel>> GetPreviewProfiles(int templateId)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }
            return await previewService.GetPreviewProfiles(templateId);
        }

        /// <summary>
        /// Save a preview profile bound to the current template.
        /// </summary>
        /// <param name="profile">A Json that meets the standards of a PreviewProfileModel</param>
        /// <param name="templateId">The id of the template that is bound to the profile</param>
        /// <returns>An int confirming the affected rows of the query.</returns>
        [HttpPost]
        public async Task<int> SaveNewPreviewProfile(PreviewProfileModel profile, int templateId)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }
            if (profile.id <= 0)
            {
                throw new ArgumentException("The profileId is invalid");
            }
            return await previewService.SaveNewPreviewProfile(profile, templateId);
        }

        /// <summary>
        /// Edit an existing profile. The existing profile with the given Id in the profile will be overwritten.
        /// </summary>
        /// <param name="profile">A Json that meets the standards of a PreviewProfileModel</param>
        /// <param name="templateId">The id of the template that is bound to the profile</param>
        /// <returns>An int confirming the affected rows of the query.</returns>
        [HttpPost]
        public async Task<int> EditPreviewProfile(PreviewProfileModel profile, int templateId)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }
            if (profile.id <= 0)
            {
                throw new ArgumentException("The profileId is invalid");
            }
            return await previewService.EditPreviewProfile(profile, templateId);
        }

        /// <summary>
        /// Delete a profile from the database. 
        /// </summary>
        /// <param name="templateId">The id of the template bound to the profile. This is added a an extra security for the deletion.</param>
        /// <param name="profileId">The id of the profile that is to be deleted</param>
        /// <returns>An int confirming the affected rows of the query.</returns>
        [HttpPost]
        public async Task<int> DeletePreviewProfiles(int templateId, int profileId)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }
            if (profileId <= 0)
            {
                throw new ArgumentException("The profileId is invalid");
            }
            return await previewService.RemovePreviewProfile(templateId, profileId);
        }

        /// <summary>
        /// This can be used to retrieve a section of the Treeview for the customer's templates. When no templateId (or a template id of 0) is supplied the root will be retrieved.
        /// </summary>
        /// <param name="templateId">The id of the folder/template which childElements need to be loaded. When this is left empty or has a value of 0 the root will be retrieved.</param>
        /// <returns>A list of treeviewtemplates containing the section underlaying the given templateId.</returns>
        [HttpGet]
        public async Task<List<TemplateTreeViewModel>> GetTreeviewSection(int templateId)
        {
            if (templateId < 0)
            {
                throw new ArgumentException("The Id is invalid");
            }
            return await templatesService.GetTreeViewSectionAsync(templateId);
        }

        [HttpGet]
        public async Task<List<SearchResultModel>> GetSearchResults(SearchSettingsModel searchSettings)
        {
            return await templatesService.GetSearchResultsAsync(searchSettings);
        }
    }
}
