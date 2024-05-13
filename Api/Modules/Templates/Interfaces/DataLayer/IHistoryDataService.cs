using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.Templates.Models.History;
using Api.Modules.Templates.Models.Template;

namespace Api.Modules.Templates.Interfaces.DataLayer
{
    /// <summary>
    /// Data service for all queries that need to do something with the history of Wiser templates.
    /// </summary>
    public interface IHistoryDataService
    {
        /// <summary>
        /// Returns the components history as a dictionary.
        /// </summary>
        /// <returns></returns>
        Task<List<HistoryVersionModel>> GetDynamicContentHistoryAsync(int contentId, int page, int itemsPerPage);

        /// <summary>
        /// Get a list of versions and their published environments form a dynamic content.
        /// </summary>
        /// <param name="contentId">The id of the dynamic content.</param>
        /// <returns>List of version numbers and their published environment.</returns>
        Task<Dictionary<int, int>> GetPublishedEnvironmentsFromDynamicContentAsync(int contentId);
        
        /// <summary>
        /// Get the history of a template. This will retrieve all versions of the template which can be compared for changes.
        /// </summary>
        /// <param name="templateId">The id of the template which history should be retrieved.</param>
        /// <returns>A list of <see cref="TemplateSettingsModel"/> forming the history of the template. The list is ordered by version number (DESC).</returns>
        Task<List<TemplateSettingsModel>> GetTemplateHistoryAsync(int templateId, int page, int itemsPerPage);

        /// <summary>
        /// Get the history of a template from the publish log table. The list will be ordered on date desc.
        /// </summary>
        /// <param name="templateId">The Id of the template whose history to retrieve</param>
        /// <param name="page"></param>
        /// <param name="itemsPerPage"></param>
        /// <returns>A list of <see cref="PublishHistoryModel"/> containing the values of the change from the publish log datatable.</returns>
        Task<List<PublishHistoryModel>> GetPublishHistoryFromTemplateAsync(int templateId, int page, int itemsPerPage);
    }
}
