using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.ContentBuilder.Models;

namespace Api.Modules.ContentBuilder.Interfaces
{
    /// <summary>
    /// A service for the content builder, to get HTML, snippets etc.
    /// </summary>
    public interface IContentBuilderService
    {
        /// <summary>
        /// Gets all snippets for the content builder. Snippets are pieces of HTML that the user can add in the Content Builder.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns>A list with zero or more <see cref="ContentBuilderSnippetModel"/>.</returns>
        Task<ServiceResult<List<ContentBuilderSnippetModel>>> GetSnippetsAsync(ClaimsIdentity identity);

        /// <summary>
        /// Gets the HTML of an item, for the Content Builder.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="itemId">The ID of the Wiser item that contains the HTML to get.</param>
        /// <param name="languageCode">Optional: The language code for the HTML, in case of a multi language website.</param>
        /// <param name="propertyName">Optional: The name of the property in the Wiser item that contains the HTML. Default value is "html".</param>
        /// <returns>The HTML as a string.</returns>
        Task<ServiceResult<string>> GetHtmlAsync(ClaimsIdentity identity, ulong itemId, string languageCode = "", string propertyName = "html");
    }
}
