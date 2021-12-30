using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Modules.Pusher.Interfaces;
using Api.Modules.Pusher.Models;
using Microsoft.AspNetCore.Authorization;

namespace Api.Modules.Pusher.Controllers
{
    //TODO Verify comments
    [Route("api/v3/[controller]"), ApiController, Authorize]
    public class PusherController : ControllerBase
    {
        private readonly IPusherService pusherService;

        public PusherController(IPusherService pusherService)
        {
            this.pusherService = pusherService;
        }

        /// <summary>
        /// Get a generated pushed ID for the user.
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("event-id"), ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public IActionResult GetPusherEventId()
        {
            var identity = (ClaimsIdentity)User.Identity;
            var userId = IdentityHelpers.GetWiserUserId(identity);
            var subDomain = IdentityHelpers.GetSubDomain(identity);
            return pusherService.GeneratePusherIdForUser(userId, subDomain).GetHttpResponseMessage();
        }

        /// <summary>
        /// Send a message to an user.
        /// </summary>
        /// <param name="data">The <see cref="PusherMessageRequestModel"/> containing the information for the message to be send.</param>
        /// <returns></returns>
        [HttpPost, Route("message"), ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetForExportModuleAsync(PusherMessageRequestModel data)
        {
            var identity = (ClaimsIdentity)User.Identity;
            var subDomain = IdentityHelpers.GetSubDomain(identity);
            return (await pusherService.SendMessageToUserAsync(subDomain, data)).GetHttpResponseMessage();
        }
    }
}
