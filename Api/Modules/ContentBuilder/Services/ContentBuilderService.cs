using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.ContentBuilder.Interfaces;
using Api.Modules.ContentBuilder.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;

namespace Api.Modules.ContentBuilder.Services
{
    /// <inheritdoc cref="IContentBuilderService" />
    public class ContentBuilderService : IContentBuilderService, IScopedService
    {
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IObjectsService objectsService;
        private readonly IWiserItemsService wiserItemsService;

        /// <summary>
        /// Creates a new instance of <see cref="ContentBuilderService"/>.
        /// </summary>
        public ContentBuilderService(IDatabaseConnection clientDatabaseConnection, IObjectsService objectsService, IWiserItemsService wiserItemsService)
        {
            this.clientDatabaseConnection = clientDatabaseConnection;
            this.objectsService = objectsService;
            this.wiserItemsService = wiserItemsService;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<ContentBuilderSnippetModel>>> GetSnippetsAsync(ClaimsIdentity identity)
        {
            // Determine main domain, using either the "maindomain" object or the "maindomain_wiser" object.
            var mainDomain = await objectsService.FindSystemObjectByDomainNameAsync("maindomain");
            if (String.IsNullOrWhiteSpace(mainDomain))
            {
                mainDomain = await objectsService.FindSystemObjectByDomainNameAsync("maindomain_wiser");
            }

            if (!mainDomain.EndsWith("/"))
            {
                mainDomain += "/";
            }

            var results = new List<ContentBuilderSnippetModel>();

            var query = $@"SELECT
                            snippet.id,
                            snippet.title,
                            category.id AS categoryId,
                            category.title AS category,
                            CONCAT_WS('', html.value, html.long_value) AS html,
                            file.file_name
                        FROM {WiserTableNames.WiserItem} AS snippet
                        JOIN {WiserTableNames.WiserItemLink} AS link ON link.item_id = snippet.id AND link.type = 1
                        JOIN {WiserTableNames.WiserItem} AS category ON category.id = link.destination_item_id AND category.entity_type = 'map'
                        LEFT JOIN {WiserTableNames.WiserItemLink} AS linkToRoot ON linkToRoot.item_id = category.id AND linkToRoot.type = 1
                        LEFT JOIN {WiserTableNames.WiserItemFile} AS file ON file.item_id = snippet.id AND file.property_name = 'preview'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS html ON html.item_id = snippet.id AND html.`key` = 'html'
                        WHERE snippet.entity_type = 'content-builder-snippet'
                        GROUP BY category.id, snippet.id
                        ORDER BY linkToRoot.ordering ASC, link.ordering ASC";
            var dataTable = await clientDatabaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<List<ContentBuilderSnippetModel>>(results);
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                var id = dataRow.Field<ulong>("id");
                results.Add(new ContentBuilderSnippetModel
                {
                    Id = id,
                    Name = dataRow.Field<string>("title"),
                    CategoryId = dataRow.Field<ulong>("categoryId"),
                    Category = dataRow.Field<string>("category"),
                    Html = await wiserItemsService.ReplaceHtmlForViewingAsync(dataRow.Field<string>("html")),
                    Thumbnail = $"//{mainDomain}image/wiser2/{id}/preview/0/0/{dataRow.Field<string>("file_name")}"
                });
            }

            return new ServiceResult<List<ContentBuilderSnippetModel>>(results);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> GetHtmlAsync(ClaimsIdentity identity, ulong itemId, string languageCode = "", string propertyName = "html")
        {
            if (itemId == 0)
            {
                return new ServiceResult<string>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "No item ID given"
                };
            }

            clientDatabaseConnection.AddParameter("itemId", itemId);
            clientDatabaseConnection.AddParameter("languageCode", languageCode ?? "");
            clientDatabaseConnection.AddParameter("propertyName", propertyName);
            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT IFNULL(CONCAT_WS('', html.value, html.long_value), '') AS html 
                                                            FROM {WiserTableNames.WiserItem} AS item
                                                            LEFT JOIN {WiserTableNames.WiserItemDetail} AS html ON html.item_id = item.id AND html.`key` = ?propertyName AND IFNULL(html.language_code, '') = ?languageCode
                                                            WHERE item.id = ?itemId
                                                            LIMIT 1");

            return dataTable.Rows.Count == 0 
                ? new ServiceResult<string>("") 
                : new ServiceResult<string>(await wiserItemsService.ReplaceHtmlForViewingAsync(dataTable.Rows[0].Field<string>("html")));
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> GetFrameworkAsync()
        {
            var framework = await objectsService.FindSystemObjectByDomainNameAsync("ContentBuilder_Framework");
            return new ServiceResult<string>(framework);
        }
    }
}
