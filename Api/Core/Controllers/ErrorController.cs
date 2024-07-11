using System.Threading.Tasks;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Core.Controllers;

public class ErrorController : Controller
{
    private readonly IIdentityServerInteractionService interactionService;

    public ErrorController(IIdentityServerInteractionService interactionService)
    {
        this.interactionService = interactionService;
    }

    [HttpGet("/home/error")]
    public async Task<IActionResult> Error([FromQuery] string errorId)
    {
        var error = await interactionService.GetErrorContextAsync(errorId);

        return new JsonResult(error);
    }
}