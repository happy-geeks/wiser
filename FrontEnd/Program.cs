using Api.Core.Interfaces;
using Api.Core.Services;
using FrontEnd.Core.Interfaces;
using FrontEnd.Core.Models;
using FrontEnd.Core.Services;
using FrontEnd.Modules.ImportExport.Interfaces;
using FrontEnd.Modules.ImportExport.Services;
using FrontEnd.Modules.Templates.Interfaces;
using FrontEnd.Modules.Templates.Services;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Exports.Interfaces;
using GeeksCoreLibrary.Modules.Exports.Services;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

var builder = WebApplication.CreateBuilder(args);

// First set the base settings for the application.
builder.Configuration
    .AddJsonFile("appsettings.json", false, true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true);

// Get the base directory for secrets and then load the secrets file from that directory.
var secretsBasePath = builder.Configuration.GetSection("GCL").GetValue<string>("SecretsBaseDirectory");
builder.Configuration
    .AddJsonFile($"{secretsBasePath}appsettings-secrets.json", true, false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true);

// Configure Serilog.
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddHealthChecks();

// Use Serilog as our main logger.
builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog());

// MVC looks in the directory "Areas" by default, but we use the directory "Modules", so we have to tell MVC that.
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.AreaViewLocationFormats.Add("/Modules/{2}/Views/{1}/{0}" + RazorViewEngine.ViewExtension);
    options.AreaViewLocationFormats.Add("/Modules/{2}/Views/Shared/{0}" + RazorViewEngine.ViewExtension);
    options.AreaViewLocationFormats.Add("/Core/Views/{1}/{0}" + RazorViewEngine.ViewExtension);
    options.AreaViewLocationFormats.Add("/Core/Views/Shared/{0}" + RazorViewEngine.ViewExtension);

    options.ViewLocationFormats.Add("/Core/Views/{1}/{0}" + RazorViewEngine.ViewExtension);
    options.ViewLocationFormats.Add("/Core/Views/Shared/{0}" + RazorViewEngine.ViewExtension);
});

// Use the options pattern for all settings in appSettings.json.
builder.Services.Configure<GclSettings>(builder.Configuration.GetSection("GCL"));
builder.Services.Configure<FrontEndSettings>(builder.Configuration.GetSection("FrontEnd"));

// Set Newtonsoft as the default JSON serializer and configure it to use camel case.
builder.Services.AddControllersWithViews(options => { options.AllowEmptyInputInBodyModelBinding = true; }).AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ContractResolver = new DefaultContractResolver
    {
        NamingStrategy = new CamelCaseNamingStrategy(false, true, false)
    };

    options.SerializerSettings.Formatting = Formatting.Indented;
    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
    options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Local;
    options.SerializerSettings.Converters.Add(new StringEnumConverter());
});

// Setup dependency injection.
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<IPluginsService, PluginsService>();
builder.Services.AddTransient<IBaseService, BaseService>();
builder.Services.AddTransient<IImportsService, ImportsService>();
builder.Services.AddTransient<IFrontEndDynamicContentService, FrontEndDynamicContentService>();
builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddSingleton<IWebPackService, WebPackService>();
builder.Services.AddSingleton<IExternalApisService, ExternalApisService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    // Force https on non-dev environments.
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHealthChecks("/health", new HealthCheckOptions {Predicate = _ => true});

var webPackService = app.Services.GetRequiredService<IWebPackService>();
await webPackService.InitializeAsync();

// Load plugins for GCL and Wiser.
var pluginService = app.Services.GetRequiredService<IPluginsService>();
pluginService.LoadPlugins(app.Configuration.GetValue<string>("Api:PluginsDirectory"));

await app.RunAsync();