using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Api.Core.Helpers;
using FrontEnd.Core.Interfaces;
using FrontEnd.Core.Models;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;

namespace FrontEnd.Core.Services
{
    public class BaseService : IBaseService
    {
        public const int Wiser1DebuggingPortNumber = 54405;

        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly GclSettings gclSettings;
        private readonly FrontEndSettings frontEndSettings;

        public BaseService(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment webHostEnvironment, IOptions<FrontEndSettings> frontEndSettings, IOptions<GclSettings> gclSettings)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.webHostEnvironment = webHostEnvironment;
            this.gclSettings = gclSettings.Value;
            this.frontEndSettings = frontEndSettings.Value;
        }

        /// <inheritdoc />
        public string GetSubDomain()
        {
            if (httpContextAccessor.HttpContext == null)
            {
                return "";
            }

            var requestUrl = new Uri(httpContextAccessor.HttpContext.Request.GetDisplayUrl());
            return HttpContextHelpers.GetSubdomainFromUrl(requestUrl, frontEndSettings.WiserHostNames, frontEndSettings.MainSubDomain);
        }

        /// <inheritdoc />
        public string GetWiser1Url()
        {
            if (httpContextAccessor.HttpContext == null)
            {
                return "";
            }

            var requestUrl = new Uri(httpContextAccessor.HttpContext.Request.GetDisplayUrl());
            var subDomain = GetSubDomain();
            if (requestUrl.Port != 443 && requestUrl.Port == 80)
            {
                return $"http://{subDomain}:{Wiser1DebuggingPortNumber}/";
            }

            if (requestUrl.Host.Contains("juicedev.nl", StringComparison.OrdinalIgnoreCase))
            {
                return $"http://{subDomain}.wiser.nl.juicedev.nl/";
            }

            return $"https://{subDomain}.wiser.nl/";
        }

        /// <inheritdoc />
        public T CreateBaseViewModel<T>() where T : BaseViewModel
        {
            var viewModel = (T)Activator.CreateInstance(typeof(T));
            viewModel!.Settings = frontEndSettings;
            viewModel.WiserVersion = Assembly.GetEntryAssembly()?.GetName().Version;
            viewModel.SubDomain = GetSubDomain();
            viewModel.IsTestEnvironment = webHostEnvironment.EnvironmentName is "test" or "development";
            viewModel.Wiser1BaseUrl = GetWiser1Url();
            viewModel.ApiAuthenticationUrl = $"{frontEndSettings.ApiBaseUrl}connect/token";
            viewModel.ApiRoot = $"{frontEndSettings.ApiBaseUrl}api/v3/";
            viewModel.IsWiserFrontEndLogin = "true".EncryptWithAesWithSalt(gclSettings.DefaultEncryptionKey, true, true);

            if (httpContextAccessor.HttpContext != null)
            {
                viewModel.CurrentDomain = httpContextAccessor.HttpContext.Request.Host.Value;
                viewModel.CurrentDomain = viewModel.CurrentDomain.Replace($"{viewModel.SubDomain}.", "");
            }

            var partnerStylesDirectory = new DirectoryInfo(Path.Combine(webHostEnvironment.ContentRootPath, "Core/Css/partner"));
            if (partnerStylesDirectory.Exists)
            {
                viewModel.LoadPartnerStyle = partnerStylesDirectory.GetFiles("*.css").Any(f => Path.GetFileNameWithoutExtension(f.Name).Equals(viewModel.SubDomain, StringComparison.OrdinalIgnoreCase));
            }

            return viewModel;
        }

        /// <inheritdoc />
        public BaseViewModel CreateBaseViewModel()
        {
            return CreateBaseViewModel<BaseViewModel>();
        }
    }
}