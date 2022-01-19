using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.History;

namespace Api.Modules.Templates.Interfaces
{
    public interface IHistoryService
    {
        Task<List<HistoryVersionModel>> GetChangesInComponent(int templateId);
        Task<int> RevertChanges(List<RevertHistoryModel> changesToRevert, int templateId);
        Task<List<DynamicContentOverviewModel>> GetPublishedEnvoirementsOfOverviewModels(List<DynamicContentOverviewModel> overviewList);
        Task<List<TemplateHistoryModel>> GetVersionHistoryFromTemplate(int templateId, Dictionary<DynamicContentOverviewModel, List<HistoryVersionModel>> dynamicContent);
        Task<List<PublishHistoryModel>> GetPublishHistoryFromTemplate(int templateId);
    }
}