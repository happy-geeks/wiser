using System.Globalization;
using Api.Modules.Translations.Services;
using System.Threading.Tasks;
using FrontEnd.Core.Interfaces;
using FrontEnd.Core.Models;
using FrontEnd.Core.Services;
using FrontEnd.Modules.ImportExport.Interfaces;
using FrontEnd.Modules.ImportExport.Services;
using FrontEnd.Modules.Templates.Interfaces;
using FrontEnd.Modules.Templates.Services;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Serilog;

[assembly: AspMvcAreaViewLocationFormat("/Modules/{2}/Views/{1}/{0}.cshtml")]
[assembly: AspMvcAreaViewLocationFormat("/Modules/{2}/Views/Shared/{0}.cshtml")]
[assembly: AspMvcAreaViewLocationFormat("/Core/Views/{1}/{0}.cshtml")]
[assembly: AspMvcAreaViewLocationFormat("/Core/Views/Shared/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("/Core/Views/{1}/{0}.cshtml")]
[assembly: AspMvcViewLocationFormat("/Core/Views/Shared/{0}.cshtml")]

namespace FrontEnd
{
    public class Startup
    {
        public Startup(IWebHostEnvironment webHostEnvironment)
        {
            // First set the base settings for the application.
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{webHostEnvironment.EnvironmentName}.json", true, true);

            // We need to build here already, so that we can read the base directory for secrets.
            Configuration = builder.Build();

            // Get the base directory for secrets and then load the secrets file from that directory.
            var secretsBasePath = Configuration.GetSection("GCL").GetValue<string>("SecretsBaseDirectory");
            builder
                .AddJsonFile($"{secretsBasePath}appsettings-secrets.json", true, false)
                .AddJsonFile($"appsettings.{webHostEnvironment.EnvironmentName}.json", true, true);

            // Build the final configuration with all combined settings.
            Configuration = builder.Build();

            // Configure Serilog.
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHealthChecks();

            // Use Serilog as our main logger.
            services.AddLogging(builder => { builder.AddSerilog(); });
            
            // MVC looks in the directory "Areas" by default, but we use the directory "Modules", so we have to tell MC that.
            services.Configure<RazorViewEngineOptions>(options =>
            {
                options.AreaViewLocationFormats.Add("/Modules/{2}/Views/{1}/{0}" + RazorViewEngine.ViewExtension);
                options.AreaViewLocationFormats.Add("/Modules/{2}/Views/Shared/{0}" + RazorViewEngine.ViewExtension);
                options.AreaViewLocationFormats.Add("/Core/Views/{1}/{0}" + RazorViewEngine.ViewExtension);
                options.AreaViewLocationFormats.Add("/Core/Views/Shared/{0}" + RazorViewEngine.ViewExtension);

                options.ViewLocationFormats.Add("/Core/Views/{1}/{0}" + RazorViewEngine.ViewExtension);
                options.ViewLocationFormats.Add("/Core/Views/Shared/{0}" + RazorViewEngine.ViewExtension);
            });

            // Use the options pattern for all settings in appSettings.json.
            services.Configure<FrontEndSettings>(Configuration.GetSection("FrontEnd"));

            // Set Newtonsoft as the default JSON serializer and configure it to use camel case.
            services.AddControllersWithViews(options => { options.AllowEmptyInputInBodyModelBinding = true; })
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver()
                    {
                        NamingStrategy = new CamelCaseNamingStrategy(false, true, false)
                    };

                    options.SerializerSettings.Formatting = Formatting.Indented;
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Local;
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                });
            
            // Set localization to use resource files with suffices instead of directories.
            // E.G. "Modules/Translations/Resources/Modules/Translations/Views/Index.nl.resx"
            // Instead of "Modules/Translations/Resources/Modules.Translations.Views.Index.nl.resx"
            services.AddControllersWithViews().AddViewLocalization();
            services.AddControllersWithViews().AddDataAnnotationsLocalization();
            
            services.Configure<RequestLocalizationOptions>(options =>
            {
                string[] supportedCultures = { "en-US", "nl-NL" };
                options.AddSupportedCultures(supportedCultures);
                options.AddSupportedUICultures(supportedCultures);
                options.SetDefaultCulture(supportedCultures[0]);
                options.FallBackToParentUICultures = true;
            });
            
            // Setup localization.
            services.AddLocalization(options => options.ResourcesPath = "Modules/Translations/Resources");

            // Setup dependency injection.
            services.AddHttpContextAccessor();
            services.AddTransient<IBaseService, BaseService>();
            services.AddTransient<IImportsService, ImportsService>();
            services.AddTransient<IFrontEndDynamicContentService, FrontEndDynamicContentService>();
            services.AddSingleton<IWebPackService, WebPackService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IWebPackService webPackService)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // Force https on non-dev environments.
                app.UseHttpsRedirection();
            }
            
            // Use the localization options set in ConfigureService()
            app.UseRequestLocalization(app.ApplicationServices
                .GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);
            
            app.UseStaticFiles();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapHealthChecks("/health", new HealthCheckOptions
                {
                    Predicate = _ => true
                });
            });

            webPackService.InitializeAsync();
        }
    }
}
