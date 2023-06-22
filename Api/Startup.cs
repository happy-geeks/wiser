using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Claims;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Api.Core.Filters;
using Api.Core.Models;
using Api.Core.Policies;
using Api.Core.Services;
using Api.Modules.Customers.Interfaces;
using Api.Modules.Customers.Services;
using Api.Modules.DigitalOcean.Models;
using Api.Modules.Files.Models;
using Api.Modules.Google.Models;
using Api.Modules.Languages.Interfaces;
using Api.Modules.Languages.Services;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Services;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Services;
using IdentityServer4.Services;
using JavaScriptEngineSwitcher.ChakraCore;
using JavaScriptEngineSwitcher.Extensions.MsDependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using React;
using React.AspNet;
using Serilog;

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
            var identityServerBuilder = services.AddIdentityServer(options =>
                {
                    options.Events.RaiseSuccessEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseErrorEvents = true;
                })
                .AddInMemoryIdentityResources(ConfigureIdentityServer.GetIdentityResources())
                .AddInMemoryApiResources(ConfigureIdentityServer.GetApiResources(clientSecret))
                .AddInMemoryApiScopes(ConfigureIdentityServer.GetApiScopes())
                .AddInMemoryClients(ConfigureIdentityServer.GetClients(clientSecret))
                .AddProfileService<WiserProfileService>()
                .AddResourceOwnerValidator<WiserGrantValidator>();

            if (webHostEnvironment.IsDevelopment())
            {
                System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                identityServerBuilder.AddDeveloperSigningCredential();
            }
            else
            {
                identityServerBuilder.AddSigningCredential(Configuration.GetValue<string>("Api:SigningCredentialCertificate"));
            }

            services.AddAuthentication("Bearer")
                .AddJwtBearer("Bearer",
                    options =>
                    {
                        options.Authority = apiBaseUrl;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateAudience = false,
                            ClockSkew = webHostEnvironment.IsDevelopment() ? new TimeSpan(0, 0, 0, 5) : new TimeSpan(0, 0, 5, 0)
                        };
                    });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("ApiScope",
                    policy =>
                    {
                        policy.RequireAuthenticatedUser();
                        policy.RequireClaim("scope", "wiser-api");
                    });
            });

            // Enable CORS for Identityserver 4.
            services.AddSingleton<ICorsPolicyService>((container) =>
            {
                var logger = container.GetRequiredService<ILogger<DefaultCorsPolicyService>>();
                return new DefaultCorsPolicyService(logger)
                {
                    AllowAll = true
                };
            });

            services.AddRateLimiter(options =>
            {
                // Set what statusCode will be returned when the client goes over the ratelimit
                // default is 503
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // You can override what happens when requests get rejected
                options.OnRejected = (context, _) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    return ValueTask.CompletedTask;
                };
                
                // The global rate limited is applied universally across all requests
                // The first type argument is the type that gets passed to the ratelimiter factory
                // the second is the type that will be passed to the options and is the key that will be used partition
                // the rate limiting
                // With CreateChained multiple rate limiters can be chained together
                options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                    PartitionedRateLimiter.Create<HttpContext, string>((httpContext) =>
                    {
                        var role = httpContext.User.FindFirst(ClaimTypes.Role);

                        //In an Partitioned Rate limiter you can conditionally apply different different rate limits
                        //in this case disabling the global policy for admins.
                        //This can also be done using a Policy
                        if (role?.Value == "Admin")
                           return RateLimitPartition.GetNoLimiter(role?.Value);
                        
                        // rate limit per user
                        var name = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                        // The fixed window limiter limits requests within a specific window
                        return RateLimitPartition.GetFixedWindowLimiter(name?.Value ?? String.Empty,
                                                                        (_) => new FixedWindowRateLimiterOptions
                                                                        {
                                                                            Window = TimeSpan.FromSeconds(12),

                                                                            PermitLimit = 4,
                                                                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                                                            QueueLimit = 0
                                                                        });
                    }), 
                    PartitionedRateLimiter.Create<HttpContext, string>((httpContext) =>
                    {
                        // rate limit per user
                        // the requests will be tracked per user
                        var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);

                        // The sliding Window is similar to the fixed window
                        // but the window is divided into segments
                        // This way you can disallow bursts of requests at the edges of the time window
                        return RateLimitPartition.GetSlidingWindowLimiter(
                            claim?.Value ?? String.Empty,
                            (_) => new SlidingWindowRateLimiterOptions()
                            {
                                Window = TimeSpan.FromSeconds(5),
                                
                                SegmentsPerWindow = 3, // How many segments the window is divided in
                                                        // SlidingPeriod = Window / SegmentsPerWindow
                                                        // every time the window slides the requests in the oldest segment are forgotten

                                PermitLimit = 3, // sets permit limit across entire window; not just a single segment
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                QueueLimit = 1
                            });
                    }));

                // Instead of purely time based the token bucket is limited by tokens
                // when the bucket is at empty further requests will be disallowed
                options.AddTokenBucketLimiter("SmallBucket",
                                              bucketOptions =>
                                              {
                                                  bucketOptions.TokenLimit = 200; // Maximum amount of tokens
                                                                                // When tokens get replenished it can't get higher than thus number
                                                  bucketOptions.QueueLimit = 100;
                                                  bucketOptions.TokensPerPeriod = 12;
                                                  bucketOptions.ReplenishmentPeriod = TimeSpan.FromSeconds(1); // How long it takes to replenish the tokens
                                                  bucketOptions.AutoReplenishment = true;
                                              });

                // The concurrency limiter limits the amount of requests can be done concurrently
                options.AddConcurrencyLimiter("1AtATime", 
                         concurrencyOptions =>
                        {
                           concurrencyOptions.PermitLimit = 1;
                           concurrencyOptions.QueueLimit = 0;
                        });
                
                options.AddFixedWindowLimiter("TestingLimitResponse",
                       limiterOptions =>
                       {
                           limiterOptions.Window = TimeSpan.FromMinutes(1);
                           limiterOptions.PermitLimit = 1;
                           limiterOptions.QueueLimit = 0;
                       });

                // With policies the rate limit logic can be put into a separate class
                options.AddPolicy<string, UserAgentPolicy>("userAgentPolicy");
            });

            // Configure dependency injection.
            services.Decorate<IDatabaseConnection, ClientDatabaseConnection>();
            services.Decorate<ITemplatesService, CachedTemplatesService>();
            services.Decorate<IUsersService, CachedUsersService>();
            services.Decorate<ILanguagesService, CachedLanguagesService>();
            services.Decorate<IWiserCustomersService, CachedWiserCustomersService>();

            // Add JavaScriptEngineSwitcher services to the services container.
            services.AddJsEngineSwitcher(options => options.DefaultEngineName = ChakraCoreJsEngine.EngineName).AddChakraCore();

            services.AddReact();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.ConfigureExceptionHandler(logger);
            }

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

            app.UseRouting();

            app.UseIdentityServer();

            app.UseCors(CorsPolicyName);

            app.UseAuthorization();
            
            // Enable rate limiting
            app.UseRateLimiter();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                endpoints.MapHealthChecks("/health", new HealthCheckOptions
                {
                    Predicate = _ => true
                });
            });
        }
    }
#pragma warning restore CS1591
}