using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using FrontEnd.Core.Interfaces;
using FrontEnd.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace FrontEnd.Core.Services;

public class HomeService : IHomeService
{
    private readonly FrontEndSettings frontEndSettings;
    private readonly HttpContext httpContext;
    
    public HomeService(IOptions<FrontEndSettings> frontEndSettings, IHttpContextAccessor httpContextAccessor)
    {
        this.frontEndSettings = frontEndSettings.Value;
        httpContext = httpContextAccessor.HttpContext;
    }
    
    /// <inheritdoc />
    public async Task Login()
    {
        var test = httpContext.Request.Cookies["idsrv.external"];
        var returnUrl = httpContext.Request.Host.Value;

        var client = new HttpClient();
        var response = await client.GetAsync($"{frontEndSettings.ApiBaseUrl}api/v3/users/external-login-callback?returnUrl=");
    }
}