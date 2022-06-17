using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Interfaces.DataLayer
{
    /// <summary>
    /// Data service for handeling data related to the template items in the version control model.
    /// </summary>
    public interface ITemplateContainerDataService
    {

        /// <summary>
        /// Gets the templates that have a lower version than the given one.
        /// </summary>
        /// <param name="templateId">The id of the template.</param>
        /// <param name="version">The version of the template.</param>
        /// <returns>Returns a dictionary with the templates that are a lower version than the given one.</returns>
        Task<Dictionary<int, int>> GetTemplatesWithLowerVersionAsync(int templateId, int version);

        /// <summary>
        /// Creates a new commit of the template.
        /// </summary>
        /// <param name="templateCommitModel">The data of the template and the commit.</param>
        /// <returns>Returns a bool.</returns>
        Task<bool> CreateNewTemplateCommitAsync(TemplateCommitModel templateCommitModel);

        /// <summary>
        /// Updates a commited template.
        /// </summary>
        /// <param name="templateCommitModel">The data of the template with the data that needs to be changed.</param>
        /// <returns>Returns a bool.</returns>
        Task<bool> UpdateTemplateCommitAsync(TemplateCommitModel templateCommitModel);

        /// <summary>
        /// Updates the published environment of the given template.
        /// </summary>
        /// <param name="templateId">The id of the template.</param>
        /// <param name="publishNumber">The environment to publish to.</param>
        /// <returns>Returns a bool.</returns>
        Task<bool> UpdatePublishEnvironmentTemplateAsync(int templateId, int publishNumber);

        /// <summary>
        /// Gets the current published environments of the given template.
        /// </summary>
        /// <param name="templateId">The id of the template.</param>
        /// <param name="version">The version of the template.</param>
        /// <returns>Returns a model with all the templates that are currently published.</returns>
        Task<TemplateEnvironments> GetCurrentPublishedEnvironmentAsync(int templateId, int version);
    }
}
