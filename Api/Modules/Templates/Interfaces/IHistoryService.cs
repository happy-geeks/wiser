using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.History;
using Api.Modules.Templates.Models.Other;

namespace Api.Modules.Templates.Interfaces
{
    /// <summary>
    /// Service for the history of Wiser templates.
    /// </summary>
    public interface IHistoryService
    {
        /// <summary>
        /// Retrieve history of the component with generated changes. The versions will be sorted by the HistoryVersion models version(DESC).
        /// </summary>
        /// <param name="contentId">The id of the content</param>
        /// <param name="pageNumber">What page number to load</param>
        /// <param name="itemsPerPage">How many versions are being loaded per page</param>
        /// <returns>List of HistoryVersionModels with generated changes. Sorted by descending version.</returns>
        Task<ServiceResult<List<HistoryVersionModel>>> GetChangesInComponentAsync(int contentId, int pageNumber, int itemsPerPage);

        /// <summary>
        /// Retrieves the current settings and applies the List of changes that should be reverted.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="contentId">The id of the content</param>
        /// <param name="changesToRevert">Contains the properties and specific versions that need to be reverted.</param>
        /// <returns>The ID of the component that was reverted.</returns>
        Task<ServiceResult<int>> RevertChangesAsync(ClaimsIdentity identity, int contentId, List<RevertHistoryModel> changesToRevert);

        /// <summary>
        /// Retrieve the published environments for dynamic content overviews. This method will accept a list of DynamicContentOverviewModel and retrieve the published environments for each dynamic content.
        /// </summary>
        /// <param name="overviewList">A list of DynamicContentOverviewModels which are to be supplied with published environments</param>
        /// <returns>The list of DynamicContentOverviewModels containing the published environments for each model</returns>
        Task<List<DynamicContentOverviewModel>> GetPublishedEnvironmentsOfOverviewModels(List<DynamicContentOverviewModel> overviewList);

        /// <summary>
        /// Retrieves a list of versions for the dynamic content containing their publish status and transforms it to a PublishedEnvironmentModel. 
        /// </summary>
        /// <param name="templateId">The id of the content.</param>
        /// <returns>A PublishedEnvironmentModel containing the published environments of dynamic content</returns>
        Task<PublishedEnvironmentModel> GetHistoryVersionsOfDynamicContent(int templateId);

        /// <summary>
        /// Retrieve the history of a template. This will start by retrieving the history of the template. 
        /// When comparing the settings for changes the linked dynamic content will be checked for changes during this version. Any changes found in the linked dynamic content will be added to the template history.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="templateId">The id of the template.</param>
        /// <param name="dynamicContent">A Dictionary containing the overview of dynamic content and its respective history</param>
        /// <param name="pageNumber">What page number to load</param>
        /// <param name="itemsPerPage">How many versions are being loaded per page</param>
        /// <returns>A list of TemplateHistoryModel containing the history of the template and its linked dynamic content for each version</returns>
        Task<List<TemplateHistoryModel>> GetVersionHistoryFromTemplate(ClaimsIdentity identity, int templateId, Dictionary<DynamicContentOverviewModel, List<HistoryVersionModel>> dynamicContent, int pageNumber, int itemsPerPage);

        /// <summary>
        /// Retrieves the publish history of a template
        /// </summary>
        /// <param name="templateId">The id of a template</param>
        /// <param name="pageNumber">What page number to load</param>
        /// <param name="itemsPerPage">How many versions are being loaded per page</param>
        /// <returns></returns>
        Task<List<PublishHistoryModel>> GetPublishHistoryFromTemplate(int templateId, int pageNumber, int itemsPerPage);
    }
}