using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Templates.Models.Template;

namespace Api.Modules.Templates.Interfaces.DataLayer
{
    public interface ITemplateDataService
    {
        public Task<TemplateDataModel> GetTemplateData(int templateId);
        public Task<Dictionary<int, int>> GetPublishedEnvironmentsFromTemplate(int templateId);

        public Task<List<LinkedTemplateModel>> GetLinkedTemplates(int templateId);
        public Task<List<LinkedTemplateModel>> GetLinkOptionsForTemplate(int templateId);

        public Task<List<LinkedDynamicContentDao>> GetLinkedDynamicContent(int templateId);
        public Task<int> PublishEnvironmentOfTemplate(int templateId, Dictionary<int, int> publishModel, PublishLogModel publishLog);
        public Task<int> SaveTemplateVersion(TemplateDataModel templateData, List<int> linksToAdd, List<int> linksToRemove);

        public Task<int> SaveLinkedTemplates(int templateId, List<int> linksToAdd, List<int> linksToRemove);

        public Task<List<TemplateTreeViewDao>> GetTreeViewSection(int parentId);
        public Task<List<SearchResultModel>> GetSearchResults(SearchSettingsModel searchSettings);
    }
}
