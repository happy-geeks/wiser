using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Templates.Models;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Templates.Models.Template;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Templates.Interfaces
{
    public interface ITemplatesService
    {
        ServiceResult<Template> Get(int templateId = 0, string templateName = null, string rootName = "");

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
        Task<ServiceResult<TemplateModel>> GetTemplateByNameAsync(string templateName, bool wiserTemplate = false);
		
        Task<TemplateDataModel> GetLatestTemplateVersionAsync(int templateId);
        Task<PublishedEnvironmentModel> GetTemplateEnvironmentsAsync(int templateId);

        Task<LinkedTemplatesModel> GetLinkedTemplatesAsync(int templateId);

        Task<List<DynamicContentOverviewModel>> GetLinkedDynamicContentAsync(int templateId);

        Task<int> PublishEnvironmentOfTemplateAsync(int templateId, int version, string environment, PublishedEnvironmentModel currentPublished);

        Task<int> SaveTemplateVersionAsync(TemplateDataModel template, List<int> scssLinks, List<int> jsLinks);
        Task<List<TemplateTreeViewModel>> GetTreeViewSectionAsync(int parentId);
        Task<List<SearchResultModel>> GetSearchResultsAsync(SearchSettingsModel searchSettings);
    }
}