using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Api.Core.Helpers;
using Api.Core.Interfaces;
using Api.Core.Models;
using Api.Core.Services;
using Api.Modules.Branches.Interfaces;
using Api.Modules.Kendo.Enums;
using Api.Modules.Templates.Helpers;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Interfaces.DataLayer;
using Api.Modules.Templates.Models;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.History;
using Api.Modules.Templates.Models.Measurements;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Templates.Models.Template;
using Api.Modules.Templates.Models.Template.WtsModels;
using Api.Modules.Tenants.Interfaces;
using GeeksCoreLibrary.Components.Account;
using GeeksCoreLibrary.Components.DataSelectorParser;
using GeeksCoreLibrary.Components.Filter;
using GeeksCoreLibrary.Components.Pagination;
using GeeksCoreLibrary.Components.Pagination.Models;
using GeeksCoreLibrary.Components.Repeater;
using GeeksCoreLibrary.Components.Repeater.Models;
using GeeksCoreLibrary.Components.ShoppingBasket;
using GeeksCoreLibrary.Components.ShoppingBasket.Models;
using GeeksCoreLibrary.Components.WebForm;
using GeeksCoreLibrary.Components.WebForm.Models;
using GeeksCoreLibrary.Components.WebPage;
using GeeksCoreLibrary.Components.WebPage.Models;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using LibSassHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUglify;
using NUglify.JavaScript;
using ITemplatesService = Api.Modules.Templates.Interfaces.ITemplatesService;

namespace Api.Modules.Templates.Services
{
    /// <inheritdoc cref="Interfaces.ITemplatesService" />
    public class TemplatesService : ITemplatesService, IScopedService
    {
        //The list of hardcodes query-strings
        private static readonly Dictionary<string, string> TemplateQueryStrings = new();

        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IWiserTenantsService wiserTenantsService;
        private readonly IStringReplacementsService stringReplacementsService;
        private readonly GeeksCoreLibrary.Modules.Templates.Interfaces.ITemplatesService gclTemplatesService;
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IDatabaseConnection wiserDatabaseConnection;
        private readonly IApiReplacementsService apiReplacementsService;
        private readonly ITemplateDataService templateDataService;
        private readonly IHistoryService historyService;
        private readonly IWiserItemsService wiserItemsService;
        private readonly IPagesService pagesService;
        private readonly IRazorViewEngine razorViewEngine;
        private readonly ITempDataProvider tempDataProvider;
        private readonly IObjectsService objectsService;
        private readonly IDatabaseHelpersService databaseHelpersService;
        private readonly ILogger<TemplatesService> logger;
        private readonly GclSettings gclSettings;
        private readonly ApiSettings apiSettings;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IBranchesService branchesService;
        private readonly IMeasurementsDataService measurementsDataService;
        private readonly IDynamicContentDataService dynamicContentDataService;
        private readonly IWtsConfigurationService wtsConfigurationService;

        /// <summary>
        /// Creates a new instance of TemplatesService.
        /// </summary>
        public TemplatesService(IHttpContextAccessor httpContextAccessor,
            IWiserTenantsService wiserTenantsService,
            IStringReplacementsService stringReplacementsService,
            GeeksCoreLibrary.Modules.Templates.Interfaces.ITemplatesService gclTemplatesService,
            IDatabaseConnection clientDatabaseConnection,
            IApiReplacementsService apiReplacementsService,
            ITemplateDataService templateDataService,
            IHistoryService historyService,
            IWiserItemsService wiserItemsService,
            IPagesService pagesService,
            IRazorViewEngine razorViewEngine,
            ITempDataProvider tempDataProvider,
            IObjectsService objectsService,
            IDatabaseHelpersService databaseHelpersService,
            ILogger<TemplatesService> logger,
            IOptions<GclSettings> gclSettings,
            IOptions<ApiSettings> apiSettings,
            IWebHostEnvironment webHostEnvironment,
            IBranchesService branchesService,
            IMeasurementsDataService measurementsDataService,
            IDynamicContentDataService dynamicContentDataService,
            IWtsConfigurationService wtsConfigurationService)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.wiserTenantsService = wiserTenantsService;
            this.stringReplacementsService = stringReplacementsService;
            this.gclTemplatesService = gclTemplatesService;
            this.clientDatabaseConnection = clientDatabaseConnection;
            this.apiReplacementsService = apiReplacementsService;
            this.templateDataService = templateDataService;
            this.historyService = historyService;
            this.wiserItemsService = wiserItemsService;
            this.pagesService = pagesService;
            this.razorViewEngine = razorViewEngine;
            this.tempDataProvider = tempDataProvider;
            this.objectsService = objectsService;
            this.databaseHelpersService = databaseHelpersService;
            this.logger = logger;
            this.gclSettings = gclSettings.Value;
            this.apiSettings = apiSettings.Value;
            this.webHostEnvironment = webHostEnvironment;
            this.branchesService = branchesService;
            this.measurementsDataService = measurementsDataService;
            this.dynamicContentDataService = dynamicContentDataService;
            this.wtsConfigurationService = wtsConfigurationService;

            if (clientDatabaseConnection is ClientDatabaseConnection connection)
            {
                wiserDatabaseConnection = connection.WiserDatabaseConnection;
            }
        }

        /// <inheritdoc />
        public ServiceResult<Template> Get(int templateId = 0, string templateName = null, string rootName = "")
        {
            if (templateId <= 0 && String.IsNullOrWhiteSpace(templateName))
            {
                throw new ArgumentException("No template ID or name entered.");
            }

            string groupingKey = null;
            string groupingPrefix = null;
            var groupingCreateObjectInsteadOfArray = false;
            var groupingKeyColumnName = "";
            var groupingValueColumnName = "";

            var content = TryGetTemplateQuery(templateName, ref groupingKey, ref groupingPrefix, ref groupingCreateObjectInsteadOfArray, ref groupingKeyColumnName, ref groupingValueColumnName);

            return new ServiceResult<Template>(new Template
            {
                Id = templateId,
                Name = templateName,
                Content = content
            });
        }

        /// <inheritdoc />
        public Task<ServiceResult<QueryTemplate>> GetQueryAsync(int templateId = 0, string templateName = null)
        {
            var result = GetQueryTemplate(templateId, templateName);

            return Task.FromResult(new ServiceResult<QueryTemplate>(result));
        }

        /// <inheritdoc />
        public async Task<ServiceResult<JToken>> GetAndExecuteQueryAsync(ClaimsIdentity identity, string templateName, IFormCollection requestPostData = null)
        {
            var tenant = (await wiserTenantsService.GetSingleAsync(identity)).ModelObject;

            // Set the encryption key for the GCL internally. The GCL can't know which key to use otherwise.
            GclSettings.Current.ExpiringEncryptionKey = tenant.EncryptionKey;

            var queryTemplate = GetQueryTemplate(0, templateName);
            queryTemplate.Content = apiReplacementsService.DoIdentityReplacements(queryTemplate.Content, identity, true);
            queryTemplate.Content = stringReplacementsService.DoHttpRequestReplacements(queryTemplate.Content, true);

            if (requestPostData != null && requestPostData.Keys.Any())
            {
                queryTemplate.Content = stringReplacementsService.DoReplacements(queryTemplate.Content, requestPostData, true);
            }

            var result = await gclTemplatesService.GetJsonResponseFromQueryAsync(queryTemplate, tenant.EncryptionKey);
            return new ServiceResult<JToken>(result);
        }

        private QueryTemplate GetQueryTemplate(int templateId = 0, string templateName = null)
        {
            if (templateId <= 0 && String.IsNullOrWhiteSpace(templateName))
            {
                throw new ArgumentException("No template ID or name entered.");
            }

            string groupingKey = null;
            string groupingPrefix = null;
            var groupingCreateObjectInsteadOfArray = false;
            var groupingKeyColumnName = "";
            var groupingValueColumnName = "";

            var content = TryGetTemplateQuery(templateName, ref groupingKey, ref groupingPrefix, ref groupingCreateObjectInsteadOfArray, ref groupingKeyColumnName, ref groupingValueColumnName);

            var result = new QueryTemplate
            {
                Id = templateId,
                Name = templateName,
                Content = content,
                Type = TemplateTypes.Query,
                GroupingSettings = new QueryGroupingSettings
                {
                    GroupingColumn = groupingKey,
                    GroupingFieldsPrefix = groupingPrefix,
                    ObjectInsteadOfArray = groupingCreateObjectInsteadOfArray,
                    GroupingKeyColumnName = groupingKeyColumnName,
                    GroupingValueColumnName = groupingValueColumnName
                }
            };

            return result;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> GetCssForHtmlEditorsAsync(ClaimsIdentity identity)
        {
            var outputCss = new StringBuilder();

            // Get stylesheets that are marked to load on every page.
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT
	template.template_id,
	template.template_data_minified
FROM {WiserTableNames.WiserTemplate} AS template
LEFT JOIN {WiserTableNames.WiserTemplate} AS otherVersion ON otherVersion.template_id = template.template_id AND otherVersion.version > template.version
WHERE template.use_in_wiser_html_editors = 1
AND template.template_type IN ({(int)TemplateTypes.Css}, {(int)TemplateTypes.Scss})
AND otherVersion.id IS NULL
ORDER BY template.ordering ASC");

            if (dataTable.Rows.Count > 0)
            {
                foreach (DataRow dataRow in dataTable.Rows)
                {
                    outputCss.Append(dataRow.Field<string>("template_data_minified"));
                }
            }

            // Replace URL to the domain.
            dataTable = await clientDatabaseConnection.GetAsync("SELECT `key`, `value` FROM easy_objects WHERE `key` IN ('maindomain', 'requiressl', 'maindomain_wiser')");
            var mainDomain = "";
            var requireSsl = false;
            var mainDomainWiser = "";
            var domainName = "";

            if (dataTable.Rows.Count > 0)
            {
                foreach (DataRow dataRow in dataTable.Rows)
                {
                    var key = dataRow.Field<string>("key");
                    var value = dataRow.Field<string>("value");
                    switch (key.ToLowerInvariant())
                    {
                        case "maindomain":
                            mainDomain = value;
                            break;
                        case "requiressl":
                            requireSsl = String.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "maindomain_wiser":
                            mainDomainWiser = value;
                            break;
                    }
                }

                if (!String.IsNullOrWhiteSpace(mainDomain))
                {
                    domainName = $"{(requireSsl ? "https" : "http")}://{mainDomain}/";
                }
                else if (!String.IsNullOrWhiteSpace(mainDomainWiser))
                {
                    domainName = $"{(requireSsl ? "https" : "http")}://{mainDomainWiser}/";
                }
            }

            outputCss = outputCss.Replace("(../", $"({domainName}");
            outputCss = outputCss.Replace("url('fonts", $"url('{domainName}css/fonts");

            return new ServiceResult<string>(outputCss.ToString());
        }

        /// <summary>
        /// Return the query-string as it was formally stored in the database. These strings are now hardcoded.
        /// Settings are also hardcoded now.
        /// </summary>
        /// <param name="templateName"></param>
        /// <param name="groupingCreateObjectInsteadOfArray"></param>
        /// <param name="groupingKey"></param>
        /// <param name="groupingPrefix"></param>
        /// <param name="groupingKeyColumnName"></param>
        /// <param name="groupingValueColumnName"></param>
        /// <returns></returns>
        private string TryGetTemplateQuery(string templateName, ref string groupingKey, ref string groupingPrefix, ref bool groupingCreateObjectInsteadOfArray, ref string groupingKeyColumnName, ref string groupingValueColumnName)
        {
            //make sure the queries are set
            InitTemplateQueries();

            //add special settings
            if (TemplateQueryStrings.ContainsKey(templateName))
            {
                if (new List<string>()
                    {
                        "GET_ITEM_DETAILS",
                        "GET_DATA_FOR_TABLE",
                        "GET_DATA_FOR_FIELD_TABLE"
                    }.Contains(templateName))
                {
                    groupingKey = "id";
                    groupingPrefix = "property_";
                    groupingCreateObjectInsteadOfArray = true;
                    groupingKeyColumnName = "name";
                    groupingValueColumnName = "value";
                }
                else if (templateName.Equals("GET_ITEM_META_DATA"))
                {
                    groupingKey = "id";
                    groupingPrefix = "permission";
                }
                else if (templateName.Equals("GET_CONTEXT_MENU"))
                {
                    groupingKey = "text";
                    groupingPrefix = "attr";
                }
                else if (templateName.Equals("GET_COLUMNS_FOR_LINK_TABLE"))
                {
                    groupingCreateObjectInsteadOfArray = true;
                }

                return TemplateQueryStrings[templateName];
            }

            return null;
        }

        /// <summary>
        /// Hardcode query-strings that before where stored in the database.
        /// </summary>
        private static void InitTemplateQueries()
        {
            if (TemplateQueryStrings.Count == 0)
            {
                //load all the template queries into the dictionary
                TemplateQueryStrings.Add("IMPORTEXPORT_GET_ENTITY_NAMES", @"SELECT `name`, module_id AS moduleId
FROM wiser_entity
WHERE `name` <> ''
ORDER BY `name`");
                TemplateQueryStrings.Add("SET_DATA_SELECTOR_REMOVED", @"UPDATE wiser_data_selector
SET removed = 1
WHERE id = {itemId};

SELECT ROW_COUNT() > 0 AS updateSuccessful;");
                TemplateQueryStrings.Add("UPDATE_LINK", @"SET @_linkId = {linkId};
SET @_destinationId = {destinationId};
SET @newOrderNumber = IFNULL((SELECT MAX(ordering) + 1 FROM wiser_itemlink WHERE destination_item_id = @destinationId), 1);
SET @_username = '{username}';
SET @_userId = '{encryptedUserId:decrypt(true)}';

# Update the ordering of all item links that come after the current item link.
UPDATE wiser_itemlink il1
JOIN wiser_itemlink il2 ON il2.destination_item_id = il1.destination_item_id AND il2.ordering > il1.ordering
SET il2.ordering = il2.ordering - 1
WHERE il1.id = @_linkId;

# Update the actual link and add it to the bottom.
UPDATE wiser_itemlink
SET destination_item_id = @destinationId, ordering = @newOrderNumber
WHERE id = @_linkId;");
                TemplateQueryStrings.Add("GET_OPTIONS_FOR_DEPENDENCY", @"SELECT DISTINCT entity_name AS entityName, IF(tab_name = """", ""Gegevens"", tab_name) as tabName, CONCAT(IF(tab_name = """", ""Gegevens"", tab_name), "" --> "", display_name) AS displayName, property_name AS propertyName FROM wiser_entityproperty
WHERE entity_name = '{entityName}'
ORDER BY displayName");

                TemplateQueryStrings.Add("DELETE_ENTITYPROPERTY", @"DELETE FROM wiser_entityproperty WHERE tab_name = '{tabName}' AND entity_name = '{entityName}' AND id = '{entityPropertyId}'");
                TemplateQueryStrings.Add("GET_ENTITY_PROPERTIES_ADMIN", @"SELECT id, entity_name AS entityName, tab_name AS tabName, display_name AS displayName, ordering FROM wiser_entityproperty
WHERE tab_name = IF('{tabName}' = 'Gegevens', '', '{tabName}') AND entity_name = '{entityName}'
ORDER BY ordering ASC");
                TemplateQueryStrings.Add("GET_ENTITY_LIST", @"SELECT 
	entity.id,
	IF(entity.name = '', 'ROOT', entity.name) AS name,
	CONCAT(IFNULL(module.name, CONCAT('Module #', entity.module_id)), ' --> ', IFNULL(CONCAT(NULLIF(entity.friendly_name, ''), ' (', entity.name, ')'), IF(entity.name = '', 'ROOT', entity.name))) AS displayName,
    entity.module_id AS moduleId 
FROM wiser_entity AS entity
LEFT JOIN wiser_module AS module ON module.id = entity.module_id
ORDER BY module.name ASC, entity.module_id ASC, entity.name ASC");
                TemplateQueryStrings.Add("GET_LANGUAGE_CODES", @"SELECT
    language_code AS text,
    language_code AS `value`
FROM wiser_entityproperty
WHERE
    (entity_name = '{entityName}' OR link_type = '{linkType}')
    AND IF(property_name = '', CreateJsonSafeProperty(display_name), property_name) = '{propertyName}'
    AND language_code <> ''
GROUP BY language_code
ORDER BY language_code");
                TemplateQueryStrings.Add("GET_ENTITY_PROPERTIES_TABNAMES", @"SELECT id, IF(tab_name = '', 'Gegevens', tab_name) AS tabName FROM wiser_entityproperty
WHERE entity_name = '{entityName}'
GROUP BY tab_name
ORDER BY tab_name ASC");
                TemplateQueryStrings.Add("GET_ROLES", @"SELECT id AS id, role_name AS roleName FROM wiser_roles
WHERE role_name != ''
ORDER BY role_name ASC;");
                TemplateQueryStrings.Add("INSERT_ROLE", @"INSERT INTO `wiser_roles` (`role_name`) VALUES ('{displayName}');");
                TemplateQueryStrings.Add("DELETE_ROLE", @"DELETE FROM `wiser_roles` WHERE id={roleId}");
                TemplateQueryStrings.Add("UPDATE_ENTITY_PROPERTY_PERMISSIONS", @"INSERT INTO `wiser_permission` (
    role_id, 
    entity_name, 
    item_id, 
    entity_property_id, 
    permissions
) VALUES (
    {roleId},
    '',
    0,
    {entityId},
    {permissionCode}
)
ON DUPLICATE KEY UPDATE permissions = {permissionCode}");
                TemplateQueryStrings.Add("GET_GROUPNAME_FOR_SELECTION", @"SELECT DISTINCT group_name AS groupName FROM `wiser_entityproperty`
WHERE entity_name = '{selectedEntityName}' AND tab_name = '{selectedTabName}';");
                TemplateQueryStrings.Add("UPDATE_API_AUTHENTICATION_DATA", @"UPDATE wiser_api_connection SET authentication_data = '{authenticationData}' WHERE id = {id:decrypt(true)};");
                TemplateQueryStrings.Add("DELETE_MODULE", @"DELETE FROM `wiser_module` WHERE id = {module_id};");
                TemplateQueryStrings.Add("INSERT_ENTITYPROPERTY", @"SET @newOrderNr = IFNULL((SELECT MAX(ordering)+1 FROM wiser_entityproperty WHERE entity_name='{entityName}' AND tab_name = '{tabName}'),1);

INSERT INTO wiser_entityproperty(entity_name, tab_name, display_name, property_name, ordering)
VALUES('{entityName}', '{tabName}', '{displayName}', '{propertyName}', @newOrderNr);
#spaties vervangen door underscore");
                TemplateQueryStrings.Add("RENAME_ITEM", @"SET @item_id={itemid:decrypt(true)};
SET @newname='{name}';

UPDATE wiser_item SET title=@newname WHERE id=@item_id LIMIT 1;");
                TemplateQueryStrings.Add("GET_UNDERLYING_LINKED_TYPES", @"SET @_entity_name = IF(
    '{entityName}' NOT LIKE '{%}',
    '{entityName}',
    # Check for old query string name. Takes the first item in a comma-separated list of entity type names.
    IF(
        '{entityTypes}' NOT LIKE '{%}',
        SUBSTRING_INDEX('{entityTypes}', ',', 1),
        ''
    )
);

SELECT connected_entity_type AS entityType, type AS linkTypeNumber, `name` AS linkTypeName
FROM wiser_link
WHERE destination_entity_type = @_entity_name AND show_in_data_selector = 1
ORDER BY entityType");
                TemplateQueryStrings.Add("GET_PARENT_LINKED_TYPES", @"SET @_entity_name = IF(
    '{entityName}' NOT LIKE '{%}',
    '{entityName}',
    # Check for old query string name. Takes the first item in a comma-separated list of entity type names.
    IF(
        '{entityTypes}' NOT LIKE '{%}',
        SUBSTRING_INDEX('{entityTypes}', ',', 1),
        ''
    )
);

SELECT destination_entity_type AS entityType, type AS linkTypeNumber, `name` AS linkTypeName
FROM wiser_link
WHERE connected_entity_type = @_entity_name AND show_in_data_selector = 1
ORDER BY entityType");
                TemplateQueryStrings.Add("GET_ITEM_VALUE", @"SELECT
	id,
    `key`,
    IF(long_value IS NULL OR long_value = '', `value`, long_value) AS `value`
FROM wiser_itemdetail
WHERE item_id = {itemId:decrypt(true)}
AND `key` = '{propertyName}'");
                TemplateQueryStrings.Add("GET_ITEM_DETAILS", @"SET @_itemId = {itemId:decrypt(true)};
SET @userId = {encryptedUserId:decrypt(true)};

SELECT 
	i.id, 
	i.id AS encryptedId_encrypt_withdate,
    CASE i.published_environment
    	WHEN 0 THEN 'onzichtbaar'
        WHEN 1 THEN 'dev'
        WHEN 2 THEN 'test'
        WHEN 3 THEN 'acceptatie'
        WHEN 4 THEN 'live'
    END AS published_environment,
	i.title, 
	i.entity_type, 
	IFNULL(p.property_name, p.display_name) AS property_name,
	CONCAT(IFNULL(id.`value`, ''), IFNULL(id.`long_value`, ''), IFNULL(CONCAT('[', GROUP_CONCAT(DISTINCT CONCAT('{ ""itemId"": ', wif.item_id, ', ""fileId"": ', wif.id, ', ""name"": ""', wif.file_name, '"", ""title"": ""', wif.title, '"", ""extension"": ""', wif.extension, '"", ""size"": ', IFNULL(OCTET_LENGTH(wif.content), 0), ' }')), ']'), '')) AS property_value
FROM wiser_item i
LEFT JOIN wiser_entityproperty p ON p.entity_name = i.entity_type
LEFT JOIN wiser_itemdetail id ON id.item_id = i.id AND ((p.property_name IS NOT NULL AND p.property_name <> '' AND id.`key` = p.property_name) OR ((p.property_name IS NULL OR p.property_name = '') AND id.`key` = p.display_name))
LEFT JOIN wiser_itemfile wif ON wif.item_id = i.id AND wif.property_name = IFNULL(p.property_name, p.display_name)

# Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
LEFT JOIN wiser_user_roles user_role ON user_role.user_id = @userId
LEFT JOIN wiser_permission permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id

WHERE i.id = @_itemId
AND (permission.id IS NULL OR (permission.permissions & 1) > 0)
GROUP BY i.id, p.id");
                TemplateQueryStrings.Add("GET_TITLE", @"SET @_itemId = {itemId:decrypt(true)};
SELECT title FROM wiser_item WHERE id = @_itemId;");
                TemplateQueryStrings.Add("GET_PROPERTIES_OF_ENTITY", @"SELECT
	IF(tab_name = '', 'Gegevens', tab_name) AS tabName,
    display_name as displayName,
    IF(property_name = '', display_name, property_name) AS propertyName
FROM wiser_entityproperty
WHERE entity_name = '{entityType}'
AND inputtype NOT IN ('file-upload', 'grid', 'image-upload', 'sub-entities-grid', 'item-linker', 'linked-item', 'action-button')

UNION SELECT 'Algemeen' AS tabName, 'ID' AS displayName, 'id' AS propertyName
UNION SELECT 'Algemeen' AS tabName, 'UUID' AS displayName, 'unique_uuid' AS propertyName
UNION SELECT 'Algemeen' AS tabName, 'Toegevoegd door' AS displayName, 'added_by' AS propertyName
UNION SELECT 'Algemeen' AS tabName, 'Gewijzigd door' AS displayName, 'changed_by' AS propertyName
UNION SELECT 'Algemeen' AS tabName, 'Naam' AS displayName, 'title' AS propertyName

ORDER BY tabName ASC, displayName ASC");
                TemplateQueryStrings.Add("IMPORTEXPORT_GET_LINK_TYPES", @"SELECT type AS id, `name`
FROM wiser_link
ORDER BY `name`");
                TemplateQueryStrings.Add("MOVE_ITEM", @"#Item verplaatsen naar ander item
SET @src_id = '{source:decrypt(true)}';
SET @dest_id = '{destination:decrypt(true)}';
SET @location = '{position}'; #can be over after or before
SET @srcparent = '{source_parent:decrypt(true)}'; #this must come from client because items can have multiple parents
SET @destparent = '{dest_parent:decrypt(true)}'; #this must come from client because items can have multiple parents
SET @oldordering = (SELECT ordering FROM wiser_itemlink WHERE item_id=@src_id AND destination_item_id=@srcparent LIMIT 1);
#SET @ordernumbernewitem = (SELECT ordering FROM wiser_itemlink WHERE item_id@dest_id AND destination_item_id=@destparent LIMIT 1);
SET @newordernumbernewfolder = (SELECT max(ordering)+1 FROM wiser_itemlink WHERE destination_item_id=IF(@location = 'over', @dest_id, @destparent));
SET @newordernumbernewfolder = IFNULL(@newordernumbernewfolder,1);
SET @newordernumber = (SELECT ordering FROM wiser_itemlink WHERE item_id=@dest_id AND destination_item_id=@destparent LIMIT 1);
SET @sourceType = (SELECT entity_type FROM wiser_item WHERE id = @src_id);
SET @destinationType = (SELECT entity_type FROM wiser_item WHERE id = IF(@location = 'over', @dest_id, @destparent));
SET @destinationAcceptedChildTypes = (
	SELECT GROUP_CONCAT(e.accepted_childtypes)
	FROM wiser_entity AS e
	LEFT JOIN wiser_item AS i ON i.entity_type = e.name AND i.id = IF(@location = 'over', @dest_id, @destparent)
	WHERE i.id IS NOT NULL
	OR (@location <> 'over' AND @destparent = '0' AND e.name = '')
	OR (@location = 'over' AND @dest_id = '0' AND e.name = '')
);

#Items voor of na de plaatsing (before/after) 1 plek naar achteren schuiven (niet bij plaatsen op een ander item, want dan komt het nieuwe item altijd achteraan)
UPDATE wiser_itemlink
SET
  ordering=ordering+1
WHERE destination_item_id=@destparent
AND ordering>=IF(@location='before',@newordernumber,@newordernumber+1) #als het before is dan alles ophogen vanaf, bij after alles erna
AND item_id<>@src_id
AND (@location='before' OR @location='after')
AND destination_item_id=@srcparent
AND FIND_IN_SET(@sourceType, @destinationAcceptedChildTypes);

#Node plaatsen op nieuwe plek
UPDATE wiser_itemlink 
SET
  destination_item_id=IF(@location='over',@dest_id,@destparent), #bij 'over' de ID wordt de parent, bij before/after wordt de parent van de nieuwe node de nieuwe parent
	ordering=IF(@location='before',@newordernumber,IF(@location='after',@newordernumber+1,@newordernumbernewfolder)) #als het before is dan op die plek zetten anders 1 hoger
WHERE item_id=@src_id
AND destination_item_id=@srcparent
AND FIND_IN_SET(@sourceType, @destinationAcceptedChildTypes);

#In oude map gat opvullen (items opschuiven naar voren)
UPDATE wiser_itemlink
SET
  ordering=ordering-1
WHERE destination_item_id=@srcparent
AND ordering>@oldordering
AND FIND_IN_SET(@sourceType, @destinationAcceptedChildTypes);

SELECT 
	@isexpanded, 
    @location, 
    @newordernumber, 
    @destparent, 
	CASE WHEN NOT FIND_IN_SET(@sourceType, @destinationAcceptedChildTypes)
        THEN (SELECT CONCAT('Items van type ""', @sourceType, '"" mogen niet toegevoegd worden onder items van type ""', @destinationType, '"".') AS error)
        ELSE ''
    END AS error;");
                TemplateQueryStrings.Add("GET_CONTEXT_MENU", @"SET @_itemId = {itemId:decrypt(true)};
SET @_moduleId = {moduleId};
SET @entity_type = (SELECT entity_type FROM wiser_item WHERE id=@_itemId);
SET @itemname = (SELECT title FROM wiser_item WHERE id=@_itemId);
SET @userId = {encryptedUserId:decrypt(true)};

SELECT 
	CONCAT('\'', @itemname, '\' hernoemen (F2)') AS text, 
    'icon-rename' AS spriteCssClass,
    'RENAME_ITEM' AS attraction,
    i.entity_type AS attrentity_type
    #the JSON must consist of a subnode with attributes, so attr is the name of the json object containing 'action' as a value, herefore the name here is attr...action
    FROM wiser_item i 
    
    # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	LEFT JOIN wiser_user_roles user_role ON user_role.user_id = @userId
	LEFT JOIN wiser_permission permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id
	LEFT JOIN wiser_permission permissionModule ON permissionModule.role_id = user_role.role_id AND permissionModule.module_id = i.moduleid
    
    WHERE i.id = @_itemId 
    AND i.readonly = 0
	AND (
			(permissionModule.id IS NULL AND permission.id IS NULL)
			OR
			(permission.id IS NULL AND (permissionModule.permissions & 4) > 0)
			OR 
			(permission.permissions & 4) > 0
		)
    
UNION
    SELECT CONCAT('Nieuw(e) \'', i.name, '\' aanmaken (SHIFT+N)'), 
	i.icon_add,
    'CREATE_ITEM', 
	i.name
    FROM wiser_entity i
    JOIN wiser_entity we ON we.module_id=@_moduleId AND we.name=@entity_type
    
    # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	LEFT JOIN wiser_user_roles user_role ON user_role.user_id = @userId
	LEFT JOIN wiser_permission permission ON permission.role_id = user_role.role_id AND permission.item_id = @_itemId
	LEFT JOIN wiser_permission permissionModule ON permissionModule.role_id = user_role.role_id AND permissionModule.module_id = @_moduleId
    
    WHERE i.module_id = @_moduleId
    AND i.`name` IN (we.accepted_childtypes) AND i.name <> ''
	AND (
			(permissionModule.id IS NULL AND permission.id IS NULL)
			OR
			(permission.id IS NULL AND (permissionModule.permissions & 2) > 0)
			OR 
			(permission.permissions & 2) > 0
		)

UNION
	SELECT CONCAT('\'', @itemname, '\' dupliceren (SHIFT+D)') AS text, 
	'icon-document-duplicate',
	'DUPLICATE_ITEM',
    i.entity_type AS attrentity_type
    FROM wiser_item i 
    
    # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	LEFT JOIN wiser_user_roles user_role ON user_role.user_id = @userId
	LEFT JOIN wiser_permission permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id
	LEFT JOIN wiser_permission permissionModule ON permissionModule.role_id = user_role.role_id AND permissionModule.module_id = i.moduleid
    
    WHERE i.id = @_itemId 
    AND i.readonly = 0
	AND (
			(permissionModule.id IS NULL AND permission.id IS NULL)
			OR
			(permission.id IS NULL AND (permissionModule.permissions & 2) > 0)
			OR 
			(permission.permissions & 2) > 0
		)

UNION
	SELECT CONCAT('Publiceer naar live'),
	'icon-globe',
	'PUBLISH_LIVE',
    i.entity_type AS attrentity_type
    FROM wiser_item i 
    
    # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	LEFT JOIN wiser_user_roles user_role ON user_role.user_id = @userId
	LEFT JOIN wiser_permission permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id
	LEFT JOIN wiser_permission permissionModule ON permissionModule.role_id = user_role.role_id AND permissionModule.module_id = i.moduleid
    
    WHERE i.id=@_itemId 
    AND i.published_environment <> 4
    AND i.readonly = 0
	AND (
			(permissionModule.id IS NULL AND permission.id IS NULL)
			OR
			(permission.id IS NULL AND (permissionModule.permissions & 4) > 0)
			OR 
			(permission.permissions & 4) > 0
		)
    
UNION
	SELECT CONCAT('\'', @itemname, '\' tonen') AS text, 
	'item-light-on',
	'PUBLISH_ITEM',
    i.entity_type AS attrentity_type
    FROM wiser_item i 
    
    # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	LEFT JOIN wiser_user_roles user_role ON user_role.user_id = @userId
	LEFT JOIN wiser_permission permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id
	LEFT JOIN wiser_permission permissionModule ON permissionModule.role_id = user_role.role_id AND permissionModule.module_id = i.moduleid
    
    WHERE i.id = @_itemId 
    AND i.published_environment = 0
    AND i.readonly = 0
	AND (
			(permissionModule.id IS NULL AND permission.id IS NULL)
			OR
			(permission.id IS NULL AND (permissionModule.permissions & 4) > 0)
			OR 
			(permission.permissions & 4) > 0
		)
    
UNION
	SELECT CONCAT('\'', @itemname, '\' verbergen') AS text, 
	'icon-light-off',
	'HIDE_ITEM',
    i.entity_type AS attrentity_type
    FROM wiser_item i 
    
    # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	LEFT JOIN wiser_user_roles user_role ON user_role.user_id = @userId
	LEFT JOIN wiser_permission permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id
	LEFT JOIN wiser_permission permissionModule ON permissionModule.role_id = user_role.role_id AND permissionModule.module_id = i.moduleid
    
    WHERE i.id = @_itemId 
    AND i.published_environment > 0
    AND i.readonly = 0
	AND (
			(permissionModule.id IS NULL AND permission.id IS NULL)
			OR
			(permission.id IS NULL AND (permissionModule.permissions & 4) > 0)
			OR 
			(permission.permissions & 4) > 0
		)
    
UNION
	SELECT CONCAT('\'', @itemname, '\' verwijderen (DEL)') AS text, 
	'icon-delete',
	'REMOVE_ITEM',
    i.entity_type AS attrentity_type
    FROM wiser_item i 
    
    # Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
	LEFT JOIN wiser_user_roles user_role ON user_role.user_id = @userId
	LEFT JOIN wiser_permission permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id
	LEFT JOIN wiser_permission permissionModule ON permissionModule.role_id = user_role.role_id AND permissionModule.module_id = i.moduleid
    
    WHERE i.id = @_itemId 
    AND i.readonly = 0
	AND (
			(permissionModule.id IS NULL AND permission.id IS NULL)
			OR
			(permission.id IS NULL AND (permissionModule.permissions & 8) > 0)
			OR 
			(permission.permissions & 8) > 0
		)");
                TemplateQueryStrings.Add("GET_COLUMNS_FOR_LINK_TABLE", @"SET @destinationId = {id:decrypt(true)};
SET @_linkTypeNumber = IF('{linkTypeNumber}' LIKE '{%}' OR '{linkTypeNumber}' = '', '2', '{linkTypeNumber}');

SELECT 
	CONCAT('property_.', CreateJsonSafeProperty(IF(p.property_name IS NULL OR p.property_name = '', p.display_name, p.property_name))) AS field,
    p.display_name AS title,
    p.overview_width AS width
FROM wiser_entityproperty p 
JOIN wiser_item i ON i.entity_type = p.entity_name
JOIN wiser_itemlink il ON il.item_id = i.id AND il.destination_item_id = @destinationId AND il.type = @_linkTypeNumber
WHERE p.visible_in_overview = 1
GROUP BY IF(p.property_name IS NULL OR p.property_name = '', p.display_name, p.property_name)
ORDER BY p.ordering;");
                TemplateQueryStrings.Add("GET_MODULES", @"SELECT id, name as moduleName
FROM wiser_module
ORDER BY name ASC;
");
                TemplateQueryStrings.Add("GET_ROLE_RIGHTS", @"SELECT
	properties.id AS `propertyId`,
	properties.entity_name AS `entityName`,
	properties.display_name as `displayName`,
    properties.tab_name AS `tabName`,
    properties.group_name AS `groupName`,
	IFNULL(permissions.permissions, 0) AS `permission`,
    {roleId} AS `roleId`
FROM `wiser_entityproperty` AS properties
LEFT JOIN `wiser_permission` AS permissions ON permissions.entity_property_id = properties.id AND permissions.role_id = {roleId}
WHERE NULLIF(properties.display_name, '') IS NOT NULL
	AND NULLIF(properties.entity_name, '') IS NOT NULL
GROUP BY properties.id
ORDER BY properties.entity_name, properties.tab_name, properties.group_name, properties.display_name");

                TemplateQueryStrings.Add("GET_DATA_SELECTOR_BY_ID", @"SET @_id = {id};

SELECT
    dataSelector.id,
    dataSelector.`name`,
    dataSelector.module_selection AS modules,
    dataSelector.request_json AS requestJson,
    dataSelector.saved_json AS savedJson,
    dataSelector.show_in_export_module AS showInExportModule,
    dataSelector.show_in_communication_module AS showInCommunicationModule,
    dataSelector.available_for_rendering AS availableForRendering,
    dataSelector.show_in_dashboard AS showInDashboard,
    dataSelector.available_for_branches AS availableForBranches,
    IFNULL(GROUP_CONCAT(permission.role_id), '') AS allowedRoles
FROM wiser_data_selector AS dataSelector
LEFT JOIN wiser_permission AS permission ON permission.data_selector_id = dataSelector.id
WHERE dataSelector.id = @_id");

                TemplateQueryStrings.Add("GET_ITEM_ENVIRONMENTS", @"SELECT
	item.id AS id_encrypt_withdate,
	item.id AS plainItemId,
	item.published_environment,
    IFNULL(item.changed_on, item.added_on) AS changed_on
FROM wiser_item AS item
JOIN wiser_entity AS entity ON entity.name = item.entity_type AND entity.module_id = item.moduleid AND entity.enable_multiple_environments = 1
WHERE item.original_item_id = {mainItemId:decrypt(true)}
AND item.original_item_id > 0
AND item.published_environment > 0");
                TemplateQueryStrings.Add("GET_ALL_ITEMS_OF_TYPE", @"#Get all the items for the treeview
SET @mid = {moduleid};
SET @_entityType = '{entityType}';
SET @_checkId = '{checkId:decrypt(true)}';
SET @_ordering = IF('{orderBy}' LIKE '{%}', '', '{orderBy}');

SELECT 
	i.id AS id_encrypt_withdate,
  	i.title AS name,
  	0 AS haschilds,
  	we.icon AS spriteCssClass,
    we.icon AS collapsedSpriteCssClass,
    we.icon_expanded AS expandedSpriteCssClass,
  	ilp.destination_item_id AS destination_item_id_encrypt_withdate,
    IF(checked.id IS NULL, 0, 1) AS checked
FROM wiser_item i
JOIN wiser_entity we ON we.name = i.entity_type AND we.show_in_tree_view = 1
LEFT JOIN wiser_itemlink ilp ON ilp.item_id = i.id
LEFT JOIN wiser_itemlink checked ON checked.item_id = i.id AND checked.destination_item_id = @_checkId AND @_checkId <> '0'
WHERE i.moduleid = @mid
AND (@_entityType = '' OR i.entity_type = @_entityType)
GROUP BY i.id
ORDER BY 
    CASE WHEN @_ordering = 'title' THEN i.title END ASC,
	CASE WHEN @_ordering <> 'title' THEN ilp.ordering END ASC");
                TemplateQueryStrings.Add("GET_COLUMNS_FOR_TABLE", @"SET @selected_id = {itemId:decrypt(true)}; # 3077

SELECT 
	CONCAT('property_.', CreateJsonSafeProperty(LOWER(IF(p.property_name IS NULL OR p.property_name = '', p.display_name, p.property_name)))) AS field,
    p.display_name AS title,
    p.overview_width AS width
FROM wiser_itemlink il
JOIN wiser_item i ON i.id=il.item_id
JOIN wiser_entityproperty p ON p.entity_name=i.entity_type AND p.visible_in_overview=1
WHERE il.destination_item_id=@selected_id
GROUP BY p.property_name
ORDER BY p.ordering;");
                TemplateQueryStrings.Add("GET_DATA_FOR_TABLE", @"SET @selected_id = {itemId:decrypt(true)}; # 3077
SET @userId = {encryptedUserId:decrypt(true)};

SELECT
    i.id,
    i.id AS encryptedId_encrypt_withdate,
    CASE i.published_environment
    	WHEN 0 THEN 'onzichtbaar'
        WHEN 1 THEN 'dev'
        WHEN 2 THEN 'test'
        WHEN 3 THEN 'acceptatie'
        WHEN 4 THEN 'live'
    END AS published_environment,
    i.title,
    i.entity_type,
    CreateJsonSafeProperty(LOWER(IF(id.id IS NOT NULL, id.`key`, id2.`key`))) AS property_name,
    IF(id.id IS NOT NULL, id.`value`, id2.`value`) AS property_value,
    il.id AS link_id
FROM wiser_itemlink il
JOIN wiser_item i ON i.id = il.item_id

LEFT JOIN wiser_entityproperty p ON p.entity_name = i.entity_type AND p.visible_in_overview = 1
LEFT JOIN wiser_itemdetail id ON id.item_id = il.item_id AND ((p.property_name IS NOT NULL AND p.property_name <> '' AND id.`key` = p.property_name) OR ((p.property_name IS NULL OR p.property_name = '') AND id.`key` = p.display_name))

LEFT JOIN wiser_entityproperty p2 ON p2.link_type = il.type AND p.visible_in_overview = 1
LEFT JOIN wiser_itemlinkdetail id2 ON id2.itemlink_id = il.id AND ((p2.property_name IS NOT NULL AND p2.property_name <> '' AND id2.`key` = p2.property_name) OR ((p2.property_name IS NULL OR p2.property_name = '') AND id2.`key` = p2.display_name))

# Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
LEFT JOIN wiser_user_roles user_role ON user_role.user_id = @userId
LEFT JOIN wiser_permission permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id

WHERE il.destination_item_id = @selected_id
AND (permission.id IS NULL OR (permission.permissions & 1) > 0)
GROUP BY il.item_id, IFNULL(id.id, id2.id)
ORDER BY il.ordering, i.title");
                TemplateQueryStrings.Add("GET_ITEMLINKS_BY_ENTITY", @"SELECT
    # reuse fields to define text and values for the kendo dropdown
    type AS type_text,
    type AS type_value
FROM wiser_itemlink AS link
JOIN wiser_item AS item ON item.id = link.item_id AND item.entity_type = '{entity_name}'
GROUP BY type");
                TemplateQueryStrings.Add("GET_ENTITY_PROPERTIES", @"SET @_entity_name = IF(
    '{entityName}' NOT LIKE '{%}',
    '{entityName}',
    # Check for old query string name.
    IF(
        '{entity_types}' NOT LIKE '{%}',
        SUBSTRING_INDEX('{entityTypes}', ',', 1),
        ''
    )
);

SELECT
    property.`value`,
    property.entityName,
    # The display name is used in the field editor.
    property.displayName,
    # These fields are just to ensure the properties exist in the Kendo data item.
    '' AS languageCode,
    '' AS aggregation,
    '' AS formatting,
    '' AS fieldAlias,
    'ASC' AS direction,
    # These fields are deprecated and will be removed in the future.
    property.text,
    property.text AS originalText
FROM (
    # ID.
    SELECT 'ID' AS displayName, 'ID' AS text, 'id' AS `value`, @_entity_name AS entityName, 0 AS dynamicField, '0' AS sort

    UNION

    # Encrypted ID.
    SELECT 'Versleuteld ID' AS displayName, 'Versleuteld ID' AS text, 'idencrypted' AS `value`, @_entity_name AS entityName, 0 AS dynamicField, '1' AS sort

    UNION

    # Unique ID.
    SELECT 'Uniek ID' AS displayName, 'Uniek ID' AS text, 'unique_uuid' AS `value`, @_entity_name AS entityName, 0 AS dynamicField, '2' AS sort

    UNION

    # Title.
    SELECT 'Item titel' AS displayName, 'Item titel' AS text, 'itemtitle' AS `value`, @_entity_name AS entityName, 0 AS dynamicField, '3' AS sort

    UNION

    # Changed on.
    SELECT 'Gewijzigd op' AS displayName, 'Gewijzigd op' AS text, 'changed_on' AS `value`, @_entity_name AS entityName, 0 AS dynamicField, '4' AS sort

    UNION

    # Changed by.
    SELECT 'Gewijzigd door' AS displayName, 'Gewijzigd door' AS text, 'changed_by' AS `value`, @_entity_name AS entityName, 0 AS dynamicField, '5' AS sort

    UNION

    # Entity properties.
    (SELECT
        IF(
            # Check if there are more than one properties with the same property name.
            COUNT(*) > 1,
            # If True; Use the property name with the character capitalizted to create the display name.
            CONCAT(UPPER(SUBSTR(property_name, 1, 1)), SUBSTR(property_name, 2)),
            # If False; Use the property's own display name.
            display_name
        ) AS displayName,
        CONCAT_WS(' - ', entity_name, IF(COUNT(*) > 1, CONCAT(UPPER(SUBSTR(property_name, 1, 1)), SUBSTR(property_name, 2)), display_name)) AS text,
        IF(property_name = '', CreateJsonSafeProperty(display_name), property_name) AS `value`,
        entity_name AS entityName,
        1 AS dynamicField,
        IF(COUNT(*) > 1, CONCAT(UPPER(SUBSTR(property_name, 1, 1)), SUBSTR(property_name, 2)), display_name) AS sort
    FROM wiser_entityproperty
    WHERE
     	entity_name = @_entity_name
        # Some entities should be ignored due to their input types.
        AND inputtype NOT IN (
            'action-button',
            'auto-increment',
            'button',
            'chart',
            'data-selector',
            'empty',
            'file-upload',
            'grid',
            'image-upload',
            'item-linker',
            'linked-item',
            'querybuilder',
            'scheduler',
            'sub-entities-grid',
            'timeline'
        )
    GROUP BY `value`)

    UNION

    # SEO variants of the entity properties.
    (SELECT
        CONCAT(
            IF(
                # Check if there are more than one properties with the same property name.
                COUNT(*) > 1,
                # If True; Use the property name with the character capitalizted to create the display name.
                CONCAT(UPPER(SUBSTR(property_name, 1, 1)), SUBSTR(property_name, 2)),
                # If False; Use the property's own display name.
                display_name
            ),
            ' (SEO)'
        ) AS displayName,
        CONCAT(CONCAT_WS(' - ', entity_name, IF(COUNT(*) > 1, CONCAT(UPPER(SUBSTR(property_name, 1, 1)), SUBSTR(property_name, 2)), display_name)), ' (SEO)') AS text,
        CONCAT(IF(property_name = '', CreateJsonSafeProperty(display_name), property_name), '_SEO') AS `value`,
        entity_name AS entityName,
        1 AS dynamicField,
        CONCAT(IF(COUNT(*) > 1, CONCAT(UPPER(SUBSTR(property_name, 1, 1)), SUBSTR(property_name, 2)), display_name), ' (SEO)') AS sort
    FROM wiser_entityproperty
    WHERE
     	entity_name = @_entity_name
        # Some entities should be ignored due to their input types.
        AND inputtype NOT IN (
            'action-button',
            'auto-increment',
            'button',
            'chart',
            'data-selector',
            'empty',
            'file-upload',
            'grid',
            'image-upload',
            'item-linker',
            'linked-item',
            'querybuilder',
            'scheduler',
            'sub-entities-grid',
            'timeline'
        )
        AND also_save_seo_value = 1
    GROUP BY `value`)
) AS property
# Static fields first, then order by the 'sort' value.
ORDER BY property.dynamicField, property.sort");
                TemplateQueryStrings.Add("GET_ENTITY_LINK_PROPERTIES", @"SET @_link_type = IF(
    '{link_type}' NOT LIKE '{%}',
    CONVERT('{link_type}', SIGNED),
    -1
);

SELECT
    property.`value`,
    property.linkType,
    # The display name is used in the field editor.
    property.displayName,
    # These fields are just to ensure the properties exist in the Kendo data item.
    '' AS languageCode,
    '' AS aggregation,
    '' AS formatting,
    '' AS fieldAlias,
    'ASC' AS direction,
    # These fields are deprecated and will be removed in the future.
    property.text,
    property.text AS originalText
FROM (
    # Entity link properties.
    (SELECT
        IF(
            # Check if there are more than one properties with the same property name.
            COUNT(*) > 1,
            # If True; Use the property name with the character capitalizted to create the display name.
            CONCAT(UPPER(SUBSTR(property_name, 1, 1)), SUBSTR(property_name, 2)),
            # If False; Use the property's own display name.
            display_name
        ) AS displayName,
        CONCAT_WS(' - ', entity_name, IF(COUNT(*) > 1, CONCAT(UPPER(SUBSTR(property_name, 1, 1)), SUBSTR(property_name, 2)), display_name)) AS text,
        IF(property_name = '', CreateJsonSafeProperty(display_name), property_name) AS `value`,
        link_type AS linkType,
        1 AS dynamicField,
        IF(COUNT(*) > 1, CONCAT(UPPER(SUBSTR(property_name, 1, 1)), SUBSTR(property_name, 2)), display_name) AS sort
    FROM wiser_entityproperty
    WHERE
     	link_type = @_link_type
        # Some entities should be ignored due to their input types.
        AND inputtype NOT IN (
            'action-button',
            'auto-increment',
            'button',
            'chart',
            'data-selector',
            'empty',
            'file-upload',
            'grid',
            'image-upload',
            'item-linker',
            'linked-item',
            'querybuilder',
            'scheduler',
            'sub-entities-grid',
            'timeline'
        )
    GROUP BY `value`)

    UNION

    # SEO variants of the entity properties.
    (SELECT
        CONCAT(
            IF(
                # Check if there are more than one properties with the same property name.
                COUNT(*) > 1,
                # If True; Use the property name with the character capitalizted to create the display name.
                CONCAT(UPPER(SUBSTR(property_name, 1, 1)), SUBSTR(property_name, 2)),
                # If False; Use the property's own display name.
                display_name
            ),
            ' (SEO)'
        ) AS displayName,
        CONCAT(CONCAT_WS(' - ', entity_name, IF(COUNT(*) > 1, CONCAT(UPPER(SUBSTR(property_name, 1, 1)), SUBSTR(property_name, 2)), display_name)), ' (SEO)') AS text,
        CONCAT(IF(property_name = '', CreateJsonSafeProperty(display_name), property_name), '_SEO') AS `value`,
        link_type AS linkType,
        1 AS dynamicField,
        CONCAT(IF(COUNT(*) > 1, CONCAT(UPPER(SUBSTR(property_name, 1, 1)), SUBSTR(property_name, 2)), display_name), ' (SEO)') AS sort
    FROM wiser_entityproperty
    WHERE
     	link_type = @_link_type
        # Some entities should be ignored due to their input types.
        AND inputtype NOT IN (
            'action-button',
            'auto-increment',
            'button',
            'chart',
            'data-selector',
            'empty',
            'file-upload',
            'grid',
            'image-upload',
            'item-linker',
            'linked-item',
            'querybuilder',
            'scheduler',
            'sub-entities-grid',
            'timeline'
        )
        AND also_save_seo_value = 1
    GROUP BY `value`)
) AS property
# Static fields first, then order by the 'sort' value.
ORDER BY property.dynamicField, property.sort");
                TemplateQueryStrings.Add("GET_LINKED_TO_ITEMS", @"#Get all the items for the treeview
SET @_module_id = IF('{module}' LIKE '{%}', '', '{module}');
SET @_parent_id = IF('{id:decrypt(true)}' LIKE '{%}', '0', '{id:decrypt(true)}');

SELECT 
	i.id AS id_encrypt_withdate,
  	i.title AS name,
  	IF(i2.id IS NULL OR i2.moduleid <> @_module_id, 0, 1) AS haschilds,
  	we.icon AS spriteCssClass,
    we.icon AS collapsedSpriteCssClass,
    we.icon_expanded AS expandedSpriteCssClass,
  	ilp.destination_item_id AS destination_item_id_encrypt_withdate,
    0 AS checked
FROM wiser_item i
JOIN wiser_entity we ON we.name = i.entity_type AND we.show_in_tree_view = 1
JOIN wiser_itemlink ilp ON ilp.destination_item_id = @_parent_id AND ilp.item_id = i.id
LEFT JOIN wiser_itemlink ilc ON ilc.destination_item_id = i.id
LEFT JOIN wiser_item i2 ON i2.id = ilc.item_id
WHERE i.moduleid = @_module_id
GROUP BY i.id
ORDER BY ilp.ordering");
                TemplateQueryStrings.Add("GET_DATA_FOR_FIELD_TABLE", @"SET @selected_id = {itemId:decrypt(true)};
SET @entitytype = IF('{entity_type}' LIKE '{%}', '', '{entity_type}');
SET @_moduleId = IF('{moduleId}' LIKE '{%}', '', '{moduleId}');
SET @_linkTypeNumber = IF('{linkTypeNumber}' LIKE '{%}', '', '{linkTypeNumber}');

SELECT
	i.id,
	i.id AS encryptedId_encrypt_withdate,
    CASE i.published_environment
    	WHEN 0 THEN 'onzichtbaar'
        WHEN 1 THEN 'dev'
        WHEN 2 THEN 'test'
        WHEN 3 THEN 'acceptatie'
        WHEN 4 THEN 'live'
    END AS published_environment,
    i.title,
    i.entity_type,
	CreateJsonSafeProperty(LOWER(id.key)) AS property_name,
	IFNULL(idt.`value`, id.`value`) AS property_value,
    il.type AS link_type_number,
    il.id AS link_id,
	il.ordering
FROM wiser_itemlink il
JOIN wiser_item i ON i.id = il.item_id AND (@entitytype = '' OR FIND_IN_SET(i.entity_type, @entitytype)) AND (@_moduleId = '' OR @_moduleId = i.moduleid)

LEFT JOIN wiser_entityproperty p ON p.entity_name = i.entity_type AND p.visible_in_overview = 1
LEFT JOIN wiser_itemdetail id ON id.item_id = il.item_id AND ((p.property_name IS NOT NULL AND p.property_name <> '' AND id.`key` = p.property_name) OR ((p.property_name IS NULL OR p.property_name = '') AND id.`key` = p.display_name))
LEFT JOIN wiser_itemdetail idt ON idt.item_id = il.item_id AND ((p.property_name IS NOT NULL AND p.property_name <> '' AND idt.`key` = CONCAT(p.property_name, '_input')) OR ((p.property_name IS NULL OR p.property_name = '') AND idt.`key` = CONCAT(p.display_name, '_input')))

WHERE il.destination_item_id = @selected_id
AND (@_linkTypeNumber = '' OR il.type = @_linkTypeNumber)
GROUP BY il.item_id, id.id

UNION

SELECT
	i.id,
	i.id AS encryptedId_encrypt_withdate,
    CASE i.published_environment
    	WHEN 0 THEN 'onzichtbaar'
        WHEN 1 THEN 'dev'
        WHEN 2 THEN 'test'
        WHEN 3 THEN 'acceptatie'
        WHEN 4 THEN 'live'
    END AS published_environment,
    i.title,
    i.entity_type,
	CreateJsonSafeProperty(id2.key) AS property_name,
	IFNULL(id2t.`value`, id2.`value`) AS property_value,
    il.type AS link_type_number,
    il.id AS link_id,
	il.ordering
FROM wiser_itemlink il
JOIN wiser_item i ON i.id = il.item_id AND (@entitytype = '' OR FIND_IN_SET(i.entity_type, @entitytype)) AND (@_moduleId = '' OR @_moduleId = i.moduleid)

LEFT JOIN wiser_entityproperty p2 ON p2.link_type = il.type AND p2.visible_in_overview = 1
LEFT JOIN wiser_itemlinkdetail id2 ON id2.itemlink_id = il.id AND ((p2.property_name IS NOT NULL AND p2.property_name <> '' AND id2.`key` = p2.property_name) OR ((p2.property_name IS NULL OR p2.property_name = '') AND id2.`key` = p2.display_name))
LEFT JOIN wiser_itemlinkdetail id2t ON id2t.itemlink_id = il.id AND ((p2.property_name IS NOT NULL AND p2.property_name <> '' AND id2t.`key` = CONCAT(p2.property_name, '_input')) OR ((p2.property_name IS NULL OR p2.property_name = '') AND id2t.`key` = CONCAT(p2.display_name, '_input')))

WHERE il.destination_item_id = @selected_id
AND (@_linkTypeNumber = '' OR il.type = @_linkTypeNumber)
GROUP BY il.item_id, id2.id

ORDER BY ordering, title");

                TemplateQueryStrings.Add("GET_WISER_LINK_LIST", @"SELECT *,
CONCAT(`name`, ' --> #', type, ' connected entity: ""', connected_entity_type ,'"" destination entity: ""', destination_entity_type, '""')AS formattedName
FROM `wiser_link`
ORDER BY type");
            }
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TemplateEntityModel>> GetTemplateByNameAsync(string templateName, bool wiserTemplate = false)
        {
            var connectionToUse = clientDatabaseConnection;

            if (wiserTemplate)
            {
                connectionToUse = wiserDatabaseConnection;
            }

            if (connectionToUse == null)
            {
                return new ServiceResult<TemplateEntityModel>(new TemplateEntityModel());
            }

            await connectionToUse.EnsureOpenConnectionForReadingAsync();
            connectionToUse.ClearParameters();
            connectionToUse.AddParameter("templateName", templateName);

            var query = @"
SELECT
    item.id AS id,
    item.title AS `name`,
    `subject`.`value` AS `subject`,
    IF(template.long_value IS NULL OR template.long_value = '', template.`value`, template.long_value) AS content
FROM wiser_item AS item
JOIN wiser_itemdetail AS template ON template.item_id = item.id AND template.`key` = 'template'
JOIN wiser_itemdetail AS `subject` ON `subject`.item_id = item.id AND `subject`.`key` = 'subject'
WHERE item.entity_type = 'template'
AND item.title = ?templateName
LIMIT 1";

            var dataTable = await connectionToUse.GetAsync(query);

            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<TemplateEntityModel>(new TemplateEntityModel());
            }

            var result = new TemplateEntityModel()
            {
                Id = dataTable.Rows[0].Field<ulong>("id"),
                Name = dataTable.Rows[0].Field<string>("name"),
                Subject = dataTable.Rows[0].Field<string>("subject"),
                Content = dataTable.Rows[0].Field<string>("content")
            };

            return new ServiceResult<TemplateEntityModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<PublishedEnvironmentModel>> GetTemplateEnvironmentsAsync(int templateId, string branchDatabaseName = null)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }

            var versionsAndPublished = await templateDataService.GetPublishedEnvironmentsAsync(templateId, branchDatabaseName);

            return new ServiceResult<PublishedEnvironmentModel>(PublishedEnvironmentHelper.CreatePublishedEnvironmentsFromVersionDictionary(versionsAndPublished));
        }

        /// <inheritdoc />
        public async Task<ServiceResult<LinkedTemplatesModel>> GetLinkedTemplatesAsync(int templateId)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }

            var rawLinkList = await templateDataService.GetLinkedTemplatesAsync(templateId);

            var resultLinks = new LinkedTemplatesModel();
            foreach (var linkedTemplate in rawLinkList)
            {
                switch (linkedTemplate.LinkType)
                {
                    case TemplateTypes.Js:
                        resultLinks.LinkedJavascript.Add(linkedTemplate);
                        break;
                    case TemplateTypes.Css:
                        resultLinks.LinkedScssTemplates.Add(linkedTemplate);
                        break;
                    case TemplateTypes.Scss:
                        resultLinks.LinkedScssTemplates.Add(linkedTemplate);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(linkedTemplate.LinkType), linkedTemplate.LinkType.ToString());
                }
            }

            resultLinks.LinkOptionsTemplates = await templateDataService.GetTemplatesAvailableForLinkingAsync(templateId);
            resultLinks.LinkOptionsTemplates = resultLinks.LinkOptionsTemplates.Where(t => rawLinkList.All(l => l.TemplateId != t.TemplateId)).ToList();

            return new ServiceResult<LinkedTemplatesModel>(resultLinks);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<DynamicContentOverviewModel>>> GetLinkedDynamicContentAsync(int templateId)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }

            var resultList = new List<DynamicContentOverviewModel>();

            foreach (var linkedContent in await templateDataService.GetLinkedDynamicContentAsync(templateId))
            {
                resultList.Add(LinkedDynamicContentToDynamicContentOverview(linkedContent));
            }

            return new ServiceResult<List<DynamicContentOverviewModel>>(resultList);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TemplateSettingsModel>> GetTemplateMetaDataAsync(int templateId)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }

            var templateData = await templateDataService.GetMetaDataAsync(templateId);
            var templateEnvironmentsResult = await GetTemplateEnvironmentsAsync(templateId);
            if (templateEnvironmentsResult.StatusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<TemplateSettingsModel>
                {
                    ErrorMessage = templateEnvironmentsResult.ErrorMessage,
                    StatusCode = templateEnvironmentsResult.StatusCode
                };
            }

            templateData.PublishedEnvironments = templateEnvironmentsResult.ModelObject;
            return new ServiceResult<TemplateSettingsModel>(templateData);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TemplateSettingsModel>> GetTemplateSettingsAsync(ClaimsIdentity identity, int templateId, Environments? environment = null)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }

            var templateData = await templateDataService.GetDataAsync(templateId, environment);
            var templateEnvironmentsResult = await GetTemplateEnvironmentsAsync(templateId);
            if (templateEnvironmentsResult.StatusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<TemplateSettingsModel>
                {
                    ErrorMessage = templateEnvironmentsResult.ErrorMessage,
                    StatusCode = templateEnvironmentsResult.StatusCode
                };
            }

            templateData.PublishedEnvironments = templateEnvironmentsResult.ModelObject;
            var encryptionKey = (await wiserTenantsService.GetEncryptionKey(identity, true)).ModelObject;
            templateDataService.DecryptEditorValueIfEncrypted(encryptionKey, templateData);

            return new ServiceResult<TemplateSettingsModel>(templateData);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TemplateWtsConfigurationModel>> GetTemplateWtsConfigurationAsync(ClaimsIdentity identity, int templateId, Environments? environment = null)
        {
            var templateSettings = await GetTemplateSettingsAsync(identity, templateId, environment);
            if (templateSettings.StatusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<TemplateWtsConfigurationModel>
                {
                    ErrorMessage = templateSettings.ErrorMessage,
                    StatusCode = templateSettings.StatusCode
                };
            }

            if (templateSettings.ModelObject.Type != TemplateTypes.Xml)
            {
                return new ServiceResult<TemplateWtsConfigurationModel>(new TemplateWtsConfigurationModel());
            }

            // Parse the xml
            var templateXml = wtsConfigurationService.ParseXmlToObject(templateSettings.ModelObject.EditorValue);

            return new ServiceResult<TemplateWtsConfigurationModel>(templateXml);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<int>> PublishToEnvironmentAsync(ClaimsIdentity identity, int templateId, int version, Environments environment, PublishedEnvironmentModel currentPublished, string branchDatabaseName = null)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id is invalid");
            }

            if (version <= 0)
            {
                throw new ArgumentException("The version is invalid");
            }

            // When deploying a template to live environment that is a view, routine or trigger, then also update the actual view/routine/trigger in the database.
            if (environment == Environments.Live)
            {
                var databaseName = branchDatabaseName ?? clientDatabaseConnection.ConnectedDatabase;
                var template = await templateDataService.GetDataAsync(templateId, version: version);

                switch (template.Type)
                {
                    case TemplateTypes.View:
                    {
                        // Create or replace view.
                        var (successful, errorMessage) = await CreateOrReplaceDatabaseViewAsync(template.Name, template.EditorValue, databaseName);
                        if (!successful)
                        {
                            throw new Exception($"The template saved successfully, but the view could not be created due to a syntax error. Error:\n{errorMessage}");
                        }

                        break;
                    }
                    case TemplateTypes.Routine:
                    {
                        // Also (re-)create the actual routine.
                        var (successful, errorMessage) = await CreateOrReplaceDatabaseRoutineAsync(template.Name, template.RoutineType, template.RoutineParameters, template.RoutineReturnType, template.EditorValue, databaseName);
                        if (!successful)
                        {
                            throw new Exception($"The template saved successfully, but the routine could not be created due to a syntax error. Error:\n{errorMessage}");
                        }

                        break;
                    }
                    case TemplateTypes.Trigger:
                    {
                        // Also (re-)create the actual trigger.
                        var (successful, errorMessage) = await CreateOrReplaceDatabaseTriggerAsync(template.Name, template.TriggerTiming, template.TriggerEvent, template.TriggerTableName, template.EditorValue, databaseName);
                        if (!successful)
                        {
                            throw new Exception($"The template saved successfully, but the trigger could not be created due to a syntax error. Error:\n{errorMessage}");
                        }

                        break;
                    }
                }
            }

            // Create a new version of the template, so that any changes made after this will be done in the new version instead of the published one.
            // Does not apply if the template was published to live within a branch.
            if (String.IsNullOrWhiteSpace(branchDatabaseName))
            {
                await CreateNewVersionAsync(templateId, version);
            }

            var newPublished = PublishedEnvironmentHelper.CalculateEnvironmentsToPublish(currentPublished, version, environment);

            var publishLog = PublishedEnvironmentHelper.GeneratePublishLog(templateId, currentPublished, newPublished);

            return new ServiceResult<int>(await templateDataService.UpdatePublishedEnvironmentAsync(templateId, version, environment, publishLog, IdentityHelpers.GetUserName(identity, true), branchDatabaseName));
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> SaveAsync(ClaimsIdentity identity, int templateId, TemplateWtsConfigurationModel configuration)
        {
            // Convert the configuration object to raw XML
            var updatedEditorValue = wtsConfigurationService.ParseObjectToXml(configuration);

            // Get the latest version of the template
            var latestVersion = await GetTemplateSettingsAsync(identity, templateId);

            // Convert the latest version to a model we can work with
            var latestVersionModel = latestVersion.ModelObject;

            // Update the model with the new configuration
            latestVersionModel.EditorValue = updatedEditorValue;

            return await SaveAsync(identity, latestVersionModel);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> SaveAsync(ClaimsIdentity identity, TemplateSettingsModel template, bool skipCompilation = false)
        {
            if (template == null)
            {
                throw new ArgumentException("TemplateData cannot be empty.");
            }

            // Compile / minify (S)CSS, javascript and HTML.
            switch (template.Type)
            {
                case TemplateTypes.Css:
                    if (template.DisableMinifier || String.IsNullOrWhiteSpace(template.EditorValue))
                    {
                        break;
                    }

                    template.MinifiedValue = Uglify.Css(template.EditorValue).Code;

                    break;
                case TemplateTypes.Scss:
                    // If the current template is a base SCSS include, it is meant to be only used for compiling other SCSS templates.
                    // Therefor we make the minified value empty and don't compile it by itself.
                    if (template.IsScssIncludeTemplate || skipCompilation)
                    {
                        break;
                    }

                    var stringBuilder = await templateDataService.GetScssIncludesForScssTemplateAsync(template.TemplateId);

                    // Add the current template value last, so it will have access to everything from the base templates.
                    stringBuilder.AppendLine(template.EditorValue);

                    // Compile to CSS and minify.
                    var compileResult = SassCompiler.Compile(stringBuilder.ToString());
                    template.MinifiedValue = Uglify.Css(compileResult.CompiledContent).Code;

                    break;
                case TemplateTypes.Js:
                    if (template.DisableMinifier || String.IsNullOrWhiteSpace(template.EditorValue))
                    {
                        break;
                    }

                    var codeSettings = new CodeSettings
                    {
                        EvalTreatment = EvalTreatment.Ignore
                    };

                    // Try to the minify the script.
                    var terserSuccessful = false;
                    string terserMinifiedScript = null;
                    if (apiSettings.UseTerserForTemplateScriptMinification)
                    {
                        // Minification through terser is enabled, attempt to minify it using that.
                        (terserSuccessful, terserMinifiedScript) = await MinifyJavaScriptWithTerserAsync(template.EditorValue);
                    }

                    if (terserSuccessful)
                    {
                        template.MinifiedValue = terserMinifiedScript;
                    }
                    else
                    {
                        // If minification through terser failed somehow (like a missing/outdated/corrupt installation), then
                        // use NUglify as a fallback.
                        try
                        {
                            // Wrapped in a try-catch statement, because NUglify is known to have various
                            // issues with minifying newer JavaScript features.
                            template.MinifiedValue = Uglify.Js(template.EditorValue, codeSettings).Code + ";";
                        }
                        catch (Exception exception)
                        {
                            // Use non-minified editor value as the minified value so the changes don't go lost.
                            template.MinifiedValue = template.EditorValue;
                            logger.LogWarning(exception, $"An error occurred while trying to minify the JavaScript using NUglify of template ID {template.TemplateId}");
                        }
                    }

                    break;
                case TemplateTypes.Html:
                    template.EditorValue = await wiserItemsService.ReplaceHtmlForSavingAsync(template.EditorValue);
                    template.MinifiedValue = template.EditorValue;
                    break;
                case TemplateTypes.Xml:
                    var trimmedValue = template.EditorValue.Trim();
                    if (!trimmedValue.StartsWith("<Configuration>", StringComparison.OrdinalIgnoreCase) && !trimmedValue.StartsWith("<OAuthConfiguration>", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    var encryptionKey = (await wiserTenantsService.GetEncryptionKey(identity, true)).ModelObject;
                    template.EditorValue = trimmedValue.EncryptWithAes(encryptionKey, useSlowerButMoreSecureMethod: true);

                    break;
            }

            var jsLinks = template.LinkedTemplates?.LinkedJavascript?.Select(x => x.TemplateId).ToList();
            var scssLinks = template.LinkedTemplates?.LinkedScssTemplates?.Select(x => x.TemplateId).ToList();
            var allLinkedTemplates = new List<int>(jsLinks ?? new List<int>());
            if (scssLinks != null)
            {
                allLinkedTemplates.AddRange(scssLinks);
            }

            var templateLinks = String.Join(",", allLinkedTemplates);
            if (templateLinks == "" && !String.IsNullOrWhiteSpace(template.LinkedTemplates?.RawLinkList))
            {
                templateLinks = template.LinkedTemplates.RawLinkList;
            }

            await templateDataService.SaveAsync(template, templateLinks, IdentityHelpers.GetUserName(identity, true));

            if (template.Type != TemplateTypes.Scss || !template.IsScssIncludeTemplate || skipCompilation)
            {
                return new ServiceResult<bool>(true);
            }

            // Get all SCSS templates that might use the current template and re-compile them, to make sure they will include the latest version of this template.
            var templates = await templateDataService.GetScssTemplatesThatAreNotIncludesAsync(template.TemplateId);
            foreach (var otherTemplate in templates)
            {
                await SaveAsync(identity, otherTemplate);
            }

            return new ServiceResult<bool>(true);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<int>> CreateNewVersionAsync(int templateId, int versionBeingDeployed = 0)
        {
            // ReSharper disable once InvertIf
            if (versionBeingDeployed > 0)
            {
                var latestVersion = await templateDataService.GetLatestVersionAsync(templateId);
                if (versionBeingDeployed != latestVersion.Version || latestVersion.Removed)
                {
                    return new ServiceResult<int>(0);
                }
            }

            return new ServiceResult<int>(await templateDataService.CreateNewVersionAsync(templateId));
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<TemplateTreeViewModel>>> GetTreeViewSectionAsync(ClaimsIdentity identity, int parentId)
        {
            // Make sure the tables are up-to-date.
            await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string>
            {
                WiserTableNames.WiserTemplate,
                WiserTableNames.WiserDynamicContent,
                WiserTableNames.WiserTemplateDynamicContent,
                WiserTableNames.WiserTemplatePublishLog,
                WiserTableNames.WiserPreviewProfiles,
                WiserTableNames.WiserDynamicContentPublishLog,
                WiserTableNames.WiserTemplateRenderLog,
                WiserTableNames.WiserDynamicContentRenderLog
            });

            // Make sure the ordering is correct.
            await templateDataService.FixTreeViewOrderingAsync(parentId);

            // Do any table updates that might be needed.
            await templateDataService.KeepTablesUpToDateAsync();
            await dynamicContentDataService.KeepTablesUpToDateAsync();

            // Get templates in correct order.
            var rawSection = await templateDataService.GetTreeViewSectionAsync(parentId);

            if (rawSection.Count > 0 && rawSection[0].TemplateType == TemplateTypes.Routine)
            {
                // Routine template names should be updated to the routine's actual name if the template's name doesn't contain
                // the "WISER_" prefix, but the routine does. This is meant for tenants that upgrade from Wiser 1/2 to Wiser 3.
                foreach (var treeViewItem in rawSection.Where(treeViewItem => !treeViewItem.IsVirtualItem))
                {
                    clientDatabaseConnection.AddParameter("routineName", treeViewItem.TemplateName);
                    var routineData = await clientDatabaseConnection.GetAsync(@"
                        SELECT ROUTINE_NAME
                        FROM information_schema.ROUTINES
                        WHERE ROUTINE_NAME = ?routineName");

                    if (routineData.Rows.Count == 0) continue;

                    clientDatabaseConnection.AddParameter("routineName", $"WISER_{treeViewItem.TemplateName}");
                    routineData = await clientDatabaseConnection.GetAsync(@"
                        SELECT ROUTINE_NAME
                        FROM information_schema.ROUTINES
                        WHERE ROUTINE_NAME = ?routineName");

                    if (routineData.Rows.Count == 0) continue;

                    // Routine doesn't exist with the template's name, but DOES exist with the "WISER_" prefix.
                    // Update the template's name to stay consistent.
                    var newTemplateName = routineData.Rows[0].Field<string>("ROUTINE_NAME");
                    await RenameAsync(identity, treeViewItem.TemplateId, newTemplateName);

                    treeViewItem.TemplateName = newTemplateName;
                }
            }

            var convertedList = rawSection.Select(TreeViewHelper.ConvertTemplateTreeViewDaoToTemplateTreeViewModel).ToList();

            return new ServiceResult<List<TemplateTreeViewModel>>(convertedList);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<SearchResultModel>>> SearchAsync(ClaimsIdentity identity, string searchValue)
        {
            var encryptionKey = (await wiserTenantsService.GetEncryptionKey(identity, true)).ModelObject;
            return new ServiceResult<List<SearchResultModel>>(await templateDataService.SearchAsync(searchValue, encryptionKey));
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TemplateHistoryOverviewModel>> GetTemplateHistoryAsync(ClaimsIdentity identity, int templateId, int pageNumber, int itemsPerPage)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }

            var dynamicContentOverview = await GetLinkedDynamicContentAsync(templateId);
            if (dynamicContentOverview.StatusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<TemplateHistoryOverviewModel>
                {
                    StatusCode = dynamicContentOverview.StatusCode,
                    ErrorMessage = dynamicContentOverview.ErrorMessage
                };
            }

            var dynamicContentHistory = new Dictionary<DynamicContentOverviewModel, List<HistoryVersionModel>>();
            foreach (var dc in dynamicContentOverview.ModelObject)
            {
                dynamicContentHistory.Add(dc, (await historyService.GetChangesInComponentAsync(dc.Id, pageNumber, itemsPerPage)).ModelObject);
            }

            var overview = new TemplateHistoryOverviewModel
            {
                TemplateId = templateId,
                TemplateHistory = await historyService.GetVersionHistoryFromTemplate(identity, templateId, dynamicContentHistory, pageNumber, itemsPerPage),
                PublishHistory = await historyService.GetPublishHistoryFromTemplate(templateId, pageNumber, itemsPerPage),
                PublishedEnvironment = (await GetTemplateEnvironmentsAsync(templateId)).ModelObject
            };

            return new ServiceResult<TemplateHistoryOverviewModel>(overview);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TemplateTreeViewModel>> CreateAsync(ClaimsIdentity identity, string name, int parent, TemplateTypes type, string editorValue = "")
        {
            var newId = await templateDataService.CreateAsync(name, parent, type, IdentityHelpers.GetUserName(identity, true), editorValue);
            return new ServiceResult<TemplateTreeViewModel>(new TemplateTreeViewModel
            {
                TemplateId = newId,
                TemplateName = name,
                HasChildren = false,
                IsFolder = type == TemplateTypes.Directory
            });
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> RenameAsync(ClaimsIdentity identity, int id, string newName)
        {
            if (String.IsNullOrWhiteSpace(newName))
            {
                return new ServiceResult<bool>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "Name cannot be empty."
                };
            }

            var templateDataResponse = await GetTemplateSettingsAsync(identity, id);
            if (templateDataResponse.StatusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<bool>
                {
                    StatusCode = templateDataResponse.StatusCode,
                    ErrorMessage = templateDataResponse.ErrorMessage
                };
            }

            var linkedTemplatesResponse = await GetLinkedTemplatesAsync(id);
            if (linkedTemplatesResponse.StatusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<bool>
                {
                    StatusCode = linkedTemplatesResponse.StatusCode,
                    ErrorMessage = linkedTemplatesResponse.ErrorMessage
                };
            }

            var oldName = templateDataResponse.ModelObject.Name;
            templateDataResponse.ModelObject.LinkedTemplates = linkedTemplatesResponse.ModelObject;
            templateDataResponse.ModelObject.Name = newName;

            if (templateDataResponse.ModelObject.Type is not (TemplateTypes.View or TemplateTypes.Routine or TemplateTypes.Trigger))
            {
                return await SaveAsync(identity, templateDataResponse.ModelObject);
            }

            // Also rename the view, routine, or trigger that this template is managing.
            switch (templateDataResponse.ModelObject.Type)
            {
                case TemplateTypes.View:
                    await CreateOrReplaceDatabaseViewAsync(newName, templateDataResponse.ModelObject.EditorValue, oldName, clientDatabaseConnection.ConnectedDatabase);
                    break;
                case TemplateTypes.Routine:
                    await CreateOrReplaceDatabaseRoutineAsync(newName, templateDataResponse.ModelObject.RoutineType, templateDataResponse.ModelObject.RoutineParameters, templateDataResponse.ModelObject.RoutineReturnType, templateDataResponse.ModelObject.EditorValue, oldName, clientDatabaseConnection.ConnectedDatabase);
                    break;
                case TemplateTypes.Trigger:
                    await CreateOrReplaceDatabaseTriggerAsync(newName, templateDataResponse.ModelObject.TriggerTiming, templateDataResponse.ModelObject.TriggerEvent, templateDataResponse.ModelObject.TriggerTableName, templateDataResponse.ModelObject.EditorValue, oldName, clientDatabaseConnection.ConnectedDatabase);
                    break;
            }

            return await SaveAsync(identity, templateDataResponse.ModelObject);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> MoveAsync(ClaimsIdentity identity, int sourceId, int destinationId, TreeViewDropPositions dropPosition)
        {
            // If the position is "over", it means the destinationId itself will become the new parent, for "before" and "after" it means the parent of the destinationId will become the new parent.
            var destinationParentId = dropPosition == TreeViewDropPositions.Over ? destinationId : (await templateDataService.GetParentAsync(destinationId) ?? new TemplateSettingsModel()).TemplateId;
            var sourceParentId = (await templateDataService.GetParentAsync(sourceId) ?? new TemplateSettingsModel()).TemplateId;
            var oldOrderNumber = await templateDataService.GetOrderingAsync(sourceId);
            var newOrderNumber = dropPosition switch
            {
                TreeViewDropPositions.Over => await templateDataService.GetHighestOrderNumberOfChildrenAsync(destinationParentId),
                TreeViewDropPositions.Before => await templateDataService.GetOrderingAsync(destinationId),
                TreeViewDropPositions.After => await templateDataService.GetOrderingAsync(destinationId) + 1,
                _ => throw new ArgumentOutOfRangeException(nameof(dropPosition), dropPosition, null)
            };

            await templateDataService.MoveAsync(sourceId, destinationId, sourceParentId, destinationParentId, oldOrderNumber, newOrderNumber, dropPosition, IdentityHelpers.GetUserName(identity, true));
            return new ServiceResult<bool>(true);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<TemplateTreeViewModel>>> GetEntireTreeViewStructureAsync(ClaimsIdentity identity, int parentId, string startFrom, Environments? environment = null)
        {
            var templates = new List<TemplateTreeViewModel>();
            var path = startFrom.Split(',');
            var remainingStartFrom = startFrom[(path[0].Length + (path.Length > 1 ? 1 : 0))..];

            var templateTrees = (await GetTreeViewSectionAsync(identity, parentId)).ModelObject;
            foreach (var templateTree in templateTrees)
            {
                if (!String.IsNullOrWhiteSpace(startFrom) && !path[0].Equals(templateTree.TemplateName, StringComparison.InvariantCultureIgnoreCase)) continue;

                if (templateTree.HasChildren)
                {
                    templateTree.ChildNodes = (await GetEntireTreeViewStructureAsync(identity, templateTree.TemplateId, remainingStartFrom, environment)).ModelObject;
                }
                else
                {
                    templateTree.TemplateSettings = (await GetTemplateSettingsAsync(identity, templateTree.TemplateId, environment)).ModelObject;
                }

                if (String.IsNullOrWhiteSpace(startFrom))
                {
                    templates.Add(templateTree);
                }
                else
                {
                    templates = templateTree.ChildNodes;
                }
            }

            return new ServiceResult<List<TemplateTreeViewModel>>(templates);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> DeleteAsync(ClaimsIdentity identity, int templateId, bool alsoDeleteChildren = true)
        {
            var result = await templateDataService.DeleteAsync(templateId, IdentityHelpers.GetUserName(identity, true), alsoDeleteChildren);
            return new ServiceResult<bool>
            {
                ModelObject = result,
                StatusCode = !result ? HttpStatusCode.NotFound : HttpStatusCode.OK,
                ErrorMessage = !result ? $"Template with ID '{templateId}' not found." : null
            };
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> CheckDefaultHeaderConflict(int templateId, string regexString)
        {
            return await InternalCheckDefaultHeaderOrFooterConflict("header", templateId, regexString);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> CheckDefaultFooterConflict(int templateId, string regexString)
        {
            return await InternalCheckDefaultHeaderOrFooterConflict("footer", templateId, regexString);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TemplateSettingsModel>> GetVirtualTemplateAsync(string objectName, TemplateTypes templateType)
        {
            if (String.IsNullOrWhiteSpace(objectName))
            {
                throw new ArgumentException("The name cannot be null or empty.");
            }

            if (!templateType.InList(TemplateTypes.View, TemplateTypes.Routine, TemplateTypes.Trigger))
            {
                throw new ArgumentException("Template type has to be either View, Routine, or Trigger.");
            }

            TemplateSettingsModel virtualTemplateData;
            switch (templateType)
            {
                case TemplateTypes.View:
                    virtualTemplateData = await GetDatabaseViewDataAsync(objectName);
                    break;
                case TemplateTypes.Routine:
                    virtualTemplateData = await GetDatabaseRoutineDataAsync(objectName);
                    break;
                case TemplateTypes.Trigger:
                    virtualTemplateData = await GetDatabaseTriggerDataAsync(objectName);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Set the rest of the data.
            virtualTemplateData.Type = templateType;
            virtualTemplateData.ChangedBy = "Database";
            virtualTemplateData.PublishedEnvironments = new PublishedEnvironmentModel
            {
                LiveVersion = 0,
                AcceptVersion = 0,
                TestVersion = 0,
                VersionList = new List<int>(0)
            };

            return new ServiceResult<TemplateSettingsModel>(virtualTemplateData);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<IList<string>>> GetTableNamesForTriggerTemplatesAsync()
        {
            return new ServiceResult<IList<string>>(await databaseHelpersService.GetAllTableNamesAsync());
        }

        /// <summary>
        /// The function used by <see cref="CheckDefaultHeaderConflict"/> and <see cref="CheckDefaultFooterConflict"/>.
        /// </summary>
        /// <param name="type">The type to check. It should be either 'header' or 'footer'.</param>
        /// <param name="templateId">ID of the current template.</param>
        /// <param name="regexString">The regex to be used in the check.</param>
        /// <returns>A string with the name of the template that this template conflicts with, or an empty string if there's no conflict.</returns>
        /// <exception cref="ArgumentOutOfRangeException">When <paramref name="type"/> is <see langword="null"/> or does not equal to either "header" or "footer".</exception>
        private async Task<ServiceResult<string>> InternalCheckDefaultHeaderOrFooterConflict(string type, int templateId, string regexString)
        {
            // Validate the type. It can only be either "header" or "footer".
            if (String.IsNullOrWhiteSpace(type) || !type.InList("header", "footer"))
            {
                throw new ArgumentOutOfRangeException(nameof(type), $"Argument '{nameof(type)}' has an invalid value. Should be either \"header\" or \"footer\".");
            }

            // Create the name of the field that needs to be checked.
            var fieldName = $"is_default_{type}";

            // Add template ID parameter. This is used by the query to make sure templates don't conflict with themselves.
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("templateId", templateId);

            string regexWherePart;
            if (String.IsNullOrWhiteSpace(regexString))
            {
                // If regexString is null or empty, the field in the table should also be null or empty.
                regexWherePart = " AND (template.default_header_footer_regex IS NULL OR TRIM(template.default_header_footer_regex) = '')";
            }
            else
            {
                // If regexString is set to a non-null and non-empty string, the string needs to be an exact match.
                clientDatabaseConnection.AddParameter("regexString", regexString);
                regexWherePart = " AND template.default_header_footer_regex = ?regexString";
            }

            var query = $@"
                SELECT template.template_name
                FROM {WiserTableNames.WiserTemplate} AS template
                JOIN (SELECT template_id, MAX(version) AS maxVersion FROM {WiserTableNames.WiserTemplate} GROUP BY template_id) AS maxVersion ON template.template_id = maxVersion.template_id AND template.version = maxVersion.maxVersion
                WHERE template.template_type = {(int)TemplateTypes.Html} AND template.removed = 0 AND template.`{fieldName}` = 1 AND template.template_id <> ?templateId {regexWherePart}
                GROUP BY template.template_id
                LIMIT 1";

            var result = await clientDatabaseConnection.GetAsync(query);
            return result.Rows.Count == 0
                ? new ServiceResult<string>(String.Empty)
                : new ServiceResult<string>(result.Rows[0].Field<string>("template_name"));
        }


        /// <inheritdoc />
        public async Task<ServiceResult<bool>> ConvertLegacyTemplatesToNewTemplatesAsync(ClaimsIdentity identity)
        {
            // Make sure the tables are up-to-date.
            await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string>
            {
                WiserTableNames.WiserTemplate,
                WiserTableNames.WiserDynamicContent,
                WiserTableNames.WiserTemplateDynamicContent,
                WiserTableNames.WiserTemplatePublishLog,
                WiserTableNames.WiserPreviewProfiles,
                WiserTableNames.WiserDynamicContentPublishLog
            });

            // Check if the tables are actually empty, otherwise we can't be sure that the conversion will be done correctly.
            var dataTable = await clientDatabaseConnection.GetAsync($"SELECT COUNT(*) FROM {WiserTableNames.WiserTemplate}");
            if (Convert.ToInt32(dataTable.Rows[0][0]) > 0)
            {
                return new ServiceResult<bool>
                {
                    ErrorMessage = $"The table {WiserTableNames.WiserTemplate} is not empty. It should be empty before attempting this, to prevent errors and duplicate templates.",
                    StatusCode = HttpStatusCode.Conflict
                };
            }

            dataTable = await clientDatabaseConnection.GetAsync($"SELECT COUNT(*) FROM {WiserTableNames.WiserDynamicContent}");
            if (Convert.ToInt32(dataTable.Rows[0][0]) > 0)
            {
                return new ServiceResult<bool>
                {
                    ErrorMessage = $"The table {WiserTableNames.WiserDynamicContent} is not empty. It should be empty before attempting this, to prevent errors and duplicate templates.",
                    StatusCode = HttpStatusCode.Conflict
                };
            }

            // Check if the legacy tables actually exist in the database.
            if (!await databaseHelpersService.TableExistsAsync("easy_items") || !await databaseHelpersService.TableExistsAsync("easy_templates") || !await databaseHelpersService.TableExistsAsync("easy_dynamiccontent"))
            {
                return new ServiceResult<bool>
                {
                    ErrorMessage = "One or more of the tables 'easy_items', 'easy_templates' and 'easy_dynamiccontent' don't exist, so we have nothing to convert.",
                    StatusCode = HttpStatusCode.Conflict
                };
            }

            // Copy easy_templates to wiser_template, but only the versions that are actually still used, we don't need the entire history.
            var query = @"
SELECT
    IF(item.parent_id <= 0, NULL, item.parent_id) AS parent_id,
    item.name AS template_name,
    IFNULL(template.html, template.template) AS template_data,
    template.html_minified AS template_data_minified,
    CONCAT_WS('/', '', parent9.name, parent8.name, parent7.name, parent6.name, parent5.name, parent4.name, parent3.name, parent2.name, parent1.name, '') AS path,
    IFNULL(template.version, 1) AS version,
    item.id AS template_id,
    IFNULL(item.lastchangedate, item.createdon) AS changed_on,
    IFNULL(item.lastchangedby, item.createdby) AS changed_by,
    IF(template.istest = 1, 2, 0) + IF(template.isacceptance = 1, 4, 0) + IF(template.islive = 1, 8, 0) AS published_environment,
    template.usecache AS use_cache,
    template.issecure AS login_required,
    template.securedsessionprefix AS login_session_prefix,
    CONCAT_WS(',', template.jstemplates, template.csstemplates) AS linked_templates,
    item.volgnr AS ordering,
    template.pagemode AS insert_mode,
    template.loadalways AS load_always,
    template.urlregex AS url_regex,
    template.externalfiles AS external_files,
    template.groupingCreateObjectInsteadOfArray AS grouping_create_object_instead_of_array,
    template.groupingprefix AS grouping_prefix,
    template.groupingkey AS grouping_key,
    template.groupingKeyColumnName AS grouping_key_column_name,
    template.groupingValueColumnName AS grouping_value_column_name,
    template.disableminifier AS disable_minifier,
    template.isscssincludetemplate AS is_scss_include_template,
    template.useinwiserhtmleditors AS use_in_wiser_html_editors,
    template.defaulttemplate AS wiser_cdn_templates,
    template.templatetype AS type,
    template.variables AS routine_parameters,
    item.ismap AS is_directory
FROM easy_items AS item
LEFT JOIN easy_templates AS template ON template.itemid = item.id
LEFT JOIN easy_items AS parent1 ON parent1.id = item.parent_id
LEFT JOIN easy_items AS parent2 ON parent2.id = parent1.parent_id
LEFT JOIN easy_items AS parent3 ON parent3.id = parent2.parent_id
LEFT JOIN easy_items AS parent4 ON parent4.id = parent3.parent_id
LEFT JOIN easy_items AS parent5 ON parent5.id = parent4.parent_id
LEFT JOIN easy_items AS parent6 ON parent6.id = parent5.parent_id
LEFT JOIN easy_items AS parent7 ON parent7.id = parent6.parent_id
LEFT JOIN easy_items AS parent8 ON parent8.id = parent7.parent_id
LEFT JOIN easy_items AS parent9 ON parent9.id = parent8.parent_id
JOIN (
    SELECT item.id, MIN(deployedVersion.version) AS version
    FROM easy_items AS item
    LEFT JOIN easy_templates AS template ON template.itemid = item.id
    LEFT JOIN easy_templates AS deployedVersion ON deployedVersion.itemid = template.itemid AND 1 IN (deployedVersion.istest, deployedVersion.isacceptance, deployedVersion.islive)
    WHERE item.moduleid = 143
    AND item.deleted = 0
    AND item.published = 1
    GROUP BY item.id
) AS lowestVersionToConvert ON lowestVersionToConvert.id = item.id AND (template.id IS NULL OR template.version >= lowestVersionToConvert.version)
WHERE template.templatetype IS NULL OR template.templatetype <> 'normal'";

            await clientDatabaseConnection.BeginTransactionAsync();
            try
            {
                dataTable = await clientDatabaseConnection.GetAsync(query);

                var allRootDirectories = new List<string> { "HTML", "JS", "SCSS", "CSS", "SQL", "SERVICES", "VIEWS", "ROUTINES", "TRIGGERS" };
                var rootDirectoriesCreated = new List<string>();
                foreach (DataRow dataRow in dataTable.Rows)
                {
                    // Get template type.
                    var templateType = TemplateTypes.Directory;
                    if (!Convert.ToBoolean(dataRow["is_directory"]))
                    {
                        var path = dataRow.Field<string>("path") ?? "";

                        if (path.Contains("/html/", StringComparison.OrdinalIgnoreCase))
                        {
                            templateType = TemplateTypes.Html;
                        }
                        else if (path.Contains("/css/", StringComparison.OrdinalIgnoreCase))
                        {
                            templateType = TemplateTypes.Css;
                        }
                        else if (path.Contains("/scss/", StringComparison.OrdinalIgnoreCase))
                        {
                            templateType = TemplateTypes.Scss;
                        }
                        else if (path.Contains("/scripts/", StringComparison.OrdinalIgnoreCase))
                        {
                            templateType = TemplateTypes.Js;
                        }
                        else if (path.Contains("/query/", StringComparison.OrdinalIgnoreCase))
                        {
                            templateType = TemplateTypes.Query;
                        }
                        else if (path.Contains("/ais/", StringComparison.OrdinalIgnoreCase))
                        {
                            templateType = TemplateTypes.Xml;
                        }
                        else if (path.Contains("/routines/", StringComparison.OrdinalIgnoreCase))
                        {
                            templateType = TemplateTypes.Routine;
                        }
                    }

                    // Combine the wiser CDN files with the external files, because we don't use Wiser CDN anymore in Wiser 3.
                    var cdnDirectory = dataRow.Field<string>("type");
                    if (String.Equals(cdnDirectory, "js"))
                    {
                        cdnDirectory = "scripts";
                    }

                    var externalFiles = dataRow.Field<string>("external_files") ?? "";
                    var wiserCdnTemplates = dataRow.Field<string>("wiser_cdn_templates") ?? "";
                    var allExternalFiles = externalFiles.Split(';').Where(f => !String.IsNullOrWhiteSpace(f)).ToList();
                    allExternalFiles.AddRange(wiserCdnTemplates.Split(';').Where(f => !String.IsNullOrWhiteSpace(f)).Select(filename => $"https://app.wiser.nl/{cdnDirectory}/cdn/{filename}"));

                    externalFiles = String.Join(";", allExternalFiles);

                    var content = dataRow.Field<string>("template_data") ?? "";
                    var minifiedContent = dataRow.Field<string>("template_data_minified") ?? "";

                    // Convert dynamic components placeholders from Wiser 1 to Wiser 3 format.
                    if (templateType == TemplateTypes.Html)
                    {
                        content = ConvertDynamicComponentsFromLegacyToNewInHtml(content);
                        minifiedContent = ConvertDynamicComponentsFromLegacyToNewInHtml(minifiedContent);
                    }

                    if (templateType is TemplateTypes.Html or TemplateTypes.Query)
                    {
                        content = ConvertLegacyReplacementMethodsToNewReplacementMethods(content);
                        minifiedContent = ConvertLegacyReplacementMethodsToNewReplacementMethods(minifiedContent);
                    }

                    var templateName = dataRow.Field<string>("template_name");
                    var ordering = dataRow.IsNull("ordering") ? 0 : dataRow["ordering"];
                    if (templateType == TemplateTypes.Directory && (dataRow.IsNull("parent_id") || Convert.ToInt32(dataRow["parent_id"]) == 0))
                    {
                        // Set the name and ordering of root directories to new Wiser 3 standards.
                        switch (templateName.ToUpperInvariant())
                        {
                            case "HTML":
                                ordering = 1;
                                break;
                            case "SCRIPTS":
                                ordering = 2;
                                templateName = "JS";
                                break;
                            case "SCSS":
                                ordering = 3;
                                break;
                            case "CSS":
                                ordering = 4;
                                break;
                            case "QUERY":
                                ordering = 5;
                                templateName = "SQL";
                                break;
                            case "AIS":
                                ordering = 6;
                                templateName = "SERVICES";
                                break;
                            case "VIEWS":
                                ordering = 7;
                                break;
                            case "ROUTINES":
                                ordering = 8;
                                break;
                            case "TRIGGERS":
                                ordering = 9;
                                break;
                        }

                        rootDirectoriesCreated.Add(templateName);
                    }

                    var templateId = dataRow["template_id"];
                    var publishedEnvironment = dataRow["published_environment"];
                    var changedOn = dataRow["changed_on"];
                    var changedBy = dataRow["changed_by"];
                    clientDatabaseConnection.ClearParameters();
                    clientDatabaseConnection.AddParameter("parent_id", dataRow["parent_id"]);
                    clientDatabaseConnection.AddParameter("template_name", templateName);
                    clientDatabaseConnection.AddParameter("template_data", content);
                    clientDatabaseConnection.AddParameter("template_data_minified", minifiedContent);
                    clientDatabaseConnection.AddParameter("template_type", templateType);
                    clientDatabaseConnection.AddParameter("version", dataRow["version"]);
                    clientDatabaseConnection.AddParameter("template_id", templateId);
                    clientDatabaseConnection.AddParameter("changed_on", changedOn);
                    clientDatabaseConnection.AddParameter("changed_by", changedBy);
                    clientDatabaseConnection.AddParameter("published_environment", publishedEnvironment);
                    clientDatabaseConnection.AddParameter("login_required", dataRow.IsNull("login_required") ? 0 : dataRow["login_required"]);
                    clientDatabaseConnection.AddParameter("login_session_prefix", dataRow["login_session_prefix"]);
                    clientDatabaseConnection.AddParameter("linked_templates", dataRow["linked_templates"]);
                    clientDatabaseConnection.AddParameter("ordering", ordering);
                    clientDatabaseConnection.AddParameter("insert_mode", dataRow.IsNull("insert_mode") ? 0 : dataRow["insert_mode"]);
                    clientDatabaseConnection.AddParameter("load_always", dataRow.IsNull("load_always") ? 0 : dataRow["load_always"]);
                    clientDatabaseConnection.AddParameter("url_regex", dataRow["url_regex"]);
                    clientDatabaseConnection.AddParameter("external_files", externalFiles);
                    clientDatabaseConnection.AddParameter("grouping_create_object_instead_of_array", dataRow.IsNull("grouping_create_object_instead_of_array") ? 0 : dataRow["grouping_create_object_instead_of_array"]);
                    clientDatabaseConnection.AddParameter("grouping_prefix", dataRow["grouping_prefix"]);
                    clientDatabaseConnection.AddParameter("grouping_key", dataRow["grouping_key"]);
                    clientDatabaseConnection.AddParameter("grouping_key_column_name", dataRow["grouping_key_column_name"]);
                    clientDatabaseConnection.AddParameter("grouping_value_column_name", dataRow["grouping_value_column_name"]);
                    clientDatabaseConnection.AddParameter("is_scss_include_template", dataRow.IsNull("is_scss_include_template") ? 0 : dataRow["is_scss_include_template"]);
                    clientDatabaseConnection.AddParameter("use_in_wiser_html_editors", dataRow.IsNull("use_in_wiser_html_editors") ? 0 : dataRow["use_in_wiser_html_editors"]);

                    var useCacheValue = dataRow.IsNull("use_cache") ? 0 : Convert.ToInt32(dataRow["use_cache"]);
                    var cacheMinutesValue = dataRow.IsNull("cache_minutes") ? 0 : Convert.ToInt32(dataRow["cache_minutes"]);
                    clientDatabaseConnection.AddParameter("cache_per_url", useCacheValue >= 3);
                    clientDatabaseConnection.AddParameter("cache_per_querystring", useCacheValue >= 4);
                    clientDatabaseConnection.AddParameter("cache_per_hostname", useCacheValue >= 5);
                    clientDatabaseConnection.AddParameter("cache_using_regex", useCacheValue >= 6);
                    clientDatabaseConnection.AddParameter("cache_minutes", useCacheValue == 0 && cacheMinutesValue <= 0 ? -1 : cacheMinutesValue);

                    await clientDatabaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(WiserTableNames.WiserTemplate, 0);

                    // Convert dynamic content.
                    if (templateType != TemplateTypes.Html)
                    {
                        continue;
                    }

                    query = @"SELECT *
FROM easy_dynamiccontent
WHERE itemid = ?template_id
AND version = ?version";
                    var dynamicContentDataTable = await clientDatabaseConnection.GetAsync(query);
                    if (dynamicContentDataTable.Rows.Count == 0)
                    {
                        continue;
                    }

                    foreach (DataRow dynamicContentRow in dynamicContentDataTable.Rows)
                    {
                        var contentId = dynamicContentRow.Field<int>("id");
                        var legacyComponentName = dynamicContentRow.Field<string>("freefield1");
                        var legacySettingsJson = dynamicContentRow.Field<string>("filledvariables");
                        var (gclComponentName, gclSettingsJson, title, componentMode) = ConvertDynamicComponentSettingsFromLegacyToNew(legacyComponentName, legacySettingsJson);

                        clientDatabaseConnection.ClearParameters();
                        clientDatabaseConnection.AddParameter("content_id", contentId);
                        clientDatabaseConnection.AddParameter("settings", gclSettingsJson);
                        clientDatabaseConnection.AddParameter("component", gclComponentName);
                        clientDatabaseConnection.AddParameter("component_mode", componentMode);
                        clientDatabaseConnection.AddParameter("version", dynamicContentRow.Field<int>("version"));
                        clientDatabaseConnection.AddParameter("title", title);
                        clientDatabaseConnection.AddParameter("published_environment", publishedEnvironment);
                        clientDatabaseConnection.AddParameter("changed_on", changedOn);
                        clientDatabaseConnection.AddParameter("changed_by", changedBy);
                        await clientDatabaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(WiserTableNames.WiserDynamicContent, 0);

                        clientDatabaseConnection.ClearParameters();
                        clientDatabaseConnection.AddParameter("content_id", contentId);
                        clientDatabaseConnection.AddParameter("destination_template_id", templateId);
                        clientDatabaseConnection.AddParameter("added_on", changedOn);
                        clientDatabaseConnection.AddParameter("added_by", changedBy);
                        await clientDatabaseConnection.ExecuteAsync(@$"INSERT IGNORE INTO {WiserTableNames.WiserTemplateDynamicContent} (content_id, destination_template_id, added_on, added_by)
VALUES (?content_id, ?destination_template_id, ?added_on, ?added_by)");
                    }
                }

                await clientDatabaseConnection.CommitTransactionAsync();

                // Add any missing root directories. For some reason we get timeouts/deadlocks when we do this in the same transaction as the rest of the conversion, so we have to to it after the commit.
                var missingRootDirectories = allRootDirectories.Except(rootDirectoriesCreated);
                foreach (var directory in missingRootDirectories)
                {
                    var ordering = directory.ToUpperInvariant() switch
                    {
                        "HTML" => 1,
                        "SCRIPTS" => 2,
                        "SCSS" => 3,
                        "CSS" => 4,
                        "QUERY" => 5,
                        "AIS" => 6,
                        "VIEWS" => 7,
                        "ROUTINES" => 8,
                        "TRIGGERS" => 9,
                        _ => 0
                    };

                    await templateDataService.CreateAsync(directory, null, TemplateTypes.Directory, IdentityHelpers.GetUserName(identity, true), ordering: ordering);
                }
            }
            catch
            {
                await clientDatabaseConnection.RollbackTransactionAsync();
                throw;
            }

            return new ServiceResult<bool>(true);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> DeployToBranchAsync(ClaimsIdentity identity, List<int> templateIds, int branchId)
        {
            // The user must be logged in the main branch, otherwise they can't use this functionality.
            if (!(await branchesService.IsMainBranchAsync(identity)).ModelObject)
            {
                return new ServiceResult<bool>
                {
                    ModelObject = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "The current branch is not the main branch. This functionality can only be used from the main branch."
                };
            }

            // Check if the branch exists.
            var branchToDeploy = (await wiserTenantsService.GetSingleAsync(branchId, true)).ModelObject;
            if (branchToDeploy == null)
            {
                return new ServiceResult<bool>
                {
                    ModelObject = false,
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = $"Branch with ID {branchId} does not exist"
                };
            }

            // Make sure the user did not try to enter an ID for a branch that they don't own.
            if (!(await branchesService.CanAccessBranchAsync(identity, branchToDeploy)).ModelObject)
            {
                return new ServiceResult<bool>
                {
                    ModelObject = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = $"You don't have permissions to access a branch with ID {branchId}"
                };
            }

            // Now we can deploy the template to the branch.
            try
            {
                await templateDataService.DeployToBranchAsync(templateIds, branchToDeploy.Database.DatabaseName);
            }
            catch (MySqlException mySqlException)
            {
                switch (mySqlException.Number)
                {
                    case (int)MySqlErrorCode.DuplicateKeyEntry:
                        // We ignore duplicate key errors, because it's possible that a template already exists in a branch, but it wasn't deployed to the correct environment.
                        // So we ignore this error, so we can still deploy that template to production in the branch, see the next bit of code after this try/catch.
                        break;
                    case (int)MySqlErrorCode.WrongValueCountOnRow:
                        return new ServiceResult<bool>
                        {
                            StatusCode = HttpStatusCode.Conflict,
                            ErrorMessage = "The tables for the template module are not up-to-date in the selected branch. Please open the template module in that branch once, so that the tables will be automatically updated."
                        };
                    default:
                        throw;
                }
            }

            // Publish all templates to live environment in the branch, because a branch will never actually be used on a live environment
            // and then we can make sure that the deployed templates will be up to date on whichever website environment uses that branch.
            // Also copy stored procedures, views and triggers to the branch.
            foreach (var templateId in templateIds)
            {
                var templateMetaData = await templateDataService.GetMetaDataAsync(templateId);
                var currentPublished = (await GetTemplateEnvironmentsAsync(templateId, branchToDeploy.Database.DatabaseName)).ModelObject;

                await PublishToEnvironmentAsync(identity, templateId, templateMetaData.Version, Environments.Live, currentPublished, branchToDeploy.Database.DatabaseName);
            }

            return new ServiceResult<bool>(true)
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }

        /// <inheritdoc />
        public async Task<ServiceResult<MeasurementSettings>> GetMeasurementSettingsAsync(int templateId = 0, int componentId = 0)
        {
            switch (templateId)
            {
                case <= 0 when componentId <= 0:
                    throw new Exception("Please specify either a template ID or a component ID.");
                case > 0 when componentId > 0:
                    throw new Exception("You have specified both a template ID and a component ID, please specify only one.");
            }

            var result = new MeasurementSettings();

            var name = templateId > 0 ? "templates" : "components";
            var developmentRenderingSettings = await objectsService.FindSystemObjectByDomainNameAsync($"log_rendering_of_{name}_development");
            var testRenderingSettings = await objectsService.FindSystemObjectByDomainNameAsync($"log_rendering_of_{name}_test");
            var acceptanceRenderingSettings = await objectsService.FindSystemObjectByDomainNameAsync($"log_rendering_of_{name}_acceptance");
            var liveRenderingSettings = await objectsService.FindSystemObjectByDomainNameAsync($"log_rendering_of_{name}_live");

            var logAllRendering = await objectsService.FindSystemObjectByDomainNameAsync($"log_rendering_of_{name}");
            if (String.IsNullOrWhiteSpace(developmentRenderingSettings))
            {
                developmentRenderingSettings = logAllRendering;
            }
            if (String.IsNullOrWhiteSpace(testRenderingSettings))
            {
                testRenderingSettings = logAllRendering;
            }
            if (String.IsNullOrWhiteSpace(acceptanceRenderingSettings))
            {
                acceptanceRenderingSettings = logAllRendering;
            }
            if (String.IsNullOrWhiteSpace(liveRenderingSettings))
            {
                liveRenderingSettings = logAllRendering;
            }

            if (!String.IsNullOrWhiteSpace(developmentRenderingSettings))
            {
                var ids = developmentRenderingSettings.Split(",").Select(value => !Int32.TryParse(value, out var id) ? 0 : id);
                result.MeasureRenderTimesOnDevelopmentForCurrent = ids.Contains(templateId);
                result.MeasureRenderTimesOnDevelopmentForEverything = String.Equals(developmentRenderingSettings, "true", StringComparison.OrdinalIgnoreCase)
                                                                      || String.Equals(developmentRenderingSettings, "all", StringComparison.OrdinalIgnoreCase);
            }

            if (!String.IsNullOrWhiteSpace(testRenderingSettings))
            {
                var ids = testRenderingSettings.Split(",").Select(value => !Int32.TryParse(value, out var id) ? 0 : id);
                result.MeasureRenderTimesOnTestForCurrent = ids.Contains(templateId);
                result.MeasureRenderTimesOnTestForEverything = String.Equals(testRenderingSettings, "true", StringComparison.OrdinalIgnoreCase)
                                                               || String.Equals(testRenderingSettings, "all", StringComparison.OrdinalIgnoreCase);
            }

            if (!String.IsNullOrWhiteSpace(acceptanceRenderingSettings))
            {
                var ids = acceptanceRenderingSettings.Split(",").Select(value => !Int32.TryParse(value, out var id) ? 0 : id);
                result.MeasureRenderTimesOnAcceptanceForCurrent = ids.Contains(templateId);
                result.MeasureRenderTimesOnAcceptanceForEverything = String.Equals(acceptanceRenderingSettings, "true", StringComparison.OrdinalIgnoreCase)
                                                                     || String.Equals(acceptanceRenderingSettings, "all", StringComparison.OrdinalIgnoreCase);
            }

            if (!String.IsNullOrWhiteSpace(liveRenderingSettings))
            {
                var ids = liveRenderingSettings.Split(",").Select(value => !Int32.TryParse(value, out var id) ? 0 : id);
                result.MeasureRenderTimesOnLiveForCurrent = ids.Contains(templateId);
                result.MeasureRenderTimesOnLiveForEverything = String.Equals(liveRenderingSettings, "true", StringComparison.OrdinalIgnoreCase)
                                                               || String.Equals(liveRenderingSettings, "all", StringComparison.OrdinalIgnoreCase);
            }

            return new ServiceResult<MeasurementSettings>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> SaveMeasurementSettingsAsync(MeasurementSettings settings, int templateId = 0, int componentId = 0)
        {
            switch (templateId)
            {
                case <= 0 when componentId <= 0:
                    throw new Exception("Please specify either a template ID or a component ID.");
                case > 0 when componentId > 0:
                    throw new Exception("You have specified both a template ID and a component ID, please specify only one.");
            }

            var previousSettings = (await GetMeasurementSettingsAsync(templateId, componentId)).ModelObject;
            if (previousSettings.MeasureRenderTimesOnDevelopmentForEverything || previousSettings.MeasureRenderTimesOnTestForEverything || previousSettings.MeasureRenderTimesOnAcceptanceForEverything || previousSettings.MeasureRenderTimesOnLiveForEverything)
            {
                return new ServiceResult<bool>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "Cannot change these settings, because they are enabled globally."
                };
            }

            // Get the current settings from database.
            var name = templateId > 0 ? "templates" : "components";
            var developmentRenderingSettings = await objectsService.FindSystemObjectByDomainNameAsync($"log_rendering_of_{name}_development");
            var testRenderingSettings = await objectsService.FindSystemObjectByDomainNameAsync($"log_rendering_of_{name}_test");
            var acceptanceRenderingSettings = await objectsService.FindSystemObjectByDomainNameAsync($"log_rendering_of_{name}_acceptance");
            var liveRenderingSettings = await objectsService.FindSystemObjectByDomainNameAsync($"log_rendering_of_{name}_live");
            var logAllRendering = await objectsService.FindSystemObjectByDomainNameAsync($"log_rendering_of_{name}");
            if (String.IsNullOrWhiteSpace(developmentRenderingSettings))
            {
                developmentRenderingSettings = logAllRendering;
            }
            if (String.IsNullOrWhiteSpace(testRenderingSettings))
            {
                testRenderingSettings = logAllRendering;
            }
            if (String.IsNullOrWhiteSpace(acceptanceRenderingSettings))
            {
                acceptanceRenderingSettings = logAllRendering;
            }
            if (String.IsNullOrWhiteSpace(liveRenderingSettings))
            {
                liveRenderingSettings = logAllRendering;
            }

            // Add or remove the current template from the settings.
            var developmentIds = developmentRenderingSettings.Split(",").Select(Int32.Parse).ToList();
            var testIds = testRenderingSettings.Split(",").Select(Int32.Parse).ToList();
            var acceptanceIds = acceptanceRenderingSettings.Split(",").Select(Int32.Parse).ToList();
            var liveIds = liveRenderingSettings.Split(",").Select(Int32.Parse).ToList();

            if (settings.MeasureRenderTimesOnDevelopmentForCurrent)
            {
                if (!developmentIds.Contains(templateId)) developmentIds.Add(templateId);
            }
            else
            {
                if (developmentIds.Contains(templateId)) developmentIds.Remove(templateId);
            }

            if (settings.MeasureRenderTimesOnTestForCurrent)
            {
                if (!testIds.Contains(templateId)) testIds.Add(templateId);
            }
            else
            {
                if (testIds.Contains(templateId)) testIds.Remove(templateId);
            }

            if (settings.MeasureRenderTimesOnAcceptanceForCurrent)
            {
                if (!acceptanceIds.Contains(templateId)) acceptanceIds.Add(templateId);
            }
            else
            {
                if (acceptanceIds.Contains(templateId)) acceptanceIds.Remove(templateId);
            }

            if (settings.MeasureRenderTimesOnLiveForCurrent)
            {
                if (!liveIds.Contains(templateId)) liveIds.Add(templateId);
            }
            else
            {
                if (liveIds.Contains(templateId)) liveIds.Remove(templateId);
            }

            // Save the new settings.
            await objectsService.SetSystemObjectValueAsync($"log_rendering_of_{name}", ""); // These are saved empty, because we copied all IDs to the specific environment settings.
            await objectsService.SetSystemObjectValueAsync($"log_rendering_of_{name}_development", String.Join(",", developmentIds));
            await objectsService.SetSystemObjectValueAsync($"log_rendering_of_{name}_test", String.Join(",", testIds));
            await objectsService.SetSystemObjectValueAsync($"log_rendering_of_{name}_acceptance", String.Join(",", acceptanceIds));
            await objectsService.SetSystemObjectValueAsync($"log_rendering_of_{name}_live", String.Join(",", liveIds));
            return new ServiceResult<bool>
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<RenderLogModel>>> GetRenderLogsAsync(int templateId, int version = 0,
            string urlRegex = null, Environments? environment = null, ulong userId = 0,
            string languageCode = null, int pageSize = 500, int pageNumber = 1,
            bool getDailyAverage = false, DateTime? start = null, DateTime? end = null)
        {
            var results = await measurementsDataService.GetRenderLogsAsync(templateId, 0, version, urlRegex, environment, userId, languageCode, pageSize, pageNumber, getDailyAverage, start, end);
            return new ServiceResult<List<RenderLogModel>>(results);
        }

        /// <summary>
        /// Converts JCL replacement methods (such as {title_seo}) to GCL replacement methods (such as {title:seo}).
        /// </summary>
        /// <param name="input">The string from the JCL to convert.</param>
        /// <returns>The converted string that can be used in the GCL.</returns>
        private static string ConvertLegacyReplacementMethodsToNewReplacementMethods(string input)
        {
            if (String.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            // Decrypt.
            input = ConvertEncryptOrDecryptReplacement(input, "decrypt_withdate", "Decrypt", true);
            input = ConvertEncryptOrDecryptReplacement(input, "normaldecrypt_withdate", "DecryptNormal", true);
            input = ConvertEncryptOrDecryptReplacement(input, "decrypt", "Decrypt", false);
            input = ConvertEncryptOrDecryptReplacement(input, "normaldecrypt", "DecryptNormal", false);

            // Encrypt.
            input = ConvertEncryptOrDecryptReplacement(input, "encrypt_withdate", "Encrypt", true);
            input = ConvertEncryptOrDecryptReplacement(input, "normalencrypt_withdate", "EncryptNormal", true);
            input = ConvertEncryptOrDecryptReplacement(input, "encrypt", "Encrypt", false);
            input = ConvertEncryptOrDecryptReplacement(input, "normalencrypt", "EncryptNormal", false);

            // Likes in queries.
            if (input.Contains("like", StringComparison.OrdinalIgnoreCase))
            {
                input = Regex.Replace(input, @"LIKE '%\[\{(?<variableName>[^}]+?)\}\]%'", "LIKE CONCAT('%', '{${variableName}}', '%')");
                input = Regex.Replace(input, @"LIKE '%\{(?<variableName>[^}]+?)\}%'", "LIKE CONCAT('%', '{${variableName}}', '%')");

                input = Regex.Replace(input, @"LIKE '%\[\{(?<variableName>[^}]+?)\}\]'", "LIKE CONCAT('%', '{${variableName}}')");
                input = Regex.Replace(input, @"LIKE '%\{(?<variableName>[^}]+?)\}'", "LIKE CONCAT('%', '{${variableName}}')");

                input = Regex.Replace(input, @"LIKE '\[\{(?<variableName>[^}]+?)\}\]%'", "LIKE CONCAT('{${variableName}}', '%')");
                input = Regex.Replace(input, @"LIKE '\{(?<variableName>[^}]+?)\}%'", "LIKE CONCAT('{${variableName}}', '%')");
            }

            input = ConvertBasicReplacement(input, "seo", "Seo");
            input = ConvertBasicReplacement(input, "htmlencode", "HtmlEncode");
            input = ConvertBasicReplacement(input, "sha512", "Sha512");
            input = ConvertBasicReplacement(input, "urldataescape", "UrlEncode");
            input = ConvertBasicReplacement(input, "urldataunescape", "UrlDecode");
            input = ConvertBasicReplacement(input, "striphtml", "StripHtml");
            input = ConvertBasicReplacement(input, "valuta", "Currency");
            input = ConvertBasicReplacement(input, "valutasup", "CurrencySup");
            input = ConvertBasicReplacement(input, "jsonsafe", "JsonSafe");
            input = ConvertBasicReplacement(input, "stripstyle", "StripInlineStyle");
            input = ConvertBasicReplacement(input, "base64", "Base64");
            input = ConvertBasicReplacement(input, "price", "Currency", "true");
            input = ConvertBasicReplacement(input, "currency", "Currency", "true");

            // Miscellaneous conversions.
            input = input.Replace("{items}", "{items:Raw}");

            return input;
        }

        /// <summary>
        /// Converts encryption/decryption replacements from JCL (eg {title_seo}) to GCL (eg {title:Seo}).
        /// </summary>
        /// <param name="input">The string from the JCL to do the replacements in.</param>
        /// <param name="jclSuffix">The JCL suffix (without underscore) for the replacement to handle, such as "seo".</param>
        /// <param name="gclSuffix">The suffix that it should be in the GCL.</param>
        /// <param name="withDate">Whether this is an encryption/decryption that uses a datetime to make it invalid after X time.</param>
        private static string ConvertEncryptOrDecryptReplacement(string input, string jclSuffix, string gclSuffix, bool withDate)
        {
            var regex = new Regex(@$"{{(?<variableName>.+)_{jclSuffix}(?<minutesOverride>[\|0-9]*)}}");
            foreach (Match match in regex.Matches(input))
            {
                var minutesOverride = match.Groups["minutesOverride"].Value.TrimStart('|');
                if (!String.IsNullOrWhiteSpace(minutesOverride))
                {
                    minutesOverride = $",{minutesOverride}";
                }
                var newVariable = $"{{{match.Groups["variableName"].Value}:{gclSuffix}({withDate}{minutesOverride})}}";
                input = input.Replace(match.Value, newVariable);
            }

            return input;
        }

        /// <summary>
        /// Converts basic replacements from JCL (eg {title_seo}) to GCL (eg {title:Seo}).
        /// </summary>
        /// <param name="input">The string from the JCL to do the replacements in.</param>
        /// <param name="jclSuffix">The JCL suffix (without underscore) for the replacement to handle, such as "seo".</param>
        /// <param name="gclSuffix">The suffix that it should be in the GCL.</param>
        /// <param name="extraParameters">Any extra parameters that the replacement function in the GCL might need.</param>
        private static string ConvertBasicReplacement(string input, string jclSuffix, string gclSuffix, string extraParameters = "")
        {
            if (!String.IsNullOrEmpty(gclSuffix))
            {
                gclSuffix = $":{gclSuffix}";
            }

            var regex = new Regex(@$"{{(?<variableName>[^\{{\}}\n]+)_{jclSuffix}(?<parameters>[\|][^\}}\n]*)?}}");
            foreach (Match match in regex.Matches(input))
            {
                var extraBracketToReplace = "";
                var parameters = match.Groups["parameters"].Value?.Trim('|');

                if (parameters != null && parameters.StartsWith("{") && !parameters.EndsWith("}"))
                {
                    // This is a bit of a hack, because in some cases there will be values like "{price_currency|{culture}}" and our regex will return "{culture" without the last bracket.
                    // But if we change the regex to include that bracket, then it will often return too much, like "{price_currency|{culture}} <div></div>{otherVariable}" for example.
                    parameters += "}";
                    extraBracketToReplace = "}";
                }

                parameters = String.IsNullOrWhiteSpace(parameters) ? extraParameters : $"{extraParameters},{parameters}";
                if (!String.IsNullOrEmpty(parameters))
                {
                    parameters = $"({parameters})";
                }

                var newVariable = $"{{{match.Groups["variableName"].Value}{gclSuffix}{parameters}}}";
                input = input.Replace($"{match.Value}{extraBracketToReplace}", newVariable);
            }

            return input;
        }

        /// <summary>
        /// Converts all dynamic components in an HTML string from JCL tags to GCL tags.
        /// The JCL uses img tags for components, the GCL uses divs.
        /// </summary>
        /// <param name="html">The HTML from the JCL templates module.</param>
        /// <param name="forJson">Set to true if this is for JSON settings, so that quotes and such will be escaped.</param>
        /// <returns>The HTML for the GCL templates module.</returns>
        private static string ConvertDynamicComponentsFromLegacyToNewInHtml(string html, bool forJson = false)
        {
            var regex = new Regex(@"<img[^>]*?(?:data=['\""\\]+(?<data>.*?)['\""\\]+[^>]*?)?contentid=['\""\\]+(?<contentId>\d+)['\""\\]+[^>]*?\/?>");
            var matches = regex.Matches(html);
            foreach (Match match in matches)
            {
                if (!match.Success)
                {
                    continue;
                }

                var dataAttribute = match.Groups["data"].Value;
                if (!String.IsNullOrWhiteSpace(dataAttribute))
                {
                    dataAttribute = $"data=\"{dataAttribute}\"";
                }

                var componentId = match.Groups["contentId"].Value;
                var newElement = $"<div class=\"dynamic-content\" {dataAttribute} component-id=\"{componentId}\"><h2>Component {componentId}</h2></div>";
                html = html.Replace(match.Value, forJson ? HttpUtility.JavaScriptStringEncode(newElement) : newElement);
            }

            return html;
        }

        /// <summary>
        /// Converts the settings JSON from the JCL to the settings JSON for the GCL.
        /// </summary>
        /// <param name="legacyComponentName">The name of the component in the JCL.</param>
        /// <param name="legacySettingsJson">The JSON with the settings from the JCL.</param>
        /// <returns>The name, settings, title and mode for the GCL component.</returns>
        private static (string GclComponentName, string GclSettingsJson, string Title, string ComponentMode) ConvertDynamicComponentSettingsFromLegacyToNew(string legacyComponentName, string legacySettingsJson)
        {
            legacySettingsJson ??= "";

            string viewComponentName;
            string newSettingsJson;
            string title;
            string componentMode;
            switch (legacyComponentName)
            {
                case "JuiceControlLibrary.MLSimpleMenu":
                {
                    viewComponentName = "Repeater";
                    var legacySettings = JsonConvert.DeserializeObject<MlSimpleMenuLegacySettingsModel>(legacySettingsJson);
                    componentMode = Repeater.LegacyComponentMode.NonLegacy.ToString();
                    newSettingsJson = JsonConvert.SerializeObject(legacySettings?.ToSettingsModel());
                    title = legacySettings?.VisibleDescription;
                    break;
                }
                case "JuiceControlLibrary.SimpleMenu":
                {
                    viewComponentName = "Repeater";
                    var legacySettings = JsonConvert.DeserializeObject<SimpleMenuLegacySettingsModel>(legacySettingsJson);
                    componentMode = Repeater.LegacyComponentMode.NonLegacy.ToString();
                    newSettingsJson = JsonConvert.SerializeObject(legacySettings?.ToSettingsModel());
                    title = legacySettings?.VisibleDescription;
                    break;
                }
                case "JuiceControlLibrary.ProductModule":
                {
                    viewComponentName = "Repeater";
                    var legacySettings = JsonConvert.DeserializeObject<ProductModuleLegacySettingsModel>(legacySettingsJson);
                    componentMode = Repeater.LegacyComponentMode.NonLegacy.ToString();
                    newSettingsJson = JsonConvert.SerializeObject(legacySettings?.ToSettingsModel());
                    title = legacySettings?.VisibleDescription;
                    break;
                }
                case "JuiceControlLibrary.DBField":
                {
                    viewComponentName = "Repeater";
                    var legacySettings = JsonConvert.DeserializeObject<DbFieldLegacySettingsModel>(legacySettingsJson);
                    componentMode = Repeater.LegacyComponentMode.NonLegacy.ToString();
                    newSettingsJson = JsonConvert.SerializeObject(legacySettings?.ToSettingsModel());
                    title = legacySettings?.VisibleDescription;
                    break;
                }
                case "JuiceControlLibrary.AccountWiser2":
                {
                    viewComponentName = "Account";
                    var legacySettings = JsonConvert.DeserializeObject<CmsSettingsLegacy>(legacySettingsJson);
                    componentMode = ((Account.ComponentModes)legacySettings.ComponentMode).ToString();
                    // Settings for account are the same for JCL and GCL.
                    newSettingsJson = legacySettingsJson;
                    title = legacySettings?.VisibleDescription;
                    break;
                }
                case "JuiceControlLibrary.ShoppingBasket":
                {
                    viewComponentName = "ShoppingBasket";
                    var legacySettings = JsonConvert.DeserializeObject<ShoppingBasketLegacySettingsModel>(legacySettingsJson);
                    componentMode = ShoppingBasket.ComponentModes.Legacy.ToString();
                    newSettingsJson = JsonConvert.SerializeObject(legacySettings?.ToSettingsModel());
                    title = legacySettings?.VisibleDescription;
                    break;
                }
                case "JuiceControlLibrary.WebPage":
                {
                    viewComponentName = "WebPage";
                    var legacySettings = JsonConvert.DeserializeObject<WebPageLegacySettingsModel>(legacySettingsJson);
                    componentMode = WebPage.ComponentModes.Render.ToString();
                    newSettingsJson = JsonConvert.SerializeObject(legacySettings?.ToSettingsModel());
                    title = legacySettings?.VisibleDescription;
                    break;
                }
                case "JuiceControlLibrary.Pagination":
                {
                    viewComponentName = "Pagination";
                    var legacySettings = JsonConvert.DeserializeObject<PaginationLegacySettingsModel>(legacySettingsJson);
                    componentMode = Pagination.ComponentModes.Normal.ToString();
                    newSettingsJson = JsonConvert.SerializeObject(legacySettings?.ToSettingsModel());
                    title = legacySettings?.VisibleDescription;
                    break;
                }
                case "JuiceControlLibrary.DynamicFilter":
                {
                    viewComponentName = "Filter";
                    var legacySettings = JsonConvert.DeserializeObject<CmsSettingsLegacy>(legacySettingsJson);
                    componentMode = Filter.ComponentModes.Aggregation.ToString();
                    // Settings for account are the same for JCL and GCL.
                    newSettingsJson = legacySettingsJson;
                    title = legacySettings?.VisibleDescription;
                    break;
                }
                case "JuiceControlLibrary.Sendform":
                {
                    viewComponentName = "WebForm";
                    var legacySettings = JsonConvert.DeserializeObject<WebFormLegacySettingsModel>(legacySettingsJson);
                    componentMode = WebForm.ComponentModes.BasicForm.ToString();
                    newSettingsJson = JsonConvert.SerializeObject(legacySettings?.ToSettingsModel());
                    title = legacySettings?.VisibleDescription;
                    break;
                }
                case "JuiceControlLibrary.DataSelectorParser":
                {
                    viewComponentName = "DataSelectorParser";
                    var legacySettings = JsonConvert.DeserializeObject<CmsSettingsLegacy>(legacySettingsJson);
                    componentMode = DataSelectorParser.ComponentModes.Render.ToString();
                    // Settings for account are the same for JCL and GCL.
                    newSettingsJson = legacySettingsJson;
                    title = legacySettings?.VisibleDescription;
                    break;
                }
                default:
                    return ("Unknown", legacySettingsJson, $"TODO - {legacyComponentName} - This needs to be converted manually!", "Default");
            }

            newSettingsJson = ConvertDynamicComponentsFromLegacyToNewInHtml(newSettingsJson, true);
            newSettingsJson = ConvertLegacyReplacementMethodsToNewReplacementMethods(newSettingsJson);

            return (viewComponentName, newSettingsJson, title, componentMode);
        }

        private static string ConvertDynamicComponentsFromLegacyToNewInHtml(string html)
        {
            var regex = new Regex(@"<img[^>]*?(?:data=['""](?<data>.*?)['""][^>]*?)?contentid=['""](?<contentId>\d+)['""][^>]*?\/?>", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
            var matches = regex.Matches(html);
            foreach (Match match in matches)
            {
                if (!match.Success)
                {
                    continue;
                }

                var dataAttribute = match.Groups["data"].Value;
                if (!String.IsNullOrWhiteSpace(dataAttribute))
                {
                    dataAttribute = $"data=\"{dataAttribute}\"";
                }

                var componentId = match.Groups["contentId"].Value;
                var newElement = $"<div class=\"dynamic-content\" {dataAttribute} component-id=\"{componentId}\"><h2>Component {componentId}</h2></div>";
                html = html.Replace(match.Value, newElement);
            }

            return html;
        }

        /// <summary>
        /// Convert LinkedDynamicContentDAO to a DynamicContentOverviewModel.
        /// </summary>
        /// <param name="linkedContent">The LinkedDynamicContentDAO that should be converted.</param>
        /// <returns>A DynamicContentOverviewModel of the linked content given as param.</returns>
        private DynamicContentOverviewModel LinkedDynamicContentToDynamicContentOverview(LinkedDynamicContentDao linkedContent)
        {
            var overview = new DynamicContentOverviewModel
            {
                Id = linkedContent.Id,
                Component = linkedContent.Component,
                ComponentMode = linkedContent.ComponentMode,
                ChangedOn = linkedContent.ChangedOn,
                ChangedBy = linkedContent.ChangedBy,
                Title = linkedContent.Title,
                Usages = String.IsNullOrEmpty(linkedContent.Usages) ? new List<string>() : linkedContent.Usages.Split(",").ToList()
            };

            return overview;
        }

        /// <summary>
        /// Adds the main domain to an url (for CSS/javascript).
        /// </summary>
        /// <param name="input"></param>
        /// <param name="domain"></param>
        /// <returns></returns>
        private static string AddMainDomainToUrl(string input, string domain)
        {
            if (String.IsNullOrWhiteSpace(domain) || String.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            if (!domain.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !domain.StartsWith("//"))
            {
                domain = $"http://{domain}";
            }

            domain = domain.TrimEnd('/');
            input = input.TrimStart('/');
            return $"{domain}/{input}";
        }

        /// <summary>
        /// Retrieves data about a view from the database.
        /// </summary>
        /// <param name="viewName">The name of the view.</param>
        /// <returns>A <see cref="TemplateSettingsModel"/> object containing the data of the view.</returns>
        private async Task<TemplateSettingsModel> GetDatabaseViewDataAsync(string viewName)
        {
            var result = new TemplateSettingsModel();

            if (String.IsNullOrWhiteSpace(viewName))
            {
                return result;
            }

            clientDatabaseConnection.AddParameter("viewName", viewName);
            var dataTable = await clientDatabaseConnection.GetAsync("SELECT TABLE_NAME, VIEW_DEFINITION FROM information_schema.VIEWS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = ?viewName");

            if (dataTable.Rows.Count == 0)
            {
                return result;
            }

            result.Name = dataTable.Rows[0].Field<string>("TABLE_NAME") ?? String.Empty;
            result.EditorValue = dataTable.Rows[0].Field<string>("VIEW_DEFINITION")?.Trim() ?? String.Empty;;

            return result;
        }

        /// <summary>
        /// Retrieves data about a routine from the database (function or stored procedure).
        /// </summary>
        /// <param name="routineName">The name of the routine.</param>
        /// <returns>A <see cref="TemplateSettingsModel"/> object containing the data of the routine.</returns>
        private async Task<TemplateSettingsModel> GetDatabaseRoutineDataAsync(string routineName)
        {
            var result = new TemplateSettingsModel();

            if (String.IsNullOrWhiteSpace(routineName))
            {
                return result;
            }

            // First retrieve the body.
            clientDatabaseConnection.AddParameter("routineName", routineName);
            var dataTable = await clientDatabaseConnection.GetAsync(@"
                SELECT ROUTINE_NAME, ROUTINE_TYPE, DTD_IDENTIFIER, ROUTINE_DEFINITION
                FROM information_schema.ROUTINES
                WHERE ROUTINE_SCHEMA = DATABASE() AND ROUTINE_NAME = ?routineName;");

            if (dataTable.Rows.Count == 0)
            {
                return result;
            }

            // Parse the routine type to the enum value, which should the same (except it's not fully uppercase).
            if (!Enum.TryParse(dataTable.Rows[0].Field<string>("ROUTINE_TYPE"), true, out RoutineTypes routineType))
            {
                return result;
            }

            var routineDefinition = dataTable.Rows[0].Field<string>("ROUTINE_DEFINITION")?.Trim() ?? String.Empty;
            if (!String.IsNullOrWhiteSpace(routineDefinition))
            {
                // Strip the "BEGIN" and "END" parts of the definition (Wiser doesn't need them).
                if (routineDefinition.StartsWith("BEGIN", StringComparison.OrdinalIgnoreCase))
                {
                    routineDefinition = routineDefinition[5..].Trim();
                }

                if (routineDefinition.EndsWith("END", StringComparison.OrdinalIgnoreCase))
                {
                    routineDefinition = routineDefinition[..^3].Trim();
                }
            }

            result.Name = dataTable.Rows[0].Field<string>("ROUTINE_NAME") ?? String.Empty;
            result.EditorValue = routineDefinition;
            result.RoutineType = routineType;
            result.RoutineReturnType = routineType == RoutineTypes.Function ? dataTable.Rows[0].Field<string>("DTD_IDENTIFIER") ?? String.Empty : String.Empty;

            // Now retrieve the parameters. The parameter at position 0 is the return type of a function, which is already known.
            dataTable = await clientDatabaseConnection.GetAsync($@"
                SELECT PARAMETER_MODE, PARAMETER_NAME, DTD_IDENTIFIER
                FROM information_schema.PARAMETERS
                WHERE SPECIFIC_SCHEMA = DATABASE() AND SPECIFIC_NAME = ?routineName AND ORDINAL_POSITION > 0
                ORDER BY ORDINAL_POSITION;");

            if (dataTable.Rows.Count == 0)
            {
                // Routine has no parameters; return the result.
                return result;
            }

            var routineParameters = new List<string>(dataTable.Rows.Count);
            foreach (var dataRow in dataTable.Rows.Cast<DataRow>())
            {
                // Parameters of a procedure have a mode (IN, OUT, and INOUT).
                var parameterModePart = String.Empty;
                if (routineType == RoutineTypes.Procedure)
                {
                    parameterModePart = $"{dataRow.Field<string>("PARAMETER_MODE")} ";
                }

                routineParameters.Add($"{parameterModePart}`{dataRow.Field<string>("PARAMETER_NAME")}` {dataRow.Field<string>("DTD_IDENTIFIER")}");
            }

            result.RoutineParameters = String.Join(", ", routineParameters);

            return result;
        }

        private async Task<TemplateSettingsModel> GetDatabaseTriggerDataAsync(string triggerName)
        {
            var result = new TemplateSettingsModel();

            if (String.IsNullOrWhiteSpace(triggerName))
            {
                return result;
            }

            // First retrieve the body.
            clientDatabaseConnection.AddParameter("triggerName", triggerName);
            var dataTable = await clientDatabaseConnection.GetAsync(@"
                SELECT TRIGGER_NAME, EVENT_MANIPULATION, EVENT_OBJECT_TABLE, ACTION_STATEMENT, ACTION_TIMING
                FROM information_schema.TRIGGERS
                WHERE TRIGGER_SCHEMA = DATABASE() AND TRIGGER_NAME = ?triggerName;");

            if (dataTable.Rows.Count == 0)
            {
                return result;
            }

            // Parse the trigger timing to the enum value, which should the same (except it's not fully uppercase).
            if (!Enum.TryParse(dataTable.Rows[0].Field<string>("ACTION_TIMING"), true, out TriggerTimings triggerTiming))
            {
                return result;
            }

            // Parse the trigger event to the enum value, which should the same (except it's not fully uppercase).
            if (!Enum.TryParse(dataTable.Rows[0].Field<string>("EVENT_MANIPULATION"), true, out TriggerEvents triggerEvent))
            {
                return result;
            }

            var triggerDefinition = dataTable.Rows[0].Field<string>("ACTION_STATEMENT")?.Trim() ?? String.Empty;
            if (!String.IsNullOrWhiteSpace(triggerDefinition))
            {
                // Strip the "BEGIN" and "END" parts of the definition (Wiser doesn't need them).
                if (triggerDefinition.StartsWith("BEGIN", StringComparison.OrdinalIgnoreCase))
                {
                    triggerDefinition = triggerDefinition[5..].Trim();
                }

                if (triggerDefinition.EndsWith("END", StringComparison.OrdinalIgnoreCase))
                {
                    triggerDefinition = triggerDefinition[..^3].Trim();
                }
            }

            result.Name = dataTable.Rows[0].Field<string>("TRIGGER_NAME") ?? String.Empty;
            result.EditorValue = triggerDefinition;
            result.TriggerTableName = dataTable.Rows[0].Field<string>("EVENT_OBJECT_TABLE") ?? String.Empty;
            result.TriggerTiming = triggerTiming;
            result.TriggerEvent = triggerEvent;

            return result;
        }

        /// <summary>
        /// Will attempt to create a VIEW in the client's database.
        /// </summary>
        /// <param name="viewName">The name of the template, which will server as the name of the view.</param>
        /// <param name="viewDefinition">The select statement of the view.</param>
        /// <param name="databaseSchema">The database schema in which to create/replace the view.</param>
        /// <param name="oldViewName">Optional: The old name of the view when the view is being renamed.</param>
        /// <returns><see langword="true"/> if the view was successfully created; otherwise, <see langword="false"/>.</returns>
        private async Task<(bool successful, string ErrorMessage)> CreateOrReplaceDatabaseViewAsync(string viewName, string viewDefinition, string databaseSchema, string oldViewName = null)
        {
            // If the view is being renamed, the oldViewName parameter will contain the current name of the view.
            // It should be dropped, otherwise the view will exist with both the new and the old name.
            if (!String.IsNullOrWhiteSpace(oldViewName))
            {
                await clientDatabaseConnection.ExecuteAsync($"DROP VIEW IF EXISTS `{databaseSchema}`.`{oldViewName}`;");
            }

            // Build the query that will created the view.
            var viewQueryBuilder = new StringBuilder();
            viewQueryBuilder.AppendLine($"CREATE OR REPLACE SQL SECURITY INVOKER VIEW `{databaseSchema}`.`{viewName}` AS");
            viewQueryBuilder.AppendLine(viewDefinition);

            var viewQuery = viewQueryBuilder.ToString();

            try
            {
                // Execute the query. No need to drop old view first, the "CREATE OR REPLACE" part takes care of that.
                await clientDatabaseConnection.ExecuteAsync(viewQuery);
                return (true, String.Empty);
            }
            catch (MySqlException mySqlException)
            {
                // Only the message of the MySQL exception should be enough to determine what went wrong.
                return (false, mySqlException.Message);
            }
            catch (Exception exception)
            {
                // Other exceptions; return entire exception.
                return (false, exception.ToString());
            }
        }

        /// <summary>
        /// Will attempt to create a FUNCTION or PROCEDURE in the client's database.
        /// </summary>
        /// <param name="routineName">The name of the template, which will serve as the name of the routine.</param>
        /// <param name="routineType">The type of the routine, which should be either <see cref="RoutineTypes.Function"/> or <see cref="RoutineTypes.Procedure"/>.</param>
        /// <param name="parameters">A string that represent the input parameters. For procedures, OUT and INOUT parameters can also be defined.</param>
        /// <param name="returnType">The data type that is expected. This is only if <paramref name="routineType"/> is set to <see cref="RoutineTypes.Function"/>.</param>
        /// <param name="routineDefinition">The body of the routine.</param>
        /// <param name="databaseSchema">The database schema in which to create/replace the routine.</param>
        /// <param name="oldRoutineName">Optional: The old name of the routine when the routing is being renamed.</param>
        /// <returns><see langword="true"/> if the routine was successfully created; otherwise, <see langword="false"/>.</returns>
        private async Task<(bool Successful, string ErrorMessage)> CreateOrReplaceDatabaseRoutineAsync(string routineName, RoutineTypes routineType, string parameters, string returnType, string routineDefinition, string databaseSchema, string oldRoutineName = null)
        {
            if (routineType == RoutineTypes.Unknown)
            {
                return (false, "Routine type 'Unknown' is not a valid routine type.");
            }

            // If the routine is being renamed, the oldRoutineName parameter will contain the current name of the routine.
            // It should be dropped, otherwise the routine will exist with both the new and the old name.
            if (!String.IsNullOrWhiteSpace(oldRoutineName))
            {
                // Drop the old routine if it exists.
                await clientDatabaseConnection.ExecuteAsync($"DROP FUNCTION IF EXISTS `{databaseSchema}`.`{oldRoutineName}`; DROP PROCEDURE IF EXISTS `{databaseSchema}`.`{oldRoutineName}`;");
            }

            // Check if routine exists.
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("routineName", routineName);
            clientDatabaseConnection.AddParameter("databaseSchema", databaseSchema);
            var getRoutineData = await clientDatabaseConnection.GetAsync(@"
SELECT COUNT(*) > 0 AS routine_exists
FROM information_schema.ROUTINES
WHERE ROUTINE_SCHEMA = ?databaseSchema
AND ROUTINE_NAME = ?routineName");

            var routineExists = getRoutineData.Rows.Count > 0 && Convert.ToBoolean(getRoutineData.Rows[0]["routine_exists"]);

            // Only FUNCTION routines directly return data.
            var returnsPart = routineType == RoutineTypes.Function ? $" RETURNS {returnType}" : String.Empty;

            if (!routineDefinition.Trim().EndsWith(";"))
            {
                routineDefinition = $"{routineDefinition.Trim()};";
            }

            // Create local function for creating the query to avoid having to create separate query builders.
            string CreateQuery(string routineNameInQuery)
            {
                var routineQueryBuilder = new StringBuilder();
                routineQueryBuilder.AppendLine($"CREATE DEFINER=CURRENT_USER {routineType.ToString("G").ToUpper()} `{databaseSchema}`.`{routineNameInQuery}` ({parameters}){returnsPart}");
                routineQueryBuilder.AppendLine("    SQL SECURITY INVOKER");
                routineQueryBuilder.AppendLine("BEGIN");
                routineQueryBuilder.AppendLine("##############################################################################");
                routineQueryBuilder.AppendLine("# NOTE: This routine was created in Wiser! Do not edit directly in database! #");
                routineQueryBuilder.AppendLine("##############################################################################");
                routineQueryBuilder.AppendLine(routineDefinition);
                routineQueryBuilder.AppendLine("END");
                return routineQueryBuilder.ToString();
            }

            var routineQuery = CreateQuery(routineName);

            try
            {
                // If the routine already exists, try to create a new one with a temporary name to check if creating the routine will not fail.
                // That way, the old routine will remain intact.
                if (routineExists)
                {
                    var tempRoutineName = $"{routineName}_temp";
                    var tempRoutineQuery = CreateQuery(tempRoutineName);

                    await clientDatabaseConnection.ExecuteAsync($"DROP FUNCTION IF EXISTS `{databaseSchema}`.`{tempRoutineName}`; DROP PROCEDURE IF EXISTS `{databaseSchema}`.`{tempRoutineName}`;");
                    await clientDatabaseConnection.ExecuteAsync(tempRoutineQuery);
                }

                // Temp routine creation succeeded. Drop temp routine and current routine, and create the new routine.
                await clientDatabaseConnection.ExecuteAsync($"DROP FUNCTION IF EXISTS `{databaseSchema}`.`{routineName}_temp`; DROP PROCEDURE IF EXISTS `{databaseSchema}`.`{routineName}_temp`; DROP FUNCTION IF EXISTS `{databaseSchema}`.`{routineName}`; DROP PROCEDURE IF EXISTS `{databaseSchema}`.`{routineName}`;");
                await clientDatabaseConnection.ExecuteAsync(routineQuery);

                return (true, String.Empty);
            }
            catch (MySqlException mySqlException)
            {
                // Remove temporary routine if it was created (it has no use anymore).
                await clientDatabaseConnection.ExecuteAsync($"DROP FUNCTION IF EXISTS `{databaseSchema}`.`{routineName}_temp`; DROP PROCEDURE IF EXISTS `{databaseSchema}`.`{routineName}_temp`;");
                // Only the message of the MySQL exception should be enough to determine what went wrong.
                return (false, mySqlException.Message);
            }
            catch (Exception exception)
            {
                // Other exceptions; return entire exception.
                return (false, exception.ToString());
            }
        }

        /// <summary>
        /// Will attempt to create a TRIGGER in the client's database.
        /// </summary>
        /// <param name="triggerName">The name of the template, which will serve as the name of the trigger.</param>
        /// <param name="triggerTiming">The timing of the trigger, which should be either <see cref="TriggerTimings.After"/> or <see cref="TriggerTimings.Before"/>.</param>
        /// <param name="triggerEvent">The event of the trigger, which should be either <see cref="TriggerEvents.Insert"/>, <see cref="TriggerEvents.Update"/>, or  <see cref="TriggerEvents.Delete"/>.</param>
        /// <param name="tableName">The name of the table that the trigger is for.</param>
        /// <param name="triggerDefinition">The body of the trigger.</param>
        /// <param name="databaseSchema">The database schema in which to create/replace the trigger.</param>
        /// <param name="oldTriggerName">Optional: The old name of the trigger when the trigger is being renamed.</param>
        /// <returns><see langword="true"/> if the trigger was successfully created; otherwise, <see langword="false"/>.</returns>
        private async Task<(bool Successful, string ErrorMessage)> CreateOrReplaceDatabaseTriggerAsync(string triggerName, TriggerTimings triggerTiming, TriggerEvents triggerEvent, string tableName, string triggerDefinition, string databaseSchema, string oldTriggerName = null)
        {
            if (triggerTiming == TriggerTimings.Unknown)
            {
                return (false, "Trigger timing 'Unknown' is not a valid trigger timing.");
            }
            if (triggerEvent == TriggerEvents.Unknown)
            {
                return (false, "Trigger event 'Unknown' is not a valid trigger event.");
            }
            if (String.IsNullOrWhiteSpace(tableName))
            {
                return (false, "The table name cannot be null or empty.");
            }

            // If the trigger is being renamed, the oldTriggerName parameter will contain the current name of the trigger.
            // It should be dropped, otherwise the trigger will exist with both the new and the old name.
            if (!String.IsNullOrWhiteSpace(oldTriggerName))
            {
                // Drop the old trigger if it exists.
                await clientDatabaseConnection.ExecuteAsync($"DROP TRIGGER IF EXISTS `{databaseSchema}`.`{oldTriggerName}`;");
            }

            // Check if trigger exists.
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("triggerName", triggerName);
            clientDatabaseConnection.AddParameter("databaseSchema", databaseSchema);
            var getTriggerData = await clientDatabaseConnection.GetAsync(@"SELECT COUNT(*) > 0 AS trigger_exists
FROM information_schema.TRIGGERS
WHERE TRIGGER_SCHEMA = ?databaseSchema
AND TRIGGER_NAME = ?triggerName");

            var triggerExists = getTriggerData.Rows.Count > 0 && Convert.ToBoolean(getTriggerData.Rows[0]["trigger_exists"]);

            if (!triggerDefinition.Trim().EndsWith(";"))
            {
                triggerDefinition = $"{triggerDefinition.Trim()};";
            }

            // Create local function for creating the query to avoid having to create separate query builders.
            string CreateQuery(string triggerNameInQuery)
            {
                var timingAndEvent = $"{triggerTiming.ToString("G").ToUpperInvariant()} {triggerEvent.ToString("G").ToUpperInvariant()}";

                var routineQueryBuilder = new StringBuilder();
                routineQueryBuilder.AppendLine($"CREATE DEFINER=CURRENT_USER TRIGGER `{databaseSchema}`.`{triggerNameInQuery}` {timingAndEvent} ON `{databaseSchema}`.`{tableName}` FOR EACH ROW BEGIN");
                routineQueryBuilder.AppendLine("##############################################################################");
                routineQueryBuilder.AppendLine("# NOTE: This trigger was created in Wiser! Do not edit directly in database! #");
                routineQueryBuilder.AppendLine("##############################################################################");
                routineQueryBuilder.AppendLine(triggerDefinition);
                routineQueryBuilder.AppendLine("END");
                return routineQueryBuilder.ToString();
            }

            var routineQuery = CreateQuery(triggerName);

            try
            {
                // If the trigger already exists, try to create a new one with a temporary name to check if creating the trigger will not fail.
                // That way, the old trigger will remain intact.
                if (triggerExists)
                {
                    var tempTriggerName = $"{triggerName}_temp";
                    var tempRoutineQuery = CreateQuery(tempTriggerName);

                    await clientDatabaseConnection.ExecuteAsync($"DROP TRIGGER IF EXISTS `{databaseSchema}`.`{tempTriggerName}`;");
                    await clientDatabaseConnection.ExecuteAsync(tempRoutineQuery);
                }

                // Temp trigger creation succeeded. Drop temp trigger and current trigger, and create the new trigger.
                await clientDatabaseConnection.ExecuteAsync($"DROP TRIGGER IF EXISTS `{databaseSchema}`.`{triggerName}_temp`; DROP TRIGGER IF EXISTS `{databaseSchema}`.`{triggerName}`;");
                await clientDatabaseConnection.ExecuteAsync(routineQuery);

                return (true, String.Empty);
            }
            catch (MySqlException mySqlException)
            {
                // Remove temporary trigger if it was created (it has no use anymore).
                await clientDatabaseConnection.ExecuteAsync($"DROP TRIGGER IF EXISTS `{databaseSchema}.`{triggerName}_temp`;");
                // Only the message of the MySQL exception should be enough to determine what went wrong.
                return (false, mySqlException.Message);
            }
            catch (Exception exception)
            {
                // Other exceptions; return entire exception.
                return (false, exception.ToString());
            }
        }

        /// <summary>
        /// Will attempt to minify JavaScript with a NodeJS package called terser.
        /// </summary>
        /// <param name="script">The raw JavaScript that will be minified.</param>
        /// <returns>A <see cref="ValueTuple"/> with the first value being whether the minification was successful, and the minified script.</returns>
        private async Task<(bool Successful, string MinifiedScript)> MinifyJavaScriptWithTerserAsync(string script)
        {
            if (String.IsNullOrWhiteSpace(script))
            {
                return (false, script);
            }

            // Create a temporary file that will contain the script. This is required because terser can only work with input files.
            var uploadsDirectory = Path.Combine(webHostEnvironment.ContentRootPath, "temp/minify");
            if (!Directory.Exists(uploadsDirectory))
            {
                Directory.CreateDirectory(uploadsDirectory);
            }

            var filePath = Path.Combine(uploadsDirectory, $"{Guid.NewGuid().ToString()}.js");
            await File.WriteAllTextAsync(filePath, script, Encoding.UTF8);

            string output = null;
            var successful = true;

            // Windows requires the ".cmd" file to run, while UNIX-based systems can just use the "terser" file (without an extension).
            var terserCommand = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "terser.cmd" : "terser";

            try
            {
                using var process = new Process();
                process.StartInfo.FileName = Path.Combine(webHostEnvironment.ContentRootPath, $"node_modules/.bin/{terserCommand}");
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.Arguments = $"{filePath} --compress --mangle";

                process.OutputDataReceived += (_, eventArgs) =>
                {
                    // eventArgs.Data holds the response from terser.
                    if (!String.IsNullOrWhiteSpace(eventArgs.Data))
                    {
                        output = eventArgs.Data;
                    }
                };

                process.ErrorDataReceived += (_, eventArgs) =>
                {
                    successful = false;

                    var message = !String.IsNullOrWhiteSpace(eventArgs.Data)
                        ? $"Error trying to minify script with terser: {eventArgs.Data}"
                        : "Unknown error trying to minify script with terser";

                    logger.LogWarning(message);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                using var cancellationTokenSource = new CancellationTokenSource(5000);
                await process.WaitForExitAsync(cancellationTokenSource.Token);
            }
            catch (Exception exception)
            {
                successful = false;
                logger.LogWarning(exception, "Error trying to minify script with terser");
            }

            // Remove temporary file afterwards, regardless if it succeeded or not.
            File.Delete(filePath);

            return (successful, !String.IsNullOrWhiteSpace(output) ? output : script);
        }
    }
}