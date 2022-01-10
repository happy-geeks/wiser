using System;
using System.Net;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Services;
using Api.Modules.Babel.Interfaces;
using Api.Modules.Babel.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using Microsoft.Extensions.Logging;
using React;

namespace Api.Modules.Babel.Services
{
    /// <inheritdoc cref="IBabelService" />
    public class BabelService : IBabelService, IScopedService
    {
        private readonly ILogger<BabelService> logger;
        private string polyFillBabel;
        private string polyFillCustom;

        /// <summary>
        /// Initializes a new instance of <see cref="BabelService"/>.
        /// </summary>
        public BabelService(ILogger<BabelService> logger)
        {
            this.logger = logger;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<ResponseModel>> ConvertJavascriptAsync(RequestModel options)
        {
            var response = new ResponseModel
            {
                Content = options?.Content
            };

            try
            {
                var babel = ReactEnvironment.Current.Babel;
                var removeStrict = false;
                var addPolyfills = true;

                if (options?.Options != null)
                {
                    // Check if the "removestrict" option is present and try to parse its value.
                    if (options.Options.ContainsKey("removestrict"))
                    {
                        // Value can be either a string or bool.
                        switch (options.Options["removestrict"])
                        {
                            case string removeStrictStringSetting:
                                removeStrict = removeStrictStringSetting is "true" or "1";
                                break;
                            case bool removeStrictBoolSetting:
                                removeStrict = removeStrictBoolSetting;
                                break;
                        }
                    }

                    // Check if the "polyfill" option is present and try to parse its value.
                    if (options.Options.ContainsKey("polyfill"))
                    {
                        // Value can be either a string or bool.
                        switch (options.Options["polyfill"])
                        {
                            case string addPolyfillsStringSetting:
                                addPolyfills = addPolyfillsStringSetting is "true" or "1";
                                break;
                            case bool addPolyfillsBoolSetting:
                                addPolyfills = addPolyfillsBoolSetting;
                                break;
                        }
                    }
                }

                response.Content = babel.Transform(response.Content);

                if (removeStrict && response.Content.StartsWith(@"""use strict"";"))
                {
                    response.Content = response.Content.Substring(13);
                }

                // AS a default, polyfills are added
                if (addPolyfills)
                {
                    var strictTxt = removeStrict ? "" : @"""use strict"";";

                    if (String.IsNullOrWhiteSpace(polyFillBabel))
                    {
                        polyFillBabel = await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Modules.Babel.Scripts.Polyfills.babel.js");
                    }

                    if (String.IsNullOrWhiteSpace(polyFillCustom))
                    {
                        polyFillCustom = await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Modules.Babel.Scripts.Polyfills.custom.js");
                    }

                    // add polyfills
                    var polyFills = polyFillCustom;

                    polyFills += Environment.NewLine + Environment.NewLine + polyFillBabel;
                    response.Content = $@"/* POLYFILLS */ {Environment.NewLine} {strictTxt} {Environment.NewLine}
                                       { polyFills} {Environment.NewLine} 
                                        {Environment.NewLine} 
                                        /* END POLYFILL  */ {Environment.NewLine} 
                                        {Environment.NewLine} 
                                        {response.Content}";
                }

                response.Success = true;
                response.Comment += "converted";

                return new ServiceResult<ResponseModel>(response);
            }
            catch (Exception exception)
            {
                logger.LogError($"An error occurred while trying to convert javascript with Babel: {exception}");
                response.Comment = exception.ToString();
                return new ServiceResult<ResponseModel>(response)
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessage = exception.Message,
                    ReasonPhrase = exception.Message
                };
            }
        }
    }
}
