using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.ContentBuilder.Interfaces;
using Api.Modules.ContentBuilder.Models;
using Api.Modules.Tenants.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Api.Modules.ContentBuilder.Services
{
    /// <inheritdoc cref="IContentBuilderService" />
    public class ContentBuilderService : IContentBuilderService, IScopedService
    {
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IObjectsService objectsService;
        private readonly IWiserItemsService wiserItemsService;
        private readonly IWiserTenantsService wiserTenantsService;

        /// <summary>
        /// Creates a new instance of <see cref="ContentBuilderService"/>.
        /// </summary>
        public ContentBuilderService(IDatabaseConnection clientDatabaseConnection, IObjectsService objectsService, IWiserItemsService wiserItemsService, IWiserTenantsService wiserTenantsService)
        {
            this.clientDatabaseConnection = clientDatabaseConnection;
            this.objectsService = objectsService;
            this.wiserItemsService = wiserItemsService;
            this.wiserTenantsService = wiserTenantsService;
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
        public async Task<ServiceResult<List<ContentBoxTemplateModel>>> GetTemplatesAsync(ClaimsIdentity identity)
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

            var results = new List<ContentBoxTemplateModel>();

            var query = $@"SELECT
    template.id,
    template.title,
    category.id AS categoryId,
    category.title AS category,
    CONCAT_WS('', html.value, html.long_value) AS html,
    file.file_name,
	link.ordering,
	linkToRoot.ordering AS parentOrdering
FROM {WiserTableNames.WiserItem} AS template
JOIN {WiserTableNames.WiserItemLink} AS link ON link.item_id = template.id AND link.type = 1
JOIN {WiserTableNames.WiserItem} AS category ON category.id = link.destination_item_id AND category.entity_type = 'map'
LEFT JOIN {WiserTableNames.WiserItemLink} AS linkToRoot ON linkToRoot.item_id = category.id AND linkToRoot.type = 1
LEFT JOIN {WiserTableNames.WiserItemFile} AS file ON file.item_id = template.id AND file.property_name = 'preview'
LEFT JOIN {WiserTableNames.WiserItemDetail} AS html ON html.item_id = template.id AND html.`key` = 'html'
WHERE template.entity_type = 'content-box-template'
GROUP BY category.id, template.id

UNION

SELECT
    template.id,
    template.title,
    category.id AS categoryId,
    category.title AS category,
    CONCAT_WS('', html.value, html.long_value) AS html,
    file.file_name,
	template.ordering,
	IFNULL(linkToRoot.ordering, category.ordering) AS parentOrdering
FROM {WiserTableNames.WiserItem} AS template
JOIN {WiserTableNames.WiserItem} AS category ON category.id = template.parent_item_id AND category.entity_type = 'map'
LEFT JOIN {WiserTableNames.WiserItemLink} AS linkToRoot ON linkToRoot.item_id = category.id AND linkToRoot.type = 1
LEFT JOIN {WiserTableNames.WiserItemFile} AS file ON file.item_id = template.id AND file.property_name = 'preview'
LEFT JOIN {WiserTableNames.WiserItemDetail} AS html ON html.item_id = template.id AND html.`key` = 'html'
WHERE template.entity_type = 'content-box-template'
GROUP BY category.id, template.id

ORDER BY ordering ASC, parentOrdering ASC";
            var dataTable = await clientDatabaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<List<ContentBoxTemplateModel>>(results);
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                var id = dataRow.Field<ulong>("id");
                results.Add(new ContentBoxTemplateModel
                {
                    Id = id,
                    Name = dataRow.Field<string>("title"),
                    CategoryId = dataRow.Field<ulong>("categoryId"),
                    Category = dataRow.Field<string>("category"),
                    Html = await wiserItemsService.ReplaceHtmlForViewingAsync(dataRow.Field<string>("html")),
                    Thumbnail = $"//{mainDomain}image/wiser2/{id}/preview/0/0/{dataRow.Field<string>("file_name")}"
                });
            }

            return new ServiceResult<List<ContentBoxTemplateModel>>(results);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<ContentBoxTemplateModel>>> GetTemplateCategoriesAsync(ClaimsIdentity identity)
        {
            var results = new List<ContentBoxTemplateModel>();

            var query = $@"SELECT
	category.id AS categoryId,
	category.title AS category,
	link.ordering,
	linkToRoot.ordering AS parentOrdering
FROM {WiserTableNames.WiserItem} AS template
JOIN {WiserTableNames.WiserItemLink} AS link ON link.item_id = template.id AND link.type = 1
JOIN {WiserTableNames.WiserItem} AS category ON category.id = link.destination_item_id AND category.entity_type = 'map'
LEFT JOIN {WiserTableNames.WiserItemLink} AS linkToRoot ON linkToRoot.item_id = category.id AND linkToRoot.type = 1
WHERE template.entity_type = 'content-box-template'
GROUP BY category.id

UNION

SELECT
	category.id AS categoryId,
	category.title AS category,
	template.ordering,
	IFNULL(linkToRoot.ordering, category.ordering) AS parentOrdering
FROM {WiserTableNames.WiserItem} AS template
JOIN {WiserTableNames.WiserItem} AS category ON category.id = template.parent_item_id AND category.entity_type = 'map'
LEFT JOIN {WiserTableNames.WiserItemLink} AS linkToRoot ON linkToRoot.item_id = category.id AND linkToRoot.type = 1
WHERE template.entity_type = 'content-box-template'
GROUP BY category.id

ORDER BY ordering ASC, parentOrdering ASC";
            var dataTable = await clientDatabaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<List<ContentBoxTemplateModel>>(results);
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                results.Add(new ContentBoxTemplateModel
                {
                    CategoryId = dataRow.Field<ulong>("categoryId"),
                    Category = dataRow.Field<string>("category")
                });
            }

            return new ServiceResult<List<ContentBoxTemplateModel>>(results);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> GetTemplateJavascriptFileAsync(ClaimsIdentity identity)
        {
            var tenant = (await wiserTenantsService.GetSingleAsync(identity)).ModelObject;
            var templatesResult = await GetTemplatesAsync(identity);
            if (templatesResult.StatusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<string>
                {
                    ErrorMessage = templatesResult.ErrorMessage,
                    StatusCode = templatesResult.StatusCode
                };
            }

            var templates = templatesResult.ModelObject ?? new List<ContentBoxTemplateModel>();

            var categories = new Dictionary<ulong, string>();
            foreach (var template in templates)
            {
                if (categories.ContainsKey(template.CategoryId))
                {
                    continue;
                }

                categories.Add(template.CategoryId, template.Category);
            }

            var categoriesArray = categories.Select(x => $"{{ id: {x.Key}, name: '{x.Value}' }}");

            var serializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented, 
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                }
            };

            var templatesForJavascript = templates.Select(x => new ContentBoxTemplateJavascriptModel
            {
                Category = x.CategoryId.ToString(),
                Html = x.Html,
                Thumbnail = x.Thumbnail,
                ContentClass = x.ContentClass,
                ContentCss = x.ContentCss,
                DesignId = x.Id
            });
            var javascript = $@"var data_templates = {{
    name: '{tenant.Name}',
    categories: [{String.Join(", ", categoriesArray)}],
    designs: {JsonConvert.SerializeObject(templatesForJavascript, serializerSettings)}
}};

try {{
    template_list.push(data_templates);
}} catch(e) {{
    //
}}";
            
            return new ServiceResult<string>(javascript);
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
