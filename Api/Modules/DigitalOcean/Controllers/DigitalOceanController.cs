using System;
using System.Linq;
using System.Threading.Tasks;
using Api.Modules.DigitalOcean.Interfaces;
using Api.Modules.DigitalOcean.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Primitives;

namespace Api.Modules.DigitalOcean.Controllers
{
    /// <summary>
    /// A controller for doing things with DigitalOcean.
    /// </summary>
    [Route("api/v3/digital-ocean"), ApiController]
    public class DigitalOceanController : ControllerBase
    {
        private readonly IDigitalOceanService digitalOceanService;

        public DigitalOceanController(IDigitalOceanService digitalOceanService)
        {
            this.digitalOceanService = digitalOceanService;
        }

        [HttpGet("authorize"), EnableCors("AllowAllOrigins")]
        public RedirectResult Authorize()
        {
            return Redirect(digitalOceanService.AuthorizationRedirect());
        }

        [HttpGet("callback"), EnableCors("AllowAllOrigins")]
        public async Task<ActionResult<string>> CallbackAsync([FromQuery] string code)
        {
            return await digitalOceanService.ProcessCallbackAsync(code);
        }

        [HttpGet("databases"), EnableCors("AllowAllOrigins")]
        public async Task<JsonResult> DatabasesAsync()
        {
            var accessToken = AccessTokenFromHeaders();
            return new JsonResult(await digitalOceanService.DatabaseListAsync(accessToken));
        }

        [HttpPost("databases"), EnableCors("AllowAllOrigins")]
        public async Task<JsonResult> CreateDatabaseAsync([FromBody]CreateDatabaseRequestModel body)
        {
            var accessToken = AccessTokenFromHeaders();
            try
            {
                var databaseInfo = await digitalOceanService.CreateDatabaseAsync(body.DatabaseCluster, body.Database, body.User, accessToken);
                await digitalOceanService.RestrictMysqlUserToDbAsync(databaseInfo, accessToken);
                return new JsonResult(databaseInfo);
            } 
            catch (Exception e)
            {
                return new JsonResult(new { Error = e.ToString() });
            }
        }

        private string AccessTokenFromHeaders()
        {
            StringValues stringValues;
            Request.Headers.TryGetValue("x-digital-ocean", out stringValues);
            return stringValues.FirstOrDefault();
        }
    }
}
