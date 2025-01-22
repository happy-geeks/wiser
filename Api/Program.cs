using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Api.Core.Filters;
using Api.Core.Interfaces;
using Api.Core.Middlewares;
using Api.Core.Models;
using Api.Core.Services;
using Api.Modules.DigitalOcean.Models;
using Api.Modules.Files.Models;
using Api.Modules.Google.Models;
using Api.Modules.Languages.Interfaces;
using Api.Modules.Languages.Services;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Services;
using Api.Modules.Tenants.Interfaces;
using Api.Modules.Tenants.Services;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Services;
using JavaScriptEngineSwitcher.ChakraCore;
using JavaScriptEngineSwitcher.Extensions.MsDependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Validation.AspNetCore;
using React;
using React.AspNet;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

const string corsPolicyName = "AllowAllOrigins";

// Configure services.
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


var apiBaseUrl = builder.Configuration.GetValue<string>("Api:BaseUrl");
var clientSecret = builder.Configuration.GetValue<string>("Api:ClientSecret");

// Use the options pattern for all GCL settings in appSettings.json.
builder.Services.AddOptions();
builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("Api"));
builder.Services.Configure<StyledOutputSettings>(builder.Configuration.GetSection("StyledOutput"));
builder.Services.Configure<DigitalOceanSettings>(builder.Configuration.GetSection("DigitalOcean"));
builder.Services.Configure<GoogleSettings>(builder.Configuration.GetSection("Google"));
builder.Services.Configure<TinyPngSettings>(builder.Configuration.GetSection("TinyPNG"));

// Use Serilog as our main logger.
builder.Services.AddLogging(loggingBuilder => { loggingBuilder.AddSerilog(); });

// Configure Swagger for documentation.
builder.Services.AddSwaggerGenNewtonsoftSupport();
builder.Services.AddSwaggerGen(config =>
{
    config.SwaggerDoc("v3",
        new OpenApiInfo
        {
            Title = "Wiser.WebAPI",
            Version = "3",
            Description = "Web API for Wiser"
        });

    // Make sure the API's own assembly is used to avoid the GCL's controllers and schemas from showing up in Swagger.
    config.DocInclusionPredicate((_, apiDesc) =>
    {
        // Filter out 3rd party controllers
        var assemblyName = ((ControllerActionDescriptor) apiDesc.ActionDescriptor).ControllerTypeInfo.Assembly.GetName().Name;
        var currentAssemblyName = typeof(Program).Assembly.GetName().Name;
        return currentAssemblyName == assemblyName;
    });

    config.EnableAnnotations();
    config.OperationFilter<AuthorizeOperationFilter>();
    config.AddSecurityDefinition("oauth2",
        new OpenApiSecurityScheme
        {
            Scheme = "Bearer",
            In = ParameterLocation.Header,
            Name = "Authorization",
            Type = SecuritySchemeType.OAuth2,
            OpenIdConnectUrl = new Uri($"{apiBaseUrl}/.well-known/openid-configuration"),
            Flows = new OpenApiOAuthFlows
            {
                Password = new OpenApiOAuthFlow
                {
                    TokenUrl = new Uri($"{apiBaseUrl}/connect/token"),
                    Extensions = new Dictionary<string, IOpenApiExtension>
                    {
                        { "subDomain", new OpenApiString("main") }
                    }
                }
            }
        });

    // Make sure the comments added to the API are used by Swagger.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        config.IncludeXmlComments(xmlPath);
    }
});

// Services from GCL. Some services are registered because they are required by other GCL services, not because this API uses them.
builder.Services.AddGclServices(builder.Configuration, false, true);
builder.Services.Decorate<IDatabaseHelpersService, CachedDatabaseHelpersService>();

// Set default settings for JSON.NET.
JsonConvert.DefaultSettings = () => new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

// Enable CORS, to allow the API to be called by javascript via other domains.
builder.Services.AddCors((options) =>
{
    options.AddPolicy(corsPolicyName,
        policyBuilder =>
        {
            policyBuilder.AllowAnyOrigin();
            policyBuilder.AllowAnyHeader();
            policyBuilder.AllowAnyMethod();
        });
});

// Set Newtonsoft as the default JSON serializer and configure it to use camel case.
builder.Services.AddControllers(options => { options.AllowEmptyInputInBodyModelBinding = true; }).AddNewtonsoftJson(options =>
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

// Make sure all API URLs are lower case.
builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

// Configure OAuth2
// Configure OpenIddict
builder.Services.AddOpenIddict()
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/connect/token");

        // Degraded mode is needed because we handle authentication, user rights etc. ourselves
        // Without Degraded mode openiddict would try find users, scopes and such using Entity Framework
        options.EnableDegradedMode();

        options.UseAspNetCore();
        options.AllowPasswordFlow();
        options.AllowRefreshTokenFlow();

        if (builder.Environment.IsDevelopment())
        {
            options.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();

            options.UseAspNetCore().DisableTransportSecurityRequirement();
        }
        else
        {
            // Add signing certificate used to sign the JWT token
            // This is needed so we can validate the JWT token was really issues by us.
            var signingCertificateName = builder.Configuration.GetValue<string>("Api:SigningCredentialCertificate");
            var signingCertificate = GetCertificateByName(signingCertificateName);
            options.AddSigningCertificate(signingCertificate);

            // Add certificate to encrypt the JWT token
            // A JWT token shouldn't contain sensitive information so this is a bit of extra security
            var encryptionCertificateName = builder.Configuration.GetValue<string>("Api:EncryptionCredentialCertificate");
            if (!String.IsNullOrWhiteSpace(encryptionCertificateName))
            {
                options.AddEncryptionCertificate(GetCertificateByName(encryptionCertificateName));
            }
            else
            {
                options.AddEncryptionCertificate(signingCertificate);
                options.DisableAccessTokenEncryption();
            }
        }

        // Define static scopes here.
        options.RegisterScopes(
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.OfflineAccess,
            "api.read",    // Read access to the API
            "api.write",    // Write access to the API
            "api.users_list"
        );

        // Register handler for token requests
        // This contains all the logic of authenticating users
        // And passing back all the data the frontend needs.
        options.AddEventHandler<OpenIddictServerEvents.HandleTokenRequestContext>(builder =>
        {
            builder.UseScopedHandler<OpenIddictTokenRequestHandler>();
        });

        options.AddEventHandler<OpenIddictServerEvents.ValidateTokenRequestContext>(builder =>
        {
            builder.UseInlineHandler(context =>
            {
                if (context.Request.ClientId != "wiser" || context.Request.ClientSecret != clientSecret)
                {
                    context.Reject(
                        error: OpenIddictConstants.Errors.InvalidClient,
                        description: "The client credentials is invalid.");
                }

                return ValueTask.CompletedTask;
            });
        });

    })
    .AddValidation(options =>
    {
        // Import the configuration from the local OpenIddict server instance.
        options.UseLocalServer();

        // Register the ASP.NET Core host.
        options.UseAspNetCore();
        options.SetClientId("wiser");
        options.SetClientSecret(clientSecret);
    });

// Sets authentication to be done using OpenIddict by default
builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

// Define policies that can be used on the Wiser endpoints
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser() // Requires the user to be authenticated
        .RequireAssertion(context =>
        {
            var scopes = context.User.GetClaims("scope").FirstOrDefault();
            return scopes is not null && scopes.Split(" ").Contains("api.write");
        })
        .Build();

    options.AddPolicy("ApiUsersList",
        policy =>
        {
            policy.RequireAuthenticatedUser()
                .RequireAssertion(context =>
                {
                    var scopes = context.User.GetClaims("scope").FirstOrDefault();
                    return scopes is not null && scopes.Split(" ").Contains("api.users_list");
                });
        });

    options.AddPolicy("ApiWrite",
        policy =>
        {
            policy.RequireAuthenticatedUser()
                .RequireAssertion(context =>
                {
                    var scopes = context.User.GetClaims("scope").FirstOrDefault();
                    return scopes is not null && scopes.Split(" ").Contains("api.write");
                });
        });

    options.AddPolicy("ApiRead",
        policy =>
        {
            policy.RequireAuthenticatedUser()
                .RequireAssertion(context =>
                {
                    var scopes = context.User.GetClaims("scope").FirstOrDefault();
                    return scopes is not null && scopes.Split(" ").Contains("api.read");
                });
        });
});

// Configure dependency injection.
builder.Services.AddScoped<MySqlDatabaseConnection>();
builder.Services.AddScoped<IDatabaseConnection>(provider => provider.GetRequiredService<MySqlDatabaseConnection>());

builder.Services.Decorate<IDatabaseConnection, ClientDatabaseConnection>();
builder.Services.Decorate<ITemplatesService, CachedTemplatesService>();
builder.Services.Decorate<IUsersService, CachedUsersService>();
builder.Services.Decorate<ILanguagesService, CachedLanguagesService>();
builder.Services.Decorate<IWiserTenantsService, CachedWiserTenantsService>();

// Add JavaScriptEngineSwitcher services to the services container.
builder.Services.AddJsEngineSwitcher(options => options.DefaultEngineName = ChakraCoreJsEngine.EngineName).AddChakraCore();

builder.Services.AddReact();

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Configure the application.
var app = builder.Build();
if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.ConfigureExceptionHandler(app.Services.GetRequiredService<ILogger<Program>>());
}

app.UseMiddleware<ApiRequestLoggingMiddleware>();

// Setup React and babel for the /api/v3/babel endpoint (for converting ES6 javascript to ES5).
app.UseReact(config =>
{
    // We have to set Babel to version 6, otherwise it won't convert ES6 to ES5 anymore for some reason.
    config.SetBabelVersion(BabelVersions.Babel6);
    config.ReuseJavaScriptEngines = true;
});

// Configure Swagger for documentation.
app.UseSwagger(options =>
{
    options.PreSerializeFilters.Add((swagger, httpReq) =>
    {
        var scheme = httpReq.Scheme;
        var host = httpReq.Host.Host;
        var port = "";
        if (host.ToLowerInvariant() == "localhost" && httpReq.Host.Port.HasValue)
        {
            port = $":{httpReq.Host.Port.Value}";
        }

        // Set own server as the server it will run requests on.
        swagger.Servers = new List<OpenApiServer>
        {
            new() { Url = $"{scheme}://{host}{port}" }
        };
    });
});

app.UseSwaggerUI(config => { config.SwaggerEndpoint("/swagger/v3/swagger.json", "Wiser.WebApi V3"); });

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseCors(corsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true
});

// Load plugins for GCL and Wiser.
var pluginService = app.Services.GetRequiredService<IPluginsService>();
pluginService.LoadPlugins(builder.Configuration.GetValue<string>("Api:PluginsDirectory"));

var applicationLifetime = app.Services.GetService<IHostApplicationLifetime>();

// ReSharper disable once AsyncVoidMethod
applicationLifetime.ApplicationStarted.Register(async void () =>
{
    // Make sure all important tables exist and are up-to-date, while starting the application.
    IServiceScope scope = null;
    try
    {
        scope = app.Services.CreateScope();
        var databaseHelpersService = scope.ServiceProvider.GetService<IWiserDatabaseHelpersService>();
        await databaseHelpersService.DoDatabaseMigrationsForMainDatabaseAsync();
    }
    catch (Exception exception)
    {
        scope?.ServiceProvider.GetService<ILogger>().LogError(exception, "Error while updating tables.");
    }
    finally
    {
        scope?.Dispose();
    }
});

await app.RunAsync();
return;

static X509Certificate2 GetCertificateByName(string certificateName)
{
    using var webHostingStore = new X509Store("WebHosting", StoreLocation.LocalMachine);
    webHostingStore.Open(OpenFlags.ReadOnly);
    var certificateCollection = webHostingStore.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, certificateName, validOnly: false);
    if (certificateCollection.Count == 0)
    {
        using var personalStore = new X509Store(StoreName.My, StoreLocation.LocalMachine);
        personalStore.Open(OpenFlags.ReadOnly);
        certificateCollection = personalStore.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, certificateName, validOnly: false);

        if (certificateCollection.Count == 0)
        {
            throw new Exception($"Certificate with name \"{certificateName}\" not found in WebHosting or Personal store.");
        }
    }

    return certificateCollection.First();
}