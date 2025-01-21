using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Babel.Models;

namespace Api.Modules.Babel.Interfaces;

/// <summary>
/// A service for using Babel to convert Javascript.
/// </summary>
public interface IBabelService
{
    /// <summary>
    /// Converts javascript, using Babel, to make it work in older browsers. This will convert ES6 code to ES5 and will add polyfills for functions that old browsers don't have built in support for.
    /// </summary>
    /// <param name="options">The content to convert and any additional options.</param>
    Task<ServiceResult<ResponseModel>> ConvertJavascriptAsync(RequestModel options);
}