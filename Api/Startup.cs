using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
using Microsoft.AspNetCore.Hosting;
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

namespace Api
{
#pragma warning disable CS1591
    public class Startup
    {
        private const string CorsPolicyName = "AllowAllOrigins";

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

            this.webHostEnvironment = webHostEnvironment;

            // Configure Serilog.
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();
        }

        public IConfiguration Configuration { get; }
        private readonly IWebHostEnvironment webHostEnvironment;

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            var apiBaseUrl = Configuration.GetValue<string>("Api:BaseUrl");
            var clientSecret = Configuration.GetValue<string>("Api:ClientSecret");

            // Use the options pattern for all GCL settings in appSettings.json.
            services.AddOptions();
            services.Configure<ApiSettings>(Configuration.GetSection("Api"));
            services.Configure<StyledOutputSettings>(Configuration.GetSection("StyledOutput"));
            services.Configure<DigitalOceanSettings>(Configuration.GetSection("DigitalOcean"));
            services.Configure<GoogleSettings>(Configuration.GetSection("Google"));
            services.Configure<TinyPngSettings>(Configuration.GetSection("TinyPNG"));

            // Use Serilog as our main logger.
            services.AddLogging(builder => { builder.AddSerilog(); });

            // Configure Swagger for documentation.
            services.AddSwaggerGenNewtonsoftSupport();
            services.AddSwaggerGen(config =>
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
                    var assemblyName = ((ControllerActionDescriptor) apiDesc.ActionDescriptor).ControllerTypeInfo
                        .Assembly.GetName().Name;
                    var currentAssemblyName = GetType().Assembly.GetName().Name;
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
            services.AddGclServices(Configuration, false, true);
            services.Decorate<IDatabaseHelpersService, CachedDatabaseHelpersService>();

            // Set default settings for JSON.NET.
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

            // Enable CORS, to allow the API to be called by javascript via other domains.
            services.AddCors((options) =>
            {
                options.AddPolicy(CorsPolicyName,
                    builder =>
                    {
                        builder.AllowAnyOrigin();
                        builder.AllowAnyHeader();
                        builder.AllowAnyMethod();
                    });
            });

            // Set Newtonsoft as the default JSON serializer and configure it to use camel case.
            services.AddControllers(options => { options.AllowEmptyInputInBodyModelBinding = true; }).AddNewtonsoftJson(options =>
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

            // Make sure all API URLs are lower case.
            services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

            // Configure OAuth2
            // Configure OpenIddict
            services.AddOpenIddict()
                .AddServer(options =>
                {
                    options.SetTokenEndpointUris("/connect/token");

                    // Degraded mode is needed because we handle authentication, user rights etc. ourselves
                    // Without Degraded mode openiddict would try find users, scopes and such using Entity Framework
                    options.EnableDegradedMode();

                    options.UseAspNetCore();
                    options.AllowPasswordFlow();
                    options.AllowRefreshTokenFlow();

                    if (webHostEnvironment.IsDevelopment())
                    {
                        options.AddDevelopmentEncryptionCertificate()
                            .AddDevelopmentSigningCertificate();

                        options.UseAspNetCore()
                            .DisableTransportSecurityRequirement();
                    }
                    else
                    {
                        // Add signing certificate used to sign the JWT token
                        // This is needed so we can validate the JWT token was really issues by us.
                        var signingCertificateName = Configuration.GetValue<string>("Api:SigningCredentialCertificate");
                        var signingCertificate = GetCertificateByName(signingCertificateName);
                        options.AddSigningCertificate(signingCertificate);

                        // Add certificate to encrypt the JWT token
                        // A JWT token shouldn't contain sensitive information so this is a bit of extra security
                        var encryptionCertificateName = Configuration.GetValue<string>("Api:EncryptionCredentialCertificate");
                        if (!String.IsNullOrWhiteSpace(encryptionCertificateName))
                        {
                            options.AddEncryptionCertificate(GetCertificateByName(signingCertificateName));
                        }
                        else
                        {
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
            services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

            // Define policies that can be used on the Wiser endpoints
            services.AddAuthorization(options =>
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
            services.AddScoped<MySqlDatabaseConnection>();
            services.AddScoped<IDatabaseConnection>(provider => provider.GetRequiredService<MySqlDatabaseConnection>());

            services.Decorate<IDatabaseConnection, ClientDatabaseConnection>();
            services.Decorate<ITemplatesService, CachedTemplatesService>();
            services.Decorate<IUsersService, CachedUsersService>();
            services.Decorate<ILanguagesService, CachedLanguagesService>();
            services.Decorate<IWiserTenantsService, CachedWiserTenantsService>();

            // Add JavaScriptEngineSwitcher services to the services container.
            services.AddJsEngineSwitcher(options => options.DefaultEngineName = ChakraCoreJsEngine.EngineName).AddChakraCore();

            services.AddReact();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger, IPluginsService pluginService)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.ConfigureExceptionHandler(logger);
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

            app.UseCors(CorsPolicyName);

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                endpoints.MapHealthChecks("/health", new HealthCheckOptions
                {
                    Predicate = _ => true
                });
            });

            // Load plugins for GCL and Wiser.
            pluginService.LoadPlugins(Configuration.GetValue<string>("Api:PluginsDirectory"));

            HandleStartupFunctions(app);
        }

        private static X509Certificate2 GetCertificateByName(string certificateName)
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

        /// <summary>
        /// Handle and execute some functions that are needed to be done during startup of the application.
        /// Don't call this method if you're already calling UseGclMiddleware, because this is called inside that.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public IApplicationBuilder HandleStartupFunctions(IApplicationBuilder builder)
        {
            var applicationLifetime = builder.ApplicationServices.GetService<IHostApplicationLifetime>();

            applicationLifetime.ApplicationStarted.Register(async () =>
            {
                using var scope = builder.ApplicationServices.CreateScope();

                // Make sure all important tables exist and are up-to-date, while starting the application.
                try
                {
                    var databaseHelpersService = scope.ServiceProvider.GetService<IWiserDatabaseHelpersService>();
                    await databaseHelpersService.DoDatabaseMigrationsForMainDatabaseAsync();
                }
                catch (Exception exception)
                {
                    scope.ServiceProvider.GetService<ILogger>().LogError(exception, "Error while updating tables.");
                }
            });

            return builder;
        }
    }
#pragma warning restore CS1591
}