using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Modules.Templates.Interfaces.DataLayer
{
    public interface IDynamicContentDataService
    {
        Task<KeyValuePair<string, Dictionary<string, object>>> GetTemplateData(int templateId);
        Task<int> SaveSettingsString(int templateid, string component, string componentMode, string templateName, Dictionary<string, object> settings);

        Task<KeyValuePair<string, Dictionary<string, object>>> GetVersionData(int version, int templateId);

        Task<List<string>> GetComponentAndModeFromContentId(int contentId);
    }
}