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
        /// <returns>A list of all version and their published environment.</returns>
        Task<Dictionary<int, int>> GetPublishedEnvironmentsAsync(int contentId);

        /// <summary>
        /// Publish the dynamic component to an environment. This method will execute the publishmodel instructions it recieves, logic for publishing linked environments should be handled in the servicelayer.
        /// </summary>
        /// <param name="contentId">The id of the template of which the enviroment should be published.</param>
        /// <param name="publishModel">A publish model containing the versions that should be altered and their respective values to be altered with.</param>
        /// <param name="publishLog"></param>
        /// <param name="username">The name of the authenticated user.</param>
        /// <returns>An int confirming the rows altered by the query.</returns>
        Task<int> UpdatePublishedEnvironmentAsync(int contentId, Dictionary<int, int> publishModel, PublishLogModel publishLog, string username);
    }
}