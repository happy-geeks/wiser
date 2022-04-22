using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Pusher.Models;

namespace Api.Modules.Pusher.Interfaces
{
    //TODO Verify comments
    /// <summary>
    /// Service for handling messages between users within Wiser.
    /// </summary>
    public interface IPusherService
    {
        /// <summary>
        /// Get a generated pushed ID for the user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="subDomain">The sub domain of Wiser.</param>
        /// <returns>The event ID for pusher.</returns>
        ServiceResult<string> GeneratePusherIdForUser(ulong userId, string subDomain);

        /// <summary>
        /// Send a message to an user.
        /// </summary>
        /// <param name="subDomain">The sub domain of Wiser.</param>
        /// <param name="data">The <see cref="PusherMessageRequestModel"/> containing the information for the message to be send.</param>
        /// <returns></returns>
        Task<ServiceResult<bool>> SendMessageToUserAsync(string subDomain, PusherMessageRequestModel data);
    }
}
