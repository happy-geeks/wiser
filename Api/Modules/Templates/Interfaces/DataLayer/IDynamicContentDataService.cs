using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.Template;
using Api.Modules.Tenants.Models;
using GeeksCoreLibrary.Core.Enums;

namespace Api.Modules.Templates.Interfaces.DataLayer
{
    /// <summary>
    /// Data service for doing things in the database with/for dynamic content components.
    /// </summary>
    public interface IDynamicContentDataService
    {
        /// <summary>
        /// Gets all dynamic content that can be linked to the given template.
        /// </summary>
        /// <param name="templateId">The ID of the template.</param>
        /// <returns>A list of dynamic components from other templates.</returns>
        Task<List<DynamicContentOverviewModel>> GetLinkableDynamicContentAsync(int templateId);

        /// <summary>
        /// Retrieve the variable data of a set version. This can be used for retrieving past versions or the current version if the version number is known.
        /// </summary>
        /// <param name="version">The version number to distinguish the values by</param>
        /// <param name="contentId">The ID of the dynamic content.</param>
        /// <returns>Dictionary of property names and their values in the given version.</returns>
        Task<KeyValuePair<string, Dictionary<string, object>>> GetVersionDataAsync(int version, int contentId);

        /// <summary>
        /// Get the type data from the database associated with the given type.
        /// </summary>
        /// <param name="contentId">The ID of the dynamic content.</param>
        /// <returns>Dictionary containing the properties and their values.</returns>
        Task<KeyValuePair<string, Dictionary<string, object>>> GetComponentDataAsync(int contentId);

        /// <summary>
        /// Get the ID, version number and published environment of the latest version of a component.
        /// </summary>
        /// <param name="contentId">The template ID.</param>
        /// <returns>The ID, version number, published environment of the template and if it is removed.</returns>
        Task<(int Id, int Version, Environments Environment, bool Removed)> GetLatestVersionAsync(int contentId);

        /// <summary>
        /// Updates the latest version of a template with new data. This method will overwrite this version, unless this version has been published to the live environment,
        /// then it will create a new version as to not overwrite the live version.
        /// </summary>
        /// <param name="contentId">The ID of the dynamic content.</param>
        /// <param name="component">The type of component.</param>
        /// <param name="componentMode">The selected component mode.</param>
        /// <param name="title">The given name for the component.</param>
        /// <param name="settings">A dictionary of property names and their values.</param>
        /// <param name="username">The name of the authenticated user.</param>=
        Task<int> SaveAsync(int contentId, string component, string componentMode, string title, Dictionary<string, object> settings, string username);

        /// <summary>
        /// Creates a new version of a dynamic component by copying the previous version and increasing the version number by one.
        /// </summary>
        /// <param name="contentId">The content ID of the dynamic component.</param>
        /// <returns>The ID of the new version.</returns>
        Task<int> CreateNewVersionAsync(int contentId);

        /// <summary>
        /// Save the given variables and their values as a new version in the database.
        /// </summary>
        /// <param name="contentId">The ID of the dynamic content.</param>
        /// <returns>The name of the component that is used.</returns>
        Task<List<string>> GetComponentAndModeFromContentIdAsync(int contentId);

        /// <summary>
        /// Gets the meta data (name, component type etc) of a component.
        /// </summary>
        /// <param name="contentId">The ID of the component.</param>
        Task<DynamicContentOverviewModel> GetMetaDataAsync(int contentId);

        /// <summary>
        /// Links a dynamic content to a template.
        /// </summary>
        /// <param name="contentId">The ID of the dynamic content.</param>
        /// <param name="templateId">The ID of the template.</param>
        /// <param name="username">The name of the user that is adding the link.</param>
        Task AddLinkToTemplateAsync(int contentId, int templateId, string username);

        /// <summary>
        /// Get published environments from a dynamic component.
        /// </summary>
        /// <param name="contentId">The id of the dynamic component for which the environment should be retrieved.</param>
        /// <param name="branch">When publishing in a different branch, enter the information for that branch here.</param>
        /// <returns>A list of all version and their published environment.</returns>
        Task<Dictionary<int, int>> GetPublishedEnvironmentsAsync(int contentId, TenantModel branch = null);

        /// <summary>
        /// Publish the dynamic component to an environment. This method will execute the publishmodel instructions it recieves, logic for publishing linked environments should be handled in the servicelayer.
        /// </summary>
        /// <param name="contentId">The id of the component of which the environment should be published.</param>
        /// <param name="version">The version that should be deployed/published.</param>
        /// <param name="environment">The environment to publish the version of the template to.</param>
        /// <param name="publishLog">Information for the history of the template, to log the version change there.</param>
        /// <param name="username">The name of the authenticated user.</param>
        /// <param name="branch">When publishing in a different branch, enter the information for that branch here.</param>
        /// <returns>An int confirming the rows altered by the query.</returns>
        Task<int> UpdatePublishedEnvironmentAsync(int contentId, int version, Environments environment, PublishLogModel publishLog, string username, TenantModel branch = null);

        /// <summary>
        /// Duplicates a dynamic component (only the latest version).
        /// </summary>
        /// <param name="contentId">The ID of the component.</param>
        /// <param name="newTemplateId">The id of the template to link the new component to.</param>
        /// <param name="username">The name of the authenticated user.</param>
        Task DuplicateAsync(int contentId, int newTemplateId, string username);

        /// <summary>
        /// Deletes a dynamic component by putting the field 'removed' to 1
        /// </summary>
        /// <param name="username">The name of the user deleting the dynamic component.</param>
        /// <param name="contentId"> The ID of the dynamic component</param>
        Task DeleteAsync(string username, int contentId);

        /// <summary>
        /// Deploys one or more components from the main branch to a sub-branch.
        /// </summary>
        /// <param name="dynamicContentIds">The IDs of the templates to deploy.</param>
        /// <param name="branch">The information about the tenant to deploy to.</param>
        Task DeployToBranchAsync(List<int> dynamicContentIds, TenantModel branch);

        /// <summary>
        /// Function that makes sure that the database tables needed for components are up to date with the latest changes.
        /// </summary>
        Task KeepTablesUpToDateAsync();
    }
}