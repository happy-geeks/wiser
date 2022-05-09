using System.Net.Mime;
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
    /// <summary>
    /// Controller for doing things with Pusher.
    /// </summary>
    [Route("api/v3/[controller]")]
    [ApiController]
    [Authorize]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    public class PusherController : ControllerBase
    {
        private readonly IPusherService pusherService;

        /// <summary>
        /// Creates a new instance of <see cref="PusherController"/>.
        /// </summary>
        public PusherController(IPusherService pusherService)
        {
            this.pusherService = pusherService;
        }

        /// <summary>
        /// Get a generated pusher event ID for the user.
        /// </summary>
        /// <returns>The event ID for pusher.</returns>
        [HttpGet]
        [Route("event-id")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
        [HttpPost]
        [Route("message")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendMessageToUserAsync(PusherMessageRequestModel data)
        {
            var identity = (ClaimsIdentity)User.Identity;
            var subDomain = IdentityHelpers.GetSubDomain(identity);
            return (await pusherService.SendMessageToUserAsync(subDomain, data)).GetHttpResponseMessage();
        }
    }
}
