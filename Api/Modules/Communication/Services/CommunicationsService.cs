using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Communication.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Modules.Communication.Models;

namespace Api.Modules.Communication.Services;

/// <inheritdoc cref="ICommunicationsService" />
public class CommunicationsService : ICommunicationsService, IScopedService
{
    private readonly GeeksCoreLibrary.Modules.Communication.Interfaces.ICommunicationsService gclCommunicationsService;

    /// <summary>
    /// Creates a new instance of <see cref="CommunicationsService"/>.
    /// </summary>
    public CommunicationsService(GeeksCoreLibrary.Modules.Communication.Interfaces.ICommunicationsService gclCommunicationsService)
    {
        this.gclCommunicationsService = gclCommunicationsService;
    }

    /// <inheritdoc />
    public async Task<ServiceResult<CommunicationSettingsModel>> GetSettingsAsync(int id)
    {
        var result = await gclCommunicationsService.GetSettingsAsync(id);
        return new ServiceResult<CommunicationSettingsModel>(result);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<CommunicationSettingsModel>> SaveSettingsAsync(CommunicationSettingsModel settings)
    {
        var result = await gclCommunicationsService.SaveSettingsAsync(settings);
        return new ServiceResult<CommunicationSettingsModel>(result);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> DeleteSettingsAsync(int id)
    {
        await gclCommunicationsService.DeleteSettingsAsync(id);
        return new ServiceResult<bool>(true);
    }
}