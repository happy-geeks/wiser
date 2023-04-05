using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Modules.Communication.Interfaces;
using GeeksCoreLibrary.Modules.Communication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Communication.Controllers;

/// <summary>
/// A controller for loading and saving communication settings in the communication module.
/// </summary>
[Route("api/v3/[controller]")]
[ApiController]
[Authorize]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class CommunicationsController : Controller
{
    private readonly ICommunicationsService communicationsService;

    /// <summary>
    /// Creates a new instance of <see cref="CommunicationsController"/>.
    /// </summary>
    public CommunicationsController(ICommunicationsService communicationsService)
    {
        this.communicationsService = communicationsService;
    }

    /// <summary>
    /// Get the settings of all communications of a specific type (such as SMS or e-mail).
    /// </summary>
    /// <param name="namesOnly">Optional: Whether to only get the names (and IDs) or everything.</param>
    /// <returns>A list of <see cref="CommunicationSettingsModel"/>.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(CommunicationSettingsModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CommunicationSettingsModel), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAllSettingsAsync(bool namesOnly = true)
    {
        return (await communicationsService.GetSettingsAsync(null, namesOnly)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Get the settings of a specific row from wiser_communication.
    /// </summary>
    /// <param name="id">The ID of the communication settings to get.</param>
    /// <param name="nameOnly">Optional: Whether to only get the name (and ID) or everything.</param>
    /// <returns>A <see cref="CommunicationSettingsModel"/> with the settings, or <see langword="null"/> if it doesn't exist.</returns>
    [HttpGet]
    [Route("{id:int}")]
    [ProducesResponseType(typeof(CommunicationSettingsModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CommunicationSettingsModel), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSettingsAsync(int id, bool nameOnly = false)
    {
        return (await communicationsService.GetSettingsAsync(id, nameOnly)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Create new settings or updates existing settings (based on <see cref="CommunicationSettingsModel.Id"/>).
    /// </summary>
    /// <param name="settings">The <see cref="CommunicationSettingsModel"/> to create or update.</param>
    [HttpPost]
    [ProducesResponseType(typeof(CommunicationSettingsModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CommunicationSettingsModel), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveSettingsAsync(CommunicationSettingsModel settings)
    {
        return (await communicationsService.SaveSettingsAsync((ClaimsIdentity)User.Identity, settings)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Deletes a row of communication settings.
    /// </summary>
    /// <param name="id">The ID of the settings to delete.</param>
    [HttpDelete]
    [Route("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSettingsAsync(int id)
    {
        return (await communicationsService.DeleteSettingsAsync((ClaimsIdentity)User.Identity, id)).GetHttpResponseMessage();
    }

    /// <summary>
    /// Send an e-mail to someone.
    /// </summary>
    /// <param name="communication">The <see cref="SingleCommunicationModel"/> with information for sending the e-mail.</param>
    [HttpPost]
    [Route("email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SendEmailAsync(SingleCommunicationModel communication)
    {
        return (await communicationsService.SendEmailAsync((ClaimsIdentity)User.Identity, communication)).GetHttpResponseMessage();
    }
}