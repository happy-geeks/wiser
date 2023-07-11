using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Api.Core.Controllers;

[Route("api/v3/google-authentication")]
[ApiController]
public class GoogleAuthenticationController
{
    [HttpGet("token")]
    public ActionResult Auth()
    {
        var properties = new AuthenticationProperties()
        {
            // actual redirect endpoint for your app
            RedirectUri = "/signin-google",
            Items =
            {
                { "LoginProvider", "Google" },
            },
            AllowRefresh = true,
        };

        return new ChallengeResult("Google", properties);
    }
}