using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Templates.Models.Template;
using GeeksCoreLibrary.Modules.Templates.Enums;

namespace Api.Modules.Templates.Interfaces.DataLayer
{
    /// <summary>
    /// Data service for doing things in database for Wiser templates.
    /// </summary>
    public interface ITemplateDataService
    {
        /// <summary>
        /// Get the meta data (name, changedOn, changedBy etc) from a template.
        /// </summary>
        /// <param name="templateId">The id of the template to retrieve the data from.</param>
        /// <returns>A <see cref="TemplateSettingsModel"/> containing the current template data of the template with the given id.</returns>
        Task<TemplateSettingsModel> GetMetaDataAsync(int templateId);

        /// <summary>
        /// Get the template data of a template.
        /// </summary>
        /// <param name="templateId">The id of the template to retrieve the data from.</param>
        /// <returns>A <see cref="TemplateSettingsModel"/> containing the current template data of the template with the given id.</returns>
        public Task<TemplateSettingsModel> GetDataAsync(int templateId);

        /// <summary>
        /// Get published environments from a template.
        /// </summary>
        /// <param name="templateId">The id of the template which environment should be retrieved.</param>
        /// <returns>A list of all version and their published environment.</returns>
        public Task<Dictionary<int, int>> GetPublishedEnvironmentsAsync(int templateId);

        /// <summary>
        /// Publish the template to an environment. This method will execute the publishmodel instructions it recieves, logic for publishing linked environments should be handled in the servicelayer.
        /// </summary>
        /// <param name="templateId">The id of the template of which the enviroment should be published.</param>
        /// <param name="publishModel">A publish model containing the versions that should be altered and their respective values to be altered with.</param>
        /// <param name="publishLog"></param>
        /// <param name="username">The name of the authenticated user.</param>
        /// <returns>An int confirming the rows altered by the query.</returns>
        public Task<int> UpdatePublishedEnvironmentAsync(int templateId, Dictionary<int, int> publishModel, PublishLogModel publishLog, string username);
        
        /// <summary>
        /// Get the templates linked to the current template and their relation to the current template.
        /// </summary>
        /// <param name="templateId">The id of the template which linked templates should be retrieved.</param>
        /// <returns>Return a list of linked templates in the form of linkedtemplatemodels.</returns>
        public Task<List<LinkedTemplateModel>> GetLinkedTemplatesAsync(int templateId);
        
        /// <summary>
        /// Get templates that can be linked to the current template but aren't linked yet.
        /// </summary>
        /// <param name="templateId">The id of the template for which the linkoptions should be retrieved.</param>
        /// <returns>A list of possible links in the form of linkedtemplatemodels.</returns>
        public Task<List<LinkedTemplateModel>> GetTemplatesAvailableForLinkingAsync(int templateId);
        
        /// <summary>
        /// Get dynamic content that is linked to the current template.
        /// </summary>
        /// <param name="templateId">The id of the template of which the linked dynamic content is to be retrieved.</param>
        /// <returns>A list of dynamic content data for all the dynamic content linked to the current template.</returns>
        public Task<List<LinkedDynamicContentDao>> GetLinkedDynamicContentAsync(int templateId);

        /// <summary>
        /// Saves the template data as a new version of the template.
        /// </summary>
        /// <param name="templateSettings">A <see cref="TemplateSettingsModel"/> containing the new data to save as a new template version.</param>
        /// <param name="linksToAdd"></param>
        /// <param name="linksToRemove"></param>
        /// <param name="username">The name of the authenticated user.</param>
        /// <returns>An int confirming the affected rows of the query.</returns>
        public Task<int> SaveAsync(TemplateSettingsModel templateSettings, List<int> linksToAdd, List<int> linksToRemove, string username);
        
        /// <summary>
        /// Saves the linked templates for a template. This will add new links and remove old links.
        /// </summary>
        /// <param name="templateId">The id of the template who's links to save.</param>
        /// <param name="linksToAdd">The list of template ids to add as a link</param>
        /// <param name="linksToRemove">The list of template ids to remove as a link</param>
        public Task<int> UpdateLinkedTemplatesAsync(int templateId, List<int> linksToAdd, List<int> linksToRemove);
        
        /// <summary>
        /// Retreives a section of the treeview around the given id. In case the id is 0 the root section of the tree will be retrieved.
        /// </summary>
        /// <param name="parentId">The id of the parent element of the treesection that needs to be retrieved</param>
        /// <returns>A list of templatetreeview items that are children of the given id.</returns>
        public Task<List<TemplateTreeViewDao>> GetTreeViewSectionAsync(int parentId);

        /// <summary>
        /// Searches for a template.
        /// </summary>
        /// <param name="searchSettings">The things to search for.</param>
        /// <returns></returns>
        public Task<List<SearchResultModel>> SearchAsync(SearchSettingsModel searchSettings);

        /// <summary>
        /// Creates an empty template with the given name, type and parent template.
        /// </summary>
        /// <param name="name">The name to give the template that will be created.</param>
        /// <param name="parent">The id of the parent template of the template that will be created.</param>
        /// <param name="type">The type of the new template that will be created.</param>
        /// <param name="username">The name of the authenticated user.</param>
        /// <returns>The id of the newly created template. This can be used to update the interface accordingly.</returns>
        Task<int> CreateAsync(string name, int parent, TemplateTypes type, string username);
    }
}
