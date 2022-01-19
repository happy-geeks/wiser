using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.Templates.Models.History;
using Api.Modules.Templates.Models.Template;

namespace Api.Modules.Templates.Interfaces.DataLayer
{
    public interface IHistoryDataService
    {
        Task<List<HistoryVersionModel>> GetDynamicContentHistory(int templateId);
        Task<Dictionary<int, int>> GetPublishedEnvoirementsFromDynamicContent(int templateId);
        Task<List<TemplateDataModel>> GetTemplateHistory(int templateId);
        Task<List<PublishHistoryModel>> GetPublishHistoryFromTemplate(int templateId);
    }
}
