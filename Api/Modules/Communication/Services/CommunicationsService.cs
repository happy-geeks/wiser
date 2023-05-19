using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Services;
using Api.Modules.Communication.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Modules.Communication.Enums;
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
    public async Task<ServiceResult<CommunicationSettingsModel>> GetSettingsAsync(int id, bool nameOnly = false)
    {
        var result = await gclCommunicationsService.GetSettingsAsync(id, nameOnly);
        if (result == null)
        {
            return new ServiceResult<CommunicationSettingsModel>
            {
                StatusCode = HttpStatusCode.NotFound,
                ErrorMessage = $"Communication with ID '{id}' does not exist"
            };
        }

        return new ServiceResult<CommunicationSettingsModel>(result);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<List<CommunicationSettingsModel>>> GetSettingsAsync(CommunicationTypes? type = null, bool namesOnly = false)
    {
        var result = await gclCommunicationsService.GetSettingsAsync(type, namesOnly);
        return new ServiceResult<List<CommunicationSettingsModel>>(result);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<CommunicationSettingsModel>> SaveSettingsAsync(ClaimsIdentity identity, CommunicationSettingsModel settings)
    {
        if (settings.Id > 0 && !await gclCommunicationsService.CommunicationExistsAsync(settings.Id))
        {
            return new ServiceResult<CommunicationSettingsModel>
            {
                StatusCode = HttpStatusCode.NotFound,
                ErrorMessage = $"Communication with ID '{settings.Id}' does not exist"
            };
        }

        var result = await gclCommunicationsService.SaveSettingsAsync(settings, IdentityHelpers.GetUserName(identity, true));
        return new ServiceResult<CommunicationSettingsModel>(result);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> DeleteSettingsAsync(ClaimsIdentity identity, int id)
    {
        if (id > 0 && !await gclCommunicationsService.CommunicationExistsAsync(id))
        {
            return new ServiceResult<bool>
            {
                StatusCode = HttpStatusCode.NotFound,
                ErrorMessage = $"Communication with ID '{id}' does not exist"
            };
        }

        await gclCommunicationsService.DeleteSettingsAsync(id, IdentityHelpers.GetUserName(identity, true));
        return new ServiceResult<bool>(true)
        {
            StatusCode = HttpStatusCode.NoContent
        };
    }

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> SendEmailAsync(ClaimsIdentity identity, SingleCommunicationModel communication)
    {
        await gclCommunicationsService.SendEmailAsync(communication);
        return new ServiceResult<bool>(true)
        {
            StatusCode = HttpStatusCode.NoContent
        };
    }
}