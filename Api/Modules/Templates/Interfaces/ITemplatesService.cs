using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Kendo.Enums;
using Api.Modules.Templates.Models;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.History;
using Api.Modules.Templates.Models.Measurements;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Templates.Models.Template;
using Api.Modules.Templates.Models.Template.WtsModels;
using GeeksCoreLibrary.Core.Enums;
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
        /// Gets a query from the wiser database and executes it in the tenant database.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="templateName">The encrypted name of the wiser template.</param>
        /// <param name="requestPostData">Optional: The post data from the request, if the content type was application/x-www-form-urlencoded. This is for backwards compatibility.</param>
        Task<ServiceResult<JToken>> GetAndExecuteQueryAsync(ClaimsIdentity identity, string templateName, IFormCollection requestPostData = null);

        /// <summary>
        /// Gets the CSS that should be used for HTML editors, so that their content will look more like how it would look on the tenant's website.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
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
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="templateId">The id of the template.</param>
        /// <param name="environment">The environment the template needs to be active on.</param>
        /// <returns>A <see cref="TemplateSettingsModel"/> containing the template data of the latest version.</returns>
        Task<ServiceResult<TemplateSettingsModel>> GetTemplateSettingsAsync(ClaimsIdentity identity, int templateId, Environments? environment = null);
        
        /// <summary>
        /// Get the latest version of the editor value parsed as an object for a given template.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="templateId">The id of the template.</param>
        /// <param name="environment">The environment the template needs to be active on.</param>
        /// <returns>A <see cref="TemplateWtsConfigurationModel"/> containing the object of the parsed editor value of the latest version.</returns>
        Task<ServiceResult<TemplateWtsConfigurationModel>> GetTemplateWtsConfigurationAsync(ClaimsIdentity identity, int templateId, Environments? environment = null);

        /// <summary>
        /// Get the template environments. This will retrieve a list of versions and their published environments and convert it to a PublishedEnvironmentModel
        /// containing the Live, accept and test versions and the list of other versions that are present in the data.
        /// </summary>
        /// <param name="templateId">The id of the template to retrieve the environments of.</param>
        /// <param name="branchDatabaseName">When publishing in a different branch, enter the database name for that branch here.</param>
        /// <returns>A model containing the versions that are currently set for the live, accept and test environment.</returns>
        Task<ServiceResult<PublishedEnvironmentModel>> GetTemplateEnvironmentsAsync(int templateId, string branchDatabaseName = null);

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
        /// <param name="branchDatabaseName">When publishing in a different branch, enter the database name for that branch here.</param>
        /// <returns>A int of the rows affected.</returns>
        Task<ServiceResult<int>> PublishToEnvironmentAsync(ClaimsIdentity identity, int templateId, int version, Environments environment, PublishedEnvironmentModel currentPublished, string branchDatabaseName = null);

        /// <summary>
        /// Updates the latest version of a template with a new wts configuration. This method will grab the latest version and apply the new wts configuration.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="templateId">The id of the give template.</param>
        /// <param name="configuration">A <see cref="TemplateWtsConfigurationModel"/> containing the data of the configuration that is to be saved as a new version</param>
        Task<ServiceResult<bool>> SaveAsync(ClaimsIdentity identity, int templateId, TemplateWtsConfigurationModel configuration);
        
        /// <summary>
        /// Updates the latest version of a template with new data. This method will overwrite this version, unless this version has been published to the live environment,
        /// then it will create a new version as to not overwrite the live version.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="template">A <see cref="TemplateSettingsModel"/> containing the data of the template that is to be saved as a new version</param>
        /// <param name="skipCompilation">Optional: Whether or not to skip the compilations of SCSS templates. Default value is <see langword="false" />.</param>
        Task<ServiceResult<bool>> SaveAsync(ClaimsIdentity identity, TemplateSettingsModel template, bool skipCompilation = false);

        /// <summary>
        /// Creates a new version of a template by copying the previous version and increasing the version number by one.
        /// </summary>
        /// <param name="templateId">The ID of the template to create a new version for.</param>
        /// <param name="versionBeingDeployed">Optional: If calling this function while deploying the template to an environment, enter the version that is being deployed here. This will then check if that is the latest version and only create a new version of the template if that is the case.</param>
        /// <returns>The ID of the new version.</returns>
        Task<ServiceResult<int>> CreateNewVersionAsync(int templateId, int versionBeingDeployed = 0);

        /// <summary>
        /// Retrieve the tree view section underlying the parentId. Transforms the tree view section into a list of TemplateTreeViewModels.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="parentId">The id of the template whose child nodes are to be retrieved.</param>
        /// <returns>A List of TemplateTreeViewModels containing the id, names and types of the templates included in the requested section.</returns>
        Task<ServiceResult<List<TemplateTreeViewModel>>> GetTreeViewSectionAsync(ClaimsIdentity identity, int parentId);

        /// <summary>
        /// Search for a template.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="searchValue">The value to search for.</param>
        Task<ServiceResult<List<SearchResultModel>>> SearchAsync(ClaimsIdentity identity, string searchValue);

        /// <summary>
        /// Retrieve the history of the template. This will include changes made to dynamic content between the releases of templates and the publishes to different environments from this template. This data is collected and combined in a TemnplateHistoryOverviewModel
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="templateId">The id of the template to retrieve the history from.</param>
        /// <param name="pageNumber">What page number to load</param>
        /// <param name="itemsPerPage">How many versions are being loaded per page</param>
        /// <returns>A TemplateHistoryOverviewModel containing a list of templatehistorymodels and a list of publishlogmodels. The model contains base info and a list of changes made within the version and its sub components (e.g. dynamic content, publishes).</returns>
        Task<ServiceResult<TemplateHistoryOverviewModel>> GetTemplateHistoryAsync(ClaimsIdentity identity, int templateId, int pageNumber, int itemsPerPage);

        /// <summary>
        /// Creates an empty template with the given name, type and parent template.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="name">The name to give the template that will be created.</param>
        /// <param name="parent">The id of the parent template of the template that will be created.</param>
        /// <param name="type">The type of the new template that will be created.</param>
        /// <param name="editorValue"> The optional editorValue of the template, this can be used for importing files.</param>
        /// <returns>The id of the newly created template. This can be used to update the interface accordingly.</returns>
        Task<ServiceResult<TemplateTreeViewModel>> CreateAsync(ClaimsIdentity identity, string name, int parent, TemplateTypes type, string editorValue = "");

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
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="parentId">The ID of the parent item.</param>
        /// <param name="startFrom">Set the place from which to start the tree view, folders separated by comma.</param>
        /// <param name="environment">The environment the template needs to be active on.</param>
        /// <returns></returns>
        Task<ServiceResult<List<TemplateTreeViewModel>>> GetEntireTreeViewStructureAsync(ClaimsIdentity identity, int parentId, string startFrom, Environments? environment = null);

        /// <summary>
        /// Deletes a template. This will not actually delete it from the database, but add a new version with removed = 1 instead.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="templateId">The ID of the template to delete.</param>
        /// <param name="alsoDeleteChildren">Optional: Whether or not to also delete all children of this template. Default value is <see langword="true"/>.</param>
        Task<ServiceResult<bool>> DeleteAsync(ClaimsIdentity identity, int templateId, bool alsoDeleteChildren = true);

        /// <summary>
        /// Checks if there's a conflict with another template that's also marked as a default header with the given regex.
        /// </summary>
        /// <param name="templateId">ID of the current template.</param>
        /// <param name="regexString">The regular expression that can filter whether the default header should be used.</param>
        /// <returns>A string with the name of the template that this template conflicts with, or an empty string if there's no conflict.</returns>
        Task<ServiceResult<string>> CheckDefaultHeaderConflict(int templateId, string regexString);

        /// <summary>
        /// Checks if there's a conflict with another template that's also marked as a default footer with the given regex.
        /// </summary>
        /// <param name="templateId">ID of the current template.</param>
        /// <param name="regexString">The regular expression that can filter whether the default footer should be used.</param>
        /// <returns>A string with the name of the template that this template conflicts with, or an empty string if there's no conflict.</returns>
        Task<ServiceResult<string>> CheckDefaultFooterConflict(int templateId, string regexString);

        /// <summary>
        /// Attempt to retrieve a virtual template, which is either a database routine, view, or trigger.
        /// </summary>
        /// <param name="objectName">The name of the routine, view, or trigger.</param>
        /// <param name="templateType">The type of virtual template.</param>
        /// <returns>A <see cref="TemplateSettingsModel"/> with data about the virtual template.</returns>
        Task<ServiceResult<TemplateSettingsModel>> GetVirtualTemplateAsync(string objectName, TemplateTypes templateType);

        /// <summary>
        /// Retrieves a list of table names for the trigger templates.
        /// </summary>
        /// <returns>A list of strings.</returns>
        Task<ServiceResult<IList<string>>> GetTableNamesForTriggerTemplatesAsync();

        /// <summary>
        /// Deploy one or more templates to a branch.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="templateIds">The IDs of the templates to deploy.</param>
        /// <param name="branchId">The ID of the branch to deploy the templates to.</param>
        Task<ServiceResult<bool>> DeployToBranchAsync(ClaimsIdentity identity, List<int> templateIds, int branchId);

        /// <summary>
        /// Get the settings for measurements of a template. You have to specify either a template ID or a component ID, not both.
        /// </summary>
        /// <param name="templateId">The ID of the template to get the settings of.</param>
        /// <param name="componentId">The ID of the dynamic content to get the settings of.</param>
        /// <returns>The measurement settings of the template.</returns>
        Task<ServiceResult<MeasurementSettings>> GetMeasurementSettingsAsync(int templateId = 0, int componentId = 0);

        /// <summary>
        /// Save the settings for measurements of this template. You have to specify either a template ID or a component ID, not both.
        /// </summary>
        /// <param name="templateId">The ID of the template to save the settings for.</param>
        /// <param name="componentId">The ID of the dynamic content to save the settings for.</param>
        /// <param name="settings">The new settings.</param>
        Task<ServiceResult<bool>> SaveMeasurementSettingsAsync(MeasurementSettings settings, int templateId = 0, int componentId = 0);

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
        Task<ServiceResult<List<RenderLogModel>>> GetRenderLogsAsync(int templateId, int version = 0,
            string urlRegex = null, Environments? environment = null, ulong userId = 0,
            string languageCode = null, int pageSize = 500, int pageNumber = 1,
            bool getDailyAverage = false, DateTime? start = null, DateTime? end = null);

        /// <summary>
        /// Converts a JCL template to a GCL template.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        Task<ServiceResult<bool>> ConvertLegacyTemplatesToNewTemplatesAsync(ClaimsIdentity identity);
    }
}