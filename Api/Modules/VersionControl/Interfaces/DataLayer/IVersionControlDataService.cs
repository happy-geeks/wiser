using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Interfaces.DataLayer
{
    /// <summary>
    /// Data service for handeling general data of the version control model.
    /// </summary>
    public interface IVersionControlDataService
    {
        /// <summary>
        /// Gets all the published templates.
        /// </summary>
        /// <returns>Returns a dictionary with all the templates that are currently published. The first value will be the template id and the second one is the version.</returns>
        Task<Dictionary<int,int>> GetPublishedTemplateIdAndVersionAsync();
      
        /// <summary>
        /// Creates a log with the changes made to the template.
        /// </summary>
        /// <param name="templateId">The id of the template.</param>
        /// <param name="version">The version of the template.</param>
        /// <returns></returns>
        Task<bool> CreatePublishLog(int templateId, int version);

        /// <summary>
        /// Gets all the templates from a specific commit.
        /// </summary>
        /// <param name="commitId">The id of the commit.</param>
        /// <returns>Returns a list with templates.</returns>
        Task<List<TemplateCommitModel>> GetTemplatesFromCommitAsync(int commitId);

        /// <summary>
        /// Gets the dynamic content items from the given commit.
        /// </summary>
        /// <param name="commitId">The id of the commit.</param>
        /// <returns>Returns a list of dynamic contnet items.</returns>
        Task<List<DynamicContentCommitModel>> GetDynamicContentFromCommitAsync(int commitId);

        /// <summary>
        /// Gets all the dynamic content items that belong to a specific template.
        /// </summary>
        /// <param name="templateId">The id of the template.</param>
        /// <returns>Returns a list of dynamic content items</returns>
        Task<List<DynamicContentModel>> GetDynamicContentInTemplateAsync(int templateId);

        
    }
}
