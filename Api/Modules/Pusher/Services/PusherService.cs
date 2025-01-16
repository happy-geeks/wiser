using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Api.Core.Models;
using Api.Core.Services;
using Api.Modules.Pusher.Interfaces;
using Api.Modules.Pusher.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using Microsoft.Extensions.Options;
using PusherServer;

namespace Api.Modules.Pusher.Services;

//TODO Verify comments
/// <summary>
/// Service for handling messages between users within Wiser.
/// </summary>
public class PusherService : IPusherService, IScopedService
{
    private readonly ApiSettings apiSettings;

    /// <summary>
    /// Creates a new instance of <see cref="PusherService"/>.
    /// </summary>
    /// <param name="apiSettings"></param>
    public PusherService(IOptions<ApiSettings> apiSettings)
    {
        this.apiSettings = apiSettings.Value;
    }

    /// <inheritdoc />
    public ServiceResult<string> GeneratePusherIdForUser(ulong userId, string subDomain)
    {
        if (userId == 0)
        {
            return new ServiceResult<string>
            {
                StatusCode = HttpStatusCode.BadRequest,
                ErrorMessage = "UserId must be greater than 0"
            };
        }

        var eventId = $"{subDomain}_{userId}";
        var hash = eventId.ToSha512ForPasswords(Encoding.UTF8.GetBytes(apiSettings.PusherSalt));
        return new ServiceResult<string>(hash);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> SendMessageToUserAsync(string subDomain, PusherMessageRequestModel data)
    {
        if (data == null || (data.UserId == 0 && !data.IsGlobalMessage))
        {
            return new ServiceResult<bool>(false)
            {
                StatusCode = HttpStatusCode.BadRequest,
                ErrorMessage = "UserId must be greater than 0"
            };
        }

        if (String.IsNullOrWhiteSpace(data.Channel))
        {
            data.Channel = "Wiser";
        }

        if (data.EventData == null)
        {
            data.EventData = new { message = "new" };
        }

        if (String.IsNullOrWhiteSpace(data.Cluster))
        {
            data.Cluster = "eu";
        }

        var pusherId = GeneratePusherIdForUser(data.UserId, subDomain).ModelObject;
        var options = new PusherOptions
        {
            Cluster = data.Cluster,
            Encrypted = true
        };

        // Global messages do not fire events for a specific user.
        var eventName = !String.IsNullOrWhiteSpace(data.EventName) ? data.EventName : data.IsGlobalMessage ? data.Channel : $"{data.Channel}_{pusherId}";

        var pusher = new PusherServer.Pusher(apiSettings.PusherAppId, apiSettings.PusherAppKey, apiSettings.PusherAppSecret, options);
        var result = await pusher.TriggerAsync(data.Channel, eventName, data.EventData);
        var success = (int)result.StatusCode >= 200 && (int)result.StatusCode < 300;
        return new ServiceResult<bool>(success)
        {
            StatusCode = result.StatusCode,
            ErrorMessage = success ? null : result.Body
        };
    }
}