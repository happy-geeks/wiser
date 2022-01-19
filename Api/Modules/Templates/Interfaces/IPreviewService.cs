using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.Templates.Models.Preview;

namespace Api.Modules.Templates.Interfaces
{
    public interface IPreviewService
    {
        public Task<List<PreviewProfileModel>> GetPreviewProfiles(int templateId);
        public Task<int> SaveNewPreviewProfile(PreviewProfileModel profile, int templateId);
        public Task<int> EditPreviewProfile(PreviewProfileModel profile, int templateId);
        public Task<int> RemovePreviewProfile(int templateId, int profileId);
    }
}
