using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.Templates.Models.Preview;

namespace Api.Modules.Templates.Interfaces.DataLayer
{
    public interface IPreviewDataService
    {
        public Task<List<PreviewProfileDAO>> GetPreviewProfiles(int templateId);
        public Task<int> SaveNewPreviewProfile(PreviewProfileDAO profile, int templateId);
        public Task<int> EditPreviewProfile(PreviewProfileDAO profile, int templateId);
        public Task<int> RemovePreviewProfile(int templateId, int profileId);
    }
}
