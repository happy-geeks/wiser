using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.Template;

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
        /// Save the given variables and their values as a new version in the database.
        /// </summary>
        /// <param name="contentId">The ID of the dynamic content.</param>
        /// <param name="component">The type of component.</param>
        /// <param name="componentMode">The selected component mode.</param>
        /// <param name="title">The given name for the component.</param>
        /// <param name="settings">A dictionary of property names and their values.</param>
        /// <param name="username">The name of the authenticated user.</param>
        /// <returns>An int indicating the result of the executed query.</returns>
        Task<int> SaveSettingsStringAsync(int contentId, string component, string componentMode, string title, Dictionary<string, object> settings, string username);
        
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
        /// <param name="branchDatabaseName">When publishing in a different branch, enter the database name for that branch here.</param>
        /// <returns>A list of all version and their published environment.</returns>
        Task<Dictionary<int, int>> GetPublishedEnvironmentsAsync(int contentId, string branchDatabaseName = null);

        /// <summary>
        /// Publish the dynamic component to an environment. This method will execute the publishmodel instructions it recieves, logic for publishing linked environments should be handled in the servicelayer.
        /// </summary>
        /// <param name="contentId">The id of the component of which the enviroment should be published.</param>
        /// <param name="publishModel">A publish model containing the versions that should be altered and their respective values to be altered with.</param>
        /// <param name="publishLog"></param>
        /// <param name="username">The name of the authenticated user.</param>
        /// <returns>An int confirming the rows altered by the query.</returns>
        Task<int> UpdatePublishedEnvironmentAsync(int contentId, Dictionary<int, int> publishModel, PublishLogModel publishLog, string username, string branchDatabaseName = null);

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
        /// <param name="contentId"> The ID of the dynamic component</param>
        Task DeleteAsync(int contentId);

        /// <summary>
        /// Deploys one or more templates from the main branch to a sub branch.
        /// </summary>
        /// <param name="dynamicContentIds">The IDs of the templates to deploy.</param>
        /// <param name="branchDatabaseName">The name of the database that contains the sub branch.</param>
        Task DeployToBranchAsync(List<int> dynamicContentIds, string branchDatabaseName);
    }
}