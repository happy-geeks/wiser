﻿using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Kendo.Enums;
using Api.Modules.Templates.Models;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.History;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Templates.Models.Template;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Templates.Interfaces
{
    /// <summary>
    /// A service for doing things with templates from the templates module in Wiser.
    /// </summary>
    public interface ITemplatesService
    {
        /// <summary>
        /// Gets a template by either name or ID.
        /// </summary>
        /// <param name="templateId">Optional: The ID of the template to get.</param>
        /// <param name="templateName">Optional: The name of the template to get.</param>
        /// <param name="rootName">Optional: The name of the root directory to look in.</param>
        /// <returns>A Template.</returns>
        ServiceResult<Template> Get(int templateId = 0, string templateName = null, string rootName = "");

        /// <summary>
        /// Get a query template by either name or ID.
        /// </summary>
        /// <param name="templateId">Optional: The ID of the template to get.</param>
        /// <param name="templateName">Optional: The name of the template to get.</param>
        /// <returns>A QueryTemplate</returns>
        Task<ServiceResult<QueryTemplate>> GetQueryAsync(int templateId = 0, string templateName = null);

        /// <summary>
        /// Gets a query from the wiser database and executes it in the customer database.
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="templateName">The encrypted name of the wiser template.</param>
        /// <param name="requestPostData">Optional: The post data from the request, if the content type was application/x-www-form-urlencoded. This is for backwards compatibility.</param>
        Task<ServiceResult<JToken>> GetAndExecuteQueryAsync(ClaimsIdentity identity, string templateName, IFormCollection requestPostData = null);
        
        /// <summary>
        /// Gets the CSS that should be used for HTML editors, so that their content will look more like how it would look on the customer's website.
        /// </summary>
        /// <returns>A string that contains the CSS that should be loaded in the HTML editor.</returns>
        Task<ServiceResult<string>> GetCssForHtmlEditorsAsync(ClaimsIdentity identity);

        /// <summary>
        /// Gets a template by name.
        /// </summary>
        /// <param name="templateName">The name of the template.</param>
        /// <param name="wiserTemplate">Optional: If true the template will be tried to be found within Wiser instead of the database of the user.</param>
        /// <returns></returns>
        Task<ServiceResult<TemplateEntityModel>> GetTemplateByNameAsync(string templateName, bool wiserTemplate = false);

        /// <summary>
        /// Get the meta data (name, changedOn, changedBy etc) from a template.
        /// </summary>
        /// <param name="templateId">The id of the template to retrieve the data from.</param>
        /// <returns>A <see cref="TemplateSettingsModel"/> containing the current template data of the template with the given id.</returns>
        Task<ServiceResult<TemplateSettingsModel>> GetTemplateMetaDataAsync(int templateId);
		
        /// <summary>
        /// Get the latest version for a given template.
        /// </summary>
        /// <param name="templateId">The id of the template.</param>
        /// <returns>A <see cref="TemplateSettingsModel"/> containing the template data of the latest version.</returns>
        Task<ServiceResult<TemplateSettingsModel>> GetTemplateSettingsAsync(int templateId);
        
        /// <summary>
        /// Get the template environments. This will retrieve a list of versions and their published environments and convert it to a PublishedEnvironmentModel 
        /// containing the Live, accept and test versions and the list of other versions that are present in the data.
        /// </summary>
        /// <param name="templateId">The id of the template to retrieve the environments of.</param>
        /// <returns>A model containing the versions that are currently set for the live, accept and test environment.</returns>
        Task<ServiceResult<PublishedEnvironmentModel>> GetTemplateEnvironmentsAsync(int templateId);
        
        /// <summary>
        /// Get the templates linked to the current template. The templates that are retrieved will be converted into a LinkedTemplatesModel using the LinkedTemplatesEnum to determine its link type.
        /// </summary>
        /// <param name="templateId">The id of the template of which to retrieve the linked templates.</param>
        /// <returns>A LinkedTemplates model that contains several lists of linked templates divided by their link type. (e.g. javascript, css)</returns>
        Task<ServiceResult<LinkedTemplatesModel>> GetLinkedTemplatesAsync(int templateId);
        
        /// <summary>
        /// Get the dynamic content that is linked to the current template. This method will convert the linked dynamic content data into a dynamic content overview which can be used for displaying a general overview of the dynamic content.
        /// </summary>
        /// <param name="templateId">The id of the template to of which to retrieve the linked dynamic content.</param>
        /// <returns>A list of overviews for dynamic content. All content in the list is linked to the current template.</returns>
        Task<ServiceResult<List<DynamicContentOverviewModel>>> GetLinkedDynamicContentAsync(int templateId);
        
        /// <summary>
        /// Publish a template version to a new environment using a template id. This requires you to provide a model with the current published state.
        /// This method will use a generated change log to determine the environments that need to be changed. In some cases publishing an environment will also publish underlaying environments.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="templateId">The id of the template to publish.</param>
        /// <param name="version">The version of the template to publish.</param>
        /// <param name="environment">The environment to publish the template to.</param>
        /// <param name="currentPublished">A PublishedEnvironmentModel containing the current published templates.</param>
        /// <returns>A int of the rows affected.</returns>
        Task<ServiceResult<int>> PublishToEnvironmentAsync(ClaimsIdentity identity, int templateId, int version, string environment, PublishedEnvironmentModel currentPublished);

        /// <summary>
        /// Save the template as a new version and save the linked templates if necessary. This method will calculate if links are to be added or removed from the current situation.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="template">A <see cref="TemplateSettingsModel"/> containing the data of the template that is to be saved as a new version</param>
        /// <param name="skipCompilation">Optional: Whether or not to skip the compilations of SCSS templates. Default value is <see langword="false" />.</param>
        Task<ServiceResult<bool>> SaveTemplateVersionAsync(ClaimsIdentity identity, TemplateSettingsModel template, bool skipCompilation = false);
        
        /// <summary>
        /// Retrieve the tree view section underlying the parentId. Transforms the tree view section into a list of TemplateTreeViewModels.
        /// </summary>
        /// <param name="parentId">The id of the template whose child nodes are to be retrieved.</param>
        /// <returns>A List of TemplateTreeViewModels containing the id, names and types of the templates included in the requested section.</returns>
        Task<ServiceResult<List<TemplateTreeViewModel>>> GetTreeViewSectionAsync(int parentId);

        /// <summary>
        /// Search for a template.
        /// </summary>
        /// <param name="searchSettings">The search parameters.</param>
        Task<ServiceResult<List<SearchResultModel>>> SearchAsync(SearchSettingsModel searchSettings);
        
        /// <summary>
        /// Retrieve the history of the template. This will include changes made to dynamic content between the releases of templates and the publishes to different environments from this template. This data is collected and combined in a TemnplateHistoryOverviewModel
        /// </summary>
        /// <param name="templateId">The id of the template to retrieve the history from.</param>
        /// <returns>A TemplateHistoryOverviewModel containing a list of templatehistorymodels and a list of publishlogmodels. The model contains base info and a list of changes made within the version and its sub components (e.g. dynamic content, publishes).</returns>
        Task<ServiceResult<TemplateHistoryOverviewModel>> GetTemplateHistoryAsync(int templateId);

        /// <summary>
        /// Creates an empty template with the given name, type and parent template.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="name">The name to give the template that will be created.</param>
        /// <param name="parent">The id of the parent template of the template that will be created.</param>
        /// <param name="type">The type of the new template that will be created.</param>
        /// <returns>The id of the newly created template. This can be used to update the interface accordingly.</returns>
        Task<ServiceResult<TemplateTreeViewModel>> CreateAsync(ClaimsIdentity identity, string name, int parent, TemplateTypes type);

        /// <summary>
        /// Renames a template. This will create a new version of the template with the name, so that we can always see in the history that the name has been changed.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="id">The ID of the template to rename.</param>
        /// <param name="newName">The new name.</param>
        Task<ServiceResult<bool>> RenameAsync(ClaimsIdentity identity, int id, string newName);

        /// <summary>
        /// Moves a template to a new position.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="sourceId">The ID of the template that is being moved.</param>
        /// <param name="destinationId">The ID of the template or directory where it's being moved to.</param>
        /// <param name="dropPosition">The drop position, can be either <see cref="TreeViewDropPositions.Over"/>, <see cref="TreeViewDropPositions.Before"/> or <see cref="TreeViewDropPositions.After"/>.</param>
        Task<ServiceResult<bool>> MoveAsync(ClaimsIdentity identity, int sourceId, int destinationId, TreeViewDropPositions dropPosition);

        /// <summary>
        /// Gets the tree view including template settings of all templates.
        /// </summary>
        /// <param name="startFrom">Set the place from which to start the tree view, folders separated by comma.</param>
        /// <returns></returns>
        Task<ServiceResult<List<TemplateTreeViewModel>>> GetEntireTreeViewStructureAsync(int parentId, string startFrom);
    }
}