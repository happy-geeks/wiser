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
using Api.Core.Helpers;
using Api.Core.Interfaces;
using Api.Core.Models;
using Api.Core.Services;
using Api.Modules.Customers.Interfaces;
using Api.Modules.Kendo.Enums;
using Api.Modules.Templates.Enums;
using Api.Modules.Templates.Helpers;
using Api.Modules.Templates.Interfaces;
using Api.Modules.Templates.Interfaces.DataLayer;
using Api.Modules.Templates.Models;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.History;
using Api.Modules.Templates.Models.Other;
using Api.Modules.Templates.Models.Template;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
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
        private readonly IWiserCustomersService wiserCustomersService;
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

        /// <summary>
        /// Creates a new instance of TemplatesService.
        /// </summary>
        public TemplatesService(IHttpContextAccessor httpContextAccessor, IWiserCustomersService wiserCustomersService, IStringReplacementsService stringReplacementsService, GeeksCoreLibrary.Modules.Templates.Interfaces.ITemplatesService gclTemplatesService, IDatabaseConnection clientDatabaseConnection, IApiReplacementsService apiReplacementsService, ITemplateDataService templateDataService, IHistoryService historyService, IWiserItemsService wiserItemsService, IPagesService pagesService, IRazorViewEngine razorViewEngine, ITempDataProvider tempDataProvider, IObjectsService objectsService, IDatabaseHelpersService databaseHelpersService, ILogger<TemplatesService> logger, IOptions<GclSettings> gclSettings, IOptions<ApiSettings> apiSettings, IWebHostEnvironment webHostEnvironment)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.wiserCustomersService = wiserCustomersService;
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
            var customer = (await wiserCustomersService.GetSingleAsync(identity)).ModelObject;

            // Set the encryption key for the GCL internally. The GCL can't know which key to use otherwise.
            GclSettings.Current.ExpiringEncryptionKey = customer.EncryptionKey;

            var queryTemplate = GetQueryTemplate(0, templateName);
            queryTemplate.Content = apiReplacementsService.DoIdentityReplacements(queryTemplate.Content, identity, true);
            queryTemplate.Content = stringReplacementsService.DoHttpRequestReplacements(queryTemplate.Content, true);

            if (requestPostData != null && requestPostData.Keys.Any())
            {
                queryTemplate.Content = stringReplacementsService.DoReplacements(queryTemplate.Content, requestPostData, true);
            }

            var result = await gclTemplatesService.GetJsonResponseFromQueryAsync(queryTemplate, customer.EncryptionKey);
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
WHERE (template.use_in_wiser_html_editors = 1 OR template.load_always = 1)
AND template.template_type IN (2, 3)
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
                        "SEARCH_ITEMS_OLD",
                        "GET_ITEM_DETAILS",
                        "GET_DESTINATION_ITEMS",
                        "GET_DESTINATION_ITEMS_REVERSED",
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
                //MYSQL-QUERY TO GENERATE THE CODE TO FILL THE DICTIONARY
                //select CONCAT('TemplateQueryStrings.Add("', name, '", @"', REPLACE(template, '"', '""'), '");') from easy_templates where binary upper(name) = name and COALESCE(trim(name), "") != "" and deleted = 0
                //and version = (select MAX(version) from easy_templates M where M.name = easy_templates.name and M.deleted = 0)    //M.itemid = easy_templates.itemid => is itemid important here?

                //load all the template queries into the dictionary
                TemplateQueryStrings.Add("GET_DATA_FOR_RADIO_BUTTONS", @"SET @_itemId = {itemId};
SET @entityproperty_id = {propertyid};
SET @querytext = (SELECT data_query FROM wiser_entityproperty WHERE id=@entityproperty_id);

PREPARE stmt1 FROM @querytext;
EXECUTE stmt1; #USING @itemid;");
                TemplateQueryStrings.Add("GET_ITEM_HTML_FOR_NEW_ITEMS", @"/********* IMPORTANT NOTE: If you change something in this query, please also change it in the query 'GET_ITEM_HTML' *********/
SET SESSION group_concat_max_len = 1000000;
SET @_moduleId = {moduleId};
SET @_entityType = '{entityType}';

SELECT 
	e.tab_name,
    
	GROUP_CONCAT(
        REPLACE(
            REPLACE(
                REPLACE(
                    REPLACE(
                         REPLACE(
                             REPLACE(
                                 REPLACE(
                                   REPLACE(
                                     REPLACE(
                                       REPLACE(
                                         REPLACE(t.html_template, '{title}', IFNULL(e.display_name,e.property_name))
                                       ,'{options}', IFNULL(e.options, ''))
                                     ,'{hint}', IFNULL(e.explanation,''))
                                   ,'{default_value}', IFNULL(e.default_value, ''))
                                 ,'{propertyId}', CONCAT('NEW_', e.id))
                             ,'{itemId}', 0)
                         ,'{propertyName}', e.property_name)
                    ,'{extraAttribute}', IF(IFNULL(e.default_value, 0) > 0, 'checked', ''))
                ,'{dependsOnField}', IFNULL(e.depends_on_field, ''))
            ,'{dependsOnOperator}', IFNULL(e.depends_on_operator, ''))
        ,'{dependsOnValue}', IFNULL(e.depends_on_value, ''))
       ORDER BY e.tab_name ASC, e.ordering ASC SEPARATOR '') AS html_template,
            
        GROUP_CONCAT(
            REPLACE(
                REPLACE(
                    REPLACE(
                           REPLACE(
                               REPLACE(
                                   REPLACE(
                                       REPLACE(
                                           REPLACE(
                                              REPLACE(t.script_template, '{propertyId}', CONCAT('NEW_', e.id)), 
                                                 '{default_value}', CONCAT(""'"", REPLACE(IFNULL(e.default_value, """"), "","", ""','""), ""'"")
                                              ),
                                       '{options}', IF(e.options IS NULL OR e.options = '', '{}', e.options)),
                                   '{propertyName}', e.property_name),
                               '{itemId}', 0),
                           '{title}', IFNULL(e.display_name, e.property_name)),
                        '{dependsOnField}', IFNULL(e.depends_on_field, '')),
                    '{dependsOnOperator}', IFNULL(e.depends_on_operator, '')),
                '{dependsOnValue}', IFNULL(e.depends_on_value, ''))
           ORDER BY e.tab_name ASC, e.ordering ASC SEPARATOR '') AS script_template
            
FROM wiser_entityproperty e
JOIN wiser_field_templates t ON t.field_type = e.inputtype
WHERE e.module_id = @_moduleId
AND e.entity_name = @_entityType
GROUP BY e.tab_name
ORDER BY e.tab_name ASC, e.ordering ASC");
                TemplateQueryStrings.Add("GET_EMAIL_TEMPLATES", @"# Module ID 64 is MailTemplates
# Using texttypes 60 (subject) and 61 (content)

SELECT
    i.id AS template_id,
    i.`name` AS template_name,
    s.content AS email_subject,
    c.content AS email_content
FROM easy_items i
JOIN item_content s ON s.item_id = i.id AND s.texttype_id = 60
JOIN item_content c ON c.item_id = i.id AND c.texttype_id = 61
WHERE i.moduleid = 64
GROUP BY i.id");
                TemplateQueryStrings.Add("SCHEDULER_GET_TEACHERS", @"SELECT 
	""Kevin Manders"" AS `text`,
    1 AS `value`
UNION
	SELECT 
	""Test Docent 2"" ,
    2
UNION
	SELECT 
	""Test Docent 3"" ,
    3
UNION
	SELECT 
	""Test Docent 4"" ,
    4");
                TemplateQueryStrings.Add("SCHEDULER_UPDATE_FAVORITE", @"# insert ignore to favourite

SET @user_id = '{userId}'; # for now always 1
SET @favorite_id = '{favoriteId}';
SET @search_input = '{search}';
SET @view_type = '{type}';
SET @set_teacher = '{teacher}';
SET @set_category = '{category}';
SET @set_location = '{location}';

INSERT INTO schedule_favorites (user_id, favorite_id, search, view_type, teacher, category, location)
VALUES(
    @user_id, 
    @favorite_id, 
    @search_input, 
    @view_type, 
    @set_teacher, 
    @set_category, 
    @set_location)
ON DUPLICATE KEY UPDATE 
	search = @search_input, 
    view_type = @view_type, 
    teacher = @set_teacher, 
    category = @set_category, 
    location = @set_location;
SELECT 1;");

                TemplateQueryStrings.Add("SET_COMMUNICATION_DATA_SELECTOR", @"SET @_communication_id = {itemId};
SET @_dataselector_id = {dataSelectorId};

UPDATE wiser_communication
SET receiver_selectionid = @_dataselector_id
WHERE id = @_communication_id;

SELECT ROW_COUNT() > 0 AS updateSuccessful;");
                TemplateQueryStrings.Add("GET_ENTITY_TYPES", @"SET @_module_list = IF('{modules}' LIKE '{%}', '', '{modules}');

SELECT DISTINCT `name` AS entityType
FROM wiser_entity
WHERE
    `name` <> ''
    AND IF(@_module_list = '', 1 = 1, FIND_IN_SET(module_id, @_module_list)) > 0
ORDER BY `name`");
                TemplateQueryStrings.Add("CHECK_DATA_SELECTOR_NAME_EXISTS", @"SET @_name = '{name}';

# Will automatically be NULL if it doesn't exist, which is good.
SET @_item_id = (SELECT id FROM wiser_data_selector WHERE `name` = @_name LIMIT 1);

SELECT @_item_id IS NOT NULL AS nameExists;");
                TemplateQueryStrings.Add("GET_ENTITY_PROPERTIES_LINKED_TO", @"################################################
#                                              #
#   NOTE: THIS QUERY IS DEPRECATED!            #
#   USE THE '_DOWN' OR '_UP' VERSION INSTEAD   #
#                                              #
################################################

SET @_module_list = IF('{modules}' LIKE '{%}', '', '{modules}');
SET @_entity_type_list = IF('{entity_types}' LIKE '{%}', '', '{entity_types}');

SELECT
    display_name AS `name`,
    CAST(IF(
        inputtype = 'item-linker',
        JSON_OBJECT(
            'inputType', inputtype,
            'type', `options`->>'$.linkTypeName',
            'entityTypes', (SELECT GROUP_CONCAT(DISTINCT entity_name) FROM wiser_itemlink WHERE type_name = `options`->>'$.linkTypeName'),
            'moduleId', `options`->>'$.moduleId'
        ),
        JSON_OBJECT(
            'inputType', inputtype,
            'type', `options`->>'$.entityType',
            'entityTypes', `options`->>'$.entityType'
        )
    ) AS CHAR) AS `options`
FROM wiser_entityproperty
WHERE
    IF(@_module_list = '', 1 = 1, FIND_IN_SET(module_id, @_module_list))
    AND FIND_IN_SET(entity_name, @_entity_type_list)
    AND inputtype IN ('item-linker', 'sub-entities-grid')
ORDER BY display_name");
                TemplateQueryStrings.Add("GET_WISER_TEMPLATES", @"SELECT
    i.id AS template_id,
    i.title AS template_name,
    '' AS email_subject,
    IF(d.long_value IS NULL OR d.long_value = '', d.`value`, d.long_value) AS email_content
FROM wiser_item i
JOIN wiser_itemdetail d ON d.item_id = i.id AND d.`key` = 'html_template'
WHERE i.entity_type = 'template'
ORDER BY i.title");
                TemplateQueryStrings.Add("GET_PROPERTY_VALUES", @"SELECT wid.`value` AS `text`, wid.`value`
FROM wiser_item wi
JOIN wiser_itemdetail wid ON wid.item_id = wi.id
WHERE wi.entity_type = '{entityName}' AND wid.`key` = '{propertyName}' AND wid.`value` <> ''
GROUP BY wid.`value`
ORDER BY wid.`value`
LIMIT 25");
                TemplateQueryStrings.Add("SAVE_COMMUNICATION_ITEM", @"SET @_item_id = '{id}';
SET @_name = '{name}';
SET @_receiver_list = '{receiverList}';
SET @_send_email = {sendEmail};
SET @_email_templateid = {emailTemplateId};
SET @_send_sms = {sendSms};
SET @_send_whatsapp = {sendWhatsApp};
SET @_create_pdf = {createPdf};
SET @_email_subject = '{emailSubject}';
SET @_email_content = '{emailContent_urldataunescape}';
SET @_email_address_selector = '{emailAddressSelector}';
SET @_sms_content = '{smsContent}';
SET @_phone_number_selector = '{phoneNumberSelector}';
SET @_pdf_templateid = {pdfTemplateId};
SET @_send_trigger = '{sendTrigger}';

SET @_senddate = IF('{sendDate}' LIKE '{%}', NULL, '{sendDate}');
SET @_trigger_start = IF('{triggerStart}' LIKE '{%}', NULL, '{triggerStart}');
SET @_trigger_end = IF('{triggerEnd}' LIKE '{%}', NULL, '{triggerEnd}');
SET @_trigger_time = IF('{triggerTime}' LIKE '{%}', NULL, '{triggerTime}');
SET @_trigger_periodvalue = IF('{triggerPeriodValue}' LIKE '{%}', 0, CAST('{triggerPeriodValue}' AS SIGNED));
SET @_trigger_period = IF('{triggerPeriodValue}' LIKE '{%}', 'day', '{triggerPeriod}');
SET @_trigger_periodbeforeafter = IF('{triggerPeriodBeforeAfter}' LIKE '{%}', 'before', '{triggerPeriodBeforeAfter}');
SET @_trigger_days = IF('{triggerDays}' LIKE '{%}', '', '{triggerDays}');
SET @_trigger_type = IF('{triggerType}' LIKE '{%}', 0, CAST('{triggerType}' AS SIGNED));

# If it's a new item, then item_id should be NULL.
SET @_item_id = IF(@_item_id LIKE '{%}' OR @_item_id = '0', NULL, CAST(@_item_id AS SIGNED));

INSERT INTO wiser_communication (id, `name`, receiver_list, send_email, email_templateid, send_sms, send_whatsapp, create_pdf, `email-subject`, `email-content`, email_address_selector, `sms-content`, phone_number_selector, pdf_templateid, send_trigger, senddate, trigger_start, trigger_end, trigger_time, trigger_periodvalue, trigger_period, trigger_periodbeforeafter, trigger_days, trigger_type)
VALUES(@_item_id, @_name, @_receiver_list, @_send_email, @_email_templateid, @_send_sms, @_send_whatsapp, @_create_pdf, @_email_subject, @_email_content, @_email_address_selector, @_sms_content, @_phone_number_selector, @_pdf_templateid, @_send_trigger, @_senddate, @_trigger_start, @_trigger_end, @_trigger_time, @_trigger_periodvalue, @_trigger_period, @_trigger_periodbeforeafter, @_trigger_days, @_trigger_type)
ON DUPLICATE KEY UPDATE
    receiver_list = VALUES(receiver_list),
    send_email = VALUES(send_email),
    email_templateid = VALUES(email_templateid),
    send_sms = VALUES(send_sms),
    send_whatsapp = VALUES(send_whatsapp),
    create_pdf = VALUES(create_pdf),
    `email-subject` = VALUES(`email-subject`),
    `email-content` = VALUES(`email-content`),
    email_address_selector = VALUES(email_address_selector),
    `sms-content` = VALUES(`sms-content`),
    phone_number_selector = VALUES(phone_number_selector),
    pdf_templateid = VALUES(pdf_templateid),
    send_trigger = VALUES(send_trigger),
    senddate = VALUES(senddate),
    trigger_start = VALUES(trigger_start),
    trigger_end = VALUES(trigger_end),
    trigger_time = VALUES(trigger_time),
    trigger_periodvalue = VALUES(trigger_periodvalue),
    trigger_period = VALUES(trigger_period),
    trigger_periodbeforeafter = VALUES(trigger_periodbeforeafter),
    trigger_days = VALUES(trigger_days),
    trigger_type = VALUES(trigger_type);

SELECT IF(@_item_id IS NULL, LAST_INSERT_ID(), @_item_id) AS newId;");
                TemplateQueryStrings.Add("CHECK_COMMUNICATION_NAME_EXISTS", @"SET @_name = '{name}';

# Will automatically be NULL if it doesn't exist, which is good.
SET @_item_id = (SELECT id FROM wiser_communication WHERE `name` = @_name LIMIT 1);

SELECT IFNULL(@_item_id, 0) AS existingItemId;");
                TemplateQueryStrings.Add("SCHEDULER_FAVORITE_CLEAR", @"SET @user_id = {userId};
SET @favorite_id = {favId};

DELETE FROM schedule_favorites WHERE user_id= @user_id AND favorite_id=@favorite_id LIMIT 1");
                TemplateQueryStrings.Add("SCHEDULER_LOAD_FAVORITES", @"SET @user_id = '{userId}';


SELECT 
	favorite_id AS favoriteId,
    search,
    view_type AS type,
    teacher,
    category,
    location
FROM schedule_favorites WHERE user_id=@user_id;
");
                TemplateQueryStrings.Add("IMPORTEXPORT_GET_ENTITY_NAMES", @"SELECT `name`, module_id AS moduleId
FROM wiser_entity
WHERE `name` <> ''
ORDER BY `name`");
                TemplateQueryStrings.Add("SET_DATA_SELECTOR_REMOVED", @"UPDATE wiser_data_selector
SET removed = 1
WHERE id = {itemId};

SELECT ROW_COUNT() > 0 AS updateSuccessful;");
                TemplateQueryStrings.Add("SET_ORDERING_DISPLAY_NAME", @"SET @_entity_name = {selectedEntityName};
SET @_tab_name = {selectedTabName};
SET @_order = {id};
SET @_display_name = {dislayName};

SET @_tab_name= IF( @_tab_name= ""gegevens"", """", @_tab_name);

UPDATE wiser_entityproperty
SET ordering = @_order
WHERE entity_name = @_entity_name AND tab_name = @_tab_name AND display_name = @_display_name
LIMIT 1");
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
                TemplateQueryStrings.Add("GET_OPTIONS_FOR_DEPENDENCY", @"SELECT DISTINCT entity_name AS entityName, IF(tab_name = """", ""Gegevens"", tab_name) as tabName, display_name AS displayName, property_name AS propertyName FROM wiser_entityproperty
WHERE entity_name = '{entityName}'");

                TemplateQueryStrings.Add("GET_AIS_DASHBOARD_OVERVIEW_DATA", @"SET @totalResults = (SELECT COUNT(*) FROM `ais_dashboard` WHERE DATE(started) >= DATE_SUB(CURDATE(), INTERVAL 30 DAY) AND FIND_IN_SET(color, '{color}'));

SELECT 
	id,
    taskname,
    config,
    friendlyname AS friendlyName,
    DATE_FORMAT(started, '%Y-%m-%d') AS startedDate,
    DATE_FORMAT(started, '%H:%i') AS startedTime,
    SUBTIME(TIME(ended), TIME(started)) AS runtime,
    IFNULL(percentage, 0) AS percentageCompleted,
    IFNULL(result, '') AS result,
    IFNULL(groupname, '') AS groupname,
	color, 
    counter,
    IFNULL(debuginformation, '') AS debugInformation,
    0 AS hasChildren,
    @totalResults AS totalResults
FROM `ais_dashboard`
WHERE DATE(started) >= DATE_SUB(CURDATE(), INTERVAL 30 DAY) 
AND FIND_IN_SET(color, '{color}')
ORDER BY startedDate DESC, startedTime DESC
LIMIT {skip}, {take}");
                TemplateQueryStrings.Add("GET_ALL_INPUT_TYPES", @"SELECT DISTINCT inputtype FROM wiser_entityproperty ORDER BY inputtype");
                TemplateQueryStrings.Add("DELETE_ENTITYPROPERTY", @"DELETE FROM wiser_entityproperty WHERE tab_name = '{tabName}' AND entity_name = '{entityName}' AND id = '{entityPropertyId}'");
                TemplateQueryStrings.Add("GET_ENTITY_PROPERTIES_ADMIN", @"SELECT id, entity_name AS entityName, tab_name AS tabName, display_name AS displayName, ordering FROM wiser_entityproperty
WHERE tab_name = '{tabName}' AND entity_name = '{entityName}'
ORDER BY ordering ASC");
                TemplateQueryStrings.Add("GET_ENTITY_LIST", @"SELECT 
	entity.id,
	IF(entity.name = '', 'ROOT', entity.name) AS name,
	CONCAT(IFNULL(module.name, CONCAT('Module #', entity.module_id)), ' --> ', IFNULL(NULLIF(entity.friendly_name, ''), IF(entity.name = '', 'ROOT', entity.name))) AS displayName,
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
                TemplateQueryStrings.Add("UPDATE_ORDERING_ENTITY_PROPERTY", @"SET @old_index = {oldIndex} + 1;
SET @new_index = {newIndex} +1;
SET @id = {currentId}; 
SET @entity_name = '{entityName}';
SET @tab_name = '{tabName}';

# move property to given index
UPDATE wiser_entityproperty SET ordering = @new_index WHERE id=@id;

# set other items to given index
UPDATE wiser_entityproperty
	SET ordering = IF(@old_index > @new_index, ordering+1, ordering-1) 
WHERE 
	ordering > IF(@old_index > @new_index, @new_index, @old_index) AND
	ordering < IF(@old_index > @new_index, @old_index, @new_index) AND
	entity_name = @entity_name AND 
	tab_name =  @tab_name AND
	id <> @id;

# update record where index equals the new index value
UPDATE wiser_entityproperty
	SET ordering = IF(@old_index > @new_index, ordering+1, ordering-1) 
WHERE 
	ordering = @new_index AND 
	entity_name = @entity_name AND 
	tab_name =  @tab_name AND
	id <> @id;
");
                TemplateQueryStrings.Add("GET_ENTITY_PROPERTIES_TABNAMES", @"SELECT id, IF(tab_name = '', 'Gegevens', tab_name) AS tabName FROM wiser_entityproperty
WHERE entity_name = '{entityName}'
GROUP BY tab_name
ORDER BY tab_name ASC");
                TemplateQueryStrings.Add("GET_ROLES", @"SELECT id AS id, role_name AS roleName FROM wiser_roles
WHERE role_name != ''
ORDER BY role_name ASC;");
                TemplateQueryStrings.Add("INSERT_ROLE", @"INSERT INTO `wiser_roles` (`role_name`) VALUES ('{displayName}');");
                TemplateQueryStrings.Add("DELETE_ROLE", @"DELETE FROM `wiser_roles` WHERE id={roleId}");
                TemplateQueryStrings.Add("GET_ITEMLINK_NAMES", @"SELECT DISTINCT type_name AS type_name_text, type_name AS type_name_value FROM `wiser_itemlink` WHERE type_name <> """" AND type_name IS NOT NULL");
                TemplateQueryStrings.Add("DELETE_RIGHT_ASSIGNMENT", @"DELETE FROM `wiser_permission` 
WHERE role_id = {role_id}
	AND entity_property_id = {entity_id}");
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

                TemplateQueryStrings.Add("GET_UNDERLYING_ENTITY_TYPES", @"#SET @_entity_type_list = IF('{entityTypes}' LIKE '{%}', '', '{entityTypes}');
SET @_entity_name = IF(
    '{entityName}' NOT LIKE '{%}',
    '{entityName}',
    # Check for old query string name.
    IF(
        '{entityTypes}' NOT LIKE '{%}',
        SUBSTRING_INDEX('{entityTypes}', ',', 1),
        ''
    )
);

SELECT inputType, `options`, '' AS acceptedChildTypes
FROM wiser_entityproperty
WHERE entity_name = @_entity_name AND inputtype IN ('item-linker', 'sub-entities-grid')

UNION

SELECT 'sub-entities-grid' AS inputType, '' AS `options`, accepted_childtypes AS acceptedChildTypes
FROM wiser_entity
WHERE name = @_entity_name AND accepted_childtypes <> ''");
                TemplateQueryStrings.Add("GET_PARENT_ENTITY_TYPES", @"#SET @_entity_type_list = IF('{entity_types}' LIKE '{%}', '', '{entity_types}');
SET @_entity_name = IF(
    '{entity_name}' NOT LIKE '{%}',
    '{entity_name}',
    # Check for old query string name.
    IF(
        '{entity_types}' NOT LIKE '{%}',
        SUBSTRING_INDEX('{entity_types}', ',', 1),
        ''
    )
);

SELECT entity_name AS `name`, 'sub-entities-grid' AS inputType, IFNULL(`options`, '') AS `options`
FROM wiser_entityproperty
#WHERE inputtype = 'sub-entities-grid' AND CheckValuesInString(@_entity_name, `options`, '""', '""') = 1
WHERE inputtype = 'sub-entities-grid' AND `options` LIKE CONCAT('%""', @_entity_name, '""%')

UNION

SELECT `name`, 'sub-entities-grid' AS inputType, '' AS `options`
FROM wiser_entity
WHERE FIND_IN_SET(accepted_childtypes, @_entity_name) > 0");
                TemplateQueryStrings.Add("INSERT_NEW_MODULE", @"INSERT INTO `wiser_module` (
	`id`,
    `custom_query`,
    `count_query`
) VALUES (
    {moduleId},
    '',
    ''
);");
                TemplateQueryStrings.Add("CHECK_IF_MODULE_EXISTS", @"SELECT id FROM `wiser_module` WHERE id = {moduleId};");
                TemplateQueryStrings.Add("GET_MODULE_FIELDS", @"SELECT 
    IFNULL(JSON_EXTRACT(`options`, '$.gridViewSettings.columns'), '') AS `fields`
FROM `wiser_module`
WHERE id = {module_id}");
                TemplateQueryStrings.Add("GET_API_ACTION", @"SELECT 
	CASE '{actionType}'
		WHEN 'after_insert' THEN api_after_insert
        WHEN 'after_update' THEN api_after_update
        WHEN 'before_update' THEN api_before_update
        WHEN 'before_delete' THEN api_before_delete
    END AS apiConnectionId_encrypt_withdate
FROM wiser_entity 
WHERE name = '{entityType}';");
                TemplateQueryStrings.Add("UPDATE_API_AUTHENTICATION_DATA", @"UPDATE wiser_api_connection SET authentication_data = '{authenticationData}' WHERE id = {id:decrypt(true)};");
                TemplateQueryStrings.Add("DELETE_MODULE", @"DELETE FROM `wiser_module` WHERE id = {module_id};");
                TemplateQueryStrings.Add("SAVE_MODULE_SETTINGS", @"SET @moduleType := '{module_type}';
SET @moduleOptions := '{options}';

UPDATE `wiser_module` SET 
	custom_query = '{custom_query}',
    count_query = '{count_query}',
    options = NULLIF(@moduleOptions, '')
WHERE id = {module_id};");
                TemplateQueryStrings.Add("INSERT_ENTITYPROPERTY", @"SET @newOrderNr = IFNULL((SELECT MAX(ordering)+1 FROM wiser_entityproperty WHERE entity_name='{entityName}' AND tab_name = '{tabName}'),1);

INSERT INTO wiser_entityproperty(entity_name, tab_name, display_name, property_name, ordering)
VALUES('{entityName}', '{tabName}', '{displayName}', '{propertyName}', @newOrderNr);
#spaties vervangen door underscore");
                TemplateQueryStrings.Add("SEARCH_ITEMS_OLD", @"SET @mid = {moduleid};
SET @parent = '{id:decrypt(true)}';
SET @_entityType = IF('{entityType}' LIKE '{%}', '', '{entityType}');
SET @_searchValue = '{search}';
SET @_searchInTitle = IF('{searchInTitle}' LIKE '{%}' OR '{searchInTitle}' = '1', TRUE, FALSE);
SET @_searchFields = IF('{searchFields}' LIKE '{%}', '', '{searchFields}');
SET @_searchEverywhere = IF('{searchEverywhere}' LIKE '{%}', FALSE, {searchEverywhere});

SELECT 
	i.id,
	i.id AS encryptedId_encrypt_withdate,
	i.title AS name,
	IF(ilc.id IS NULL, 0, 1) AS haschilds,
	we.icon AS spriteCssClass,
	ilp.destination_item_id AS destination_item_id_withdate,
    CASE i.published_environment
    	WHEN 0 THEN 'onzichtbaar'
        WHEN 1 THEN 'dev'
        WHEN 2 THEN 'test'
        WHEN 3 THEN 'acceptatie'
        WHEN 4 THEN 'live'
    END AS published_environment,
    i.entity_type,
    CreateJsonSafeProperty(id.`key`) AS property_name,
    id.`value` AS property_value,
    ilp.type_name AS link_type
FROM wiser_item i
LEFT JOIN wiser_itemlink ilp ON ilp.destination_item_id = @parent AND ilp.item_id = i.id
LEFT JOIN wiser_entityproperty p ON p.entity_name = i.entity_type
LEFT JOIN wiser_itemdetail id ON id.item_id = i.id AND ((p.property_name IS NOT NULL AND p.property_name <> '' AND id.`key` = p.property_name) OR ((p.property_name IS NULL OR p.property_name = '') AND id.`key` = p.display_name))
LEFT JOIN wiser_itemlink ilc ON ilc.destination_item_id = i.id
LEFT JOIN wiser_entity we ON we.name = i.entity_type
WHERE i.removed = 0
AND i.entity_type = @_entityType
AND (@_searchEverywhere = TRUE OR ilp.id IS NOT NULL)
AND (
    (NOT @_searchInTitle AND @_searchFields = '')
    OR (@_searchInTitle AND i.title LIKE CONCAT('%', @_searchValue, '%'))
    OR (@_searchFields <> '' AND FIND_IN_SET(id.key, @_searchFields) AND id.value LIKE CONCAT('%', @_searchValue, '%'))
)

GROUP BY i.id, id.id
ORDER BY ilp.ordering, i.title
#LIMIT {skip}, {take}");
                TemplateQueryStrings.Add("PUBLISH_LIVE", @"UPDATE wiser_item SET published_environment=4 WHERE id={itemid:decrypt(true)};");
                TemplateQueryStrings.Add("PUBLISH_ITEM", @"UPDATE wiser_item SET published_environment=4 WHERE id={itemid:decrypt(true)};");
                TemplateQueryStrings.Add("HIDE_ITEM", @"UPDATE wiser_item SET published_environment=0 WHERE id={itemid:decrypt(true)};");
                TemplateQueryStrings.Add("RENAME_ITEM", @"SET @item_id={itemid:decrypt(true)};
SET @newname='{name}';

UPDATE wiser_item SET title=@newname WHERE id=@item_id LIMIT 1;");
                TemplateQueryStrings.Add("LOAD_USER_SETTING", @"SET @user_id = '{encryptedUserId:decrypt(true)}';
SET @setting_name = '{settingName}';
SET @entity_type = 'wiser_user_settings';

SELECT CONCAT(`value`, long_value) AS `value`
FROM wiser_itemdetail detail
	JOIN wiser_item item ON item.id=detail.item_id AND item.unique_uuid = @user_id AND item.entity_type=@entity_type
WHERE detail.`key` = @setting_name ");
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
                TemplateQueryStrings.Add("SAVE_USER_SETTING", @"SET @user_id = '{encryptedUserId:decrypt(true)}';
SET @setting_name = '{settingName}';
SET @setting_value = '{settingValue}';
SET @entity_type = 'wiser_user_settings';
SET @title = 'Wiser user settings';

SET @itemId = (SELECT id FROM wiser_item item WHERE item.unique_uuid = @user_id AND item.entity_type=@entity_type AND @setting_name <> ''AND @user_id <> '' AND @user_id NOT LIKE '{%}');

# make sure the wiser item exists
INSERT IGNORE INTO wiser_item (id, unique_uuid, entity_type, title)
	VALUES(@itemId, @user_id, @entity_type, @title);

# now update the correct value
INSERT INTO wiser_itemdetail (item_id, `key`, `value`, `long_value`)
	SELECT 
		item.id,
		@setting_name,
		IF(LENGTH(@setting_value > 1000), '', @setting_value),
		IF(LENGTH(@setting_value >= 1000), @setting_value, null)
	FROM wiser_item item WHERE item.unique_uuid = @user_id AND item.entity_type=@entity_type
ON DUPLICATE KEY UPDATE 
	`value` = IF(LENGTH(@setting_value > 1000), '', @setting_value), 
	`long_value` = IF(LENGTH(@setting_value >= 1000), @setting_value, null);
    
SELECT @setting_value;");
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
                TemplateQueryStrings.Add("GET_ALL_MODULES_INFORMATION", @"SELECT 
	id,
	custom_query,
	count_query,
	`options`,
	IF(`options` IS NULL, 0, 1) AS `isGridview`,    
    IF(`options` IS NULL, 'treeview', 'gridview') AS `type`,
# we check for type and isValidJson
    IF(JSON_VALID(`options`) AND `options` IS NOT NULL AND `options` <> '', 1, 0) AS `isValidJson`,
    #adding extra JSON_VALID to prevent errors in the query result.
	IF(JSON_VALID(`options`),IFNULL(JSON_EXTRACT(`options`, '$.gridViewSettings.pageSize'), ''), '') AS `pageSize`,
	IF(JSON_VALID(`options`),IF(JSON_EXTRACT(`options`, '$.gridViewSettings.toolbar.hideCreateButton') = true, 1, 0), 0) AS `hideCreationButton`,
	IF(JSON_VALID(`options`),IF(JSON_EXTRACT(`options`, '$.gridViewSettings.hideCommandColumn') = true, 1, 0), 0) AS `hideCommandButton`
FROM `wiser_module` 
");

                TemplateQueryStrings.Add("GET_AVAILABLE_ENTITY_TYPES", @"SELECT DISTINCT(e2.name)
FROM wiser_entity e
LEFT JOIN wiser_item i ON i.entity_type = e.name AND i.moduleid = e.module_id
JOIN wiser_entity e2 ON e2.module_id = {moduleId} AND e2.name <> '' AND FIND_IN_SET(e2.name, e.accepted_childtypes)
WHERE e.module_id = {moduleId}
AND (({parentId:decrypt(true)} = 0 AND e.name = '') OR ({parentId:decrypt(true)} > 0 AND i.id = {parentId:decrypt(true)}))");

                TemplateQueryStrings.Add("IMPORTEXPORT_GET_LINK_TYPES", @"SELECT type AS id, `name`
FROM wiser_link
ORDER BY `name`");
                TemplateQueryStrings.Add("SAVE_INITIAL_VALUES", @"SET @_entity_name = '{entityName}';
SET @_tab_name = '{tabName}';
SET @_tab_name = IF( @_tab_name='gegevens', '', @_tab_name);
SET @_display_name = '{displayName}';
SET @_property_name = IF('{propertyName}' = '', @_display_name, '{propertyName}');
SET @_overviewvisibility = '{visibleInOverview}';
SET @_overviewvisibility = IF(@_overviewvisibility = TRUE OR @_overviewvisibility = 'true', 1, 0);
SET @_overviewType = '{overviewFieldtype}';
SET @_overviewWidth = '{overviewWidth}';
SET @_groupName = '{groupName}';
SET @_input_type = '{inputtype}';
SET @_explanation = '{explanation}';
SET @_mandatory = '{mandatory}';
SET @_mandatory = IF(@_mandatory = TRUE OR @_mandatory = 'true', 1, 0);
SET @_readOnly = '{readonly}';
SET @_readOnly = IF(@_readOnly = TRUE OR @_readOnly = 'true', 1, 0);
SET @_seo = '{alsoSaveSeoValue}';
SET @_seo = IF(@_seo = TRUE OR @_seo = 'true', 1, 0);
SET @_width = '{width}';
SET @_height = '{height}';
SET @_langCode = '{languageCode}';
SET @_dependsOnField = '{dependsOnField}';
SET @_dependsOnOperator = IF('{dependsOnOperator}' = '', NULL, '{dependsOnOperator}');
SET @_dependsOnValue = '{dependsOnValue}';
SET @_css = '{css}';
SET @_regexValidation = '{regexValidation}';
SET @_defaultValue = '{defaultValue}';
SET @_automation = '{automation}';
SET @_customScript = '{customScript}';
SET @_options = '{options}';
SET @_data_query = '{dataQuery}';
SET @_grid_delete_query = '{gridDeleteQuery}';
SET @_grid_insert_query = '{gridInsertQuery}';
SET @_grid_update_query = '{gridUpdateQuery}';

SET @_id = {id};

UPDATE wiser_entityproperty
SET 
inputtype = @_input_type,
display_name = @_display_name,
property_name = @_property_name,
visible_in_overview= @_overviewvisibility,
overview_fieldtype= @_overviewType,
overview_width= @_overviewWidth,
group_name= @_groupName,
explanation= @_explanation,
regex_validation= @_regexValidation,
mandatory= @_mandatory,
readonly= @_readOnly,
default_value= @_defaultValue,
automation= @_automation,
css= @_css,
width= @_width,
height= @_height,
depends_on_field= @_dependsOnField,
depends_on_operator= @_dependsOnOperator,
depends_on_value= @_dependsOnValue,
language_code= @_langCode,
custom_script= @_customScript,
also_save_seo_value = @_seo,
tab_name = @_tab_name,
options = @_options,
data_query = @_data_query,
grid_delete_query = @_grid_delete_query, 
grid_insert_query= @_grid_insert_query,
grid_update_query = @_grid_update_query
WHERE entity_name = @_entity_name AND id = @_id
LIMIT 1; ");

                TemplateQueryStrings.Add("GET_ENTITY_FIELD_PROPERTIES_FOR_SELECTED", @"SELECT
id,
display_name AS displayName,
inputtype, 
visible_in_overview AS visibleInOverview, 
overview_width AS overviewWidth, 
overview_fieldtype AS overviewFieldtype, 
mandatory, 
readonly, 
also_save_seo_value AS alsoSaveSeoValue,
width,
height,
IF(tab_name = '', 'Gegevens', tab_name) AS tabName,
group_name AS groupName,
property_name AS propertyName,
explanation,
default_value AS defaultValue,
automation,
options,
depends_on_field AS dependsOnField,
depends_on_operator AS dependsOnOperator,
depends_on_value AS dependsOnValue,
IFNULL(data_query,"""") AS dataQuery,
IFNULL(grid_delete_query,"""") AS gridDeleteQuery,
IFNULL(grid_update_query,"""") AS gridUpdateQuery,
IFNULL(grid_insert_query,"""") AS gridInsertQuery,
regex_validation AS regexValidation,
IFNULL(css, '') AS css,
language_code AS languageCode,
IFNULL(custom_script, '') AS customScript,
(SELECT COUNT(1) FROM wiser_itemdetail INNER JOIN wiser_item ON wiser_itemdetail.item_id = wiser_item.id WHERE wiser_itemdetail.key = wiser_entityproperty.property_name AND wiser_item.entity_type = wiser_entityproperty.entity_name) > 0 as field_in_use
FROM wiser_entityproperty
WHERE id = {id} AND entity_name = '{entityName}'
");
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
                TemplateQueryStrings.Add("GET_ITEMLINK_NUMBERS", @"SELECT 'Hoofdkoppeling' AS type_text, 1 AS type_value 
UNION ALL
SELECT 'Subkoppeling' AS type_text, 2 AS type_value 
UNION ALL
SELECT 'Automatisch gegeneerd' AS type_text, 3 AS type_value 
UNION ALL
SELECT 'Media' AS type_text, 4 AS type_value 
UNION ALL
SELECT DISTINCT type AS type_text, type AS type_value FROM `wiser_itemlink` WHERE type > 100");
                TemplateQueryStrings.Add("GET_COLUMNS_FOR_FIELD_TABLE", @"#Verkrijg de kolommen die getoond moeten worden bij een specifiek soort entiteit
SET @entitytype = '{entity_type}';
SET @_linkType = '{linkType}';

SELECT  
	CONCAT('property_.', CreateJsonSafeProperty(LOWER(IF(p.property_name IS NULL OR p.property_name = '', p.display_name, p.property_name)))) AS field,
    p.display_name AS title,
    p.overview_fieldtype AS fieldType,
    p.overview_width AS width
FROM wiser_entityproperty p 
WHERE (p.entity_name = @entitytype OR (p.link_type > 0 AND p.link_type = @_linkType))
AND p.visible_in_overview = 1
GROUP BY IF(p.property_name IS NULL OR p.property_name = '', p.display_name, p.property_name)
ORDER BY p.ordering;");
                TemplateQueryStrings.Add("GET_COLUMNS_FOR_LINK_TABLE", @"SET @destinationId = {id:decrypt(true)};
SET @_linkTypeNumber = IF('{linkTypeNumber}' LIKE '{%}' OR '{linkTypeNumber}' = '', '2', '{linkTypeNumber}');

SELECT 
	CONCAT('property_.', CreateJsonSafeProperty(IF(p.property_name IS NULL OR p.property_name = '', p.display_name, p.property_name))) AS field,
    p.display_name AS title,
    p.overview_fieldtype AS fieldType,
    p.overview_width AS width
FROM wiser_entityproperty p 
JOIN wiser_item i ON i.entity_type = p.entity_name
JOIN wiser_itemlink il ON il.item_id = i.id AND il.destination_item_id = @destinationId AND il.type = @_linkTypeNumber
WHERE p.visible_in_overview = 1
GROUP BY IF(p.property_name IS NULL OR p.property_name = '', p.display_name, p.property_name)
ORDER BY p.ordering;");
                TemplateQueryStrings.Add("GET_ENTITY_PROPERTIES_BY_LINK", @"SET @_link_type = IF('{linktype}' LIKE '{%}', '', '{linktype}');
SET @_entity_type = IF('{entitytype}' LIKE '{%}', '', '{entitytype}');

SELECT
    CONCAT_WS(' - ', wip.entity_name, NULLIF(wip.tab_name, ''), NULLIF(wip.group_name, ''), wip.display_name) AS `text`,
    IF(property_name = '', CreateJsonSafeProperty(display_name), property_name) AS `value`
FROM wiser_itemlink wil
JOIN wiser_entityproperty wip ON wip.entity_name = wil.entity_name
WHERE wil.type = @_link_type
# Some entities should be ignored due to their input types.
AND wip.inputtype NOT IN ('sub-entities-grid', 'item-linker', 'linked-item', 'auto-increment', 'file-upload', 'action-button')
GROUP BY wip.id");
                TemplateQueryStrings.Add("GET_ENTITY_PROPERTIES_LINKED_TO_UP", @"# Bovenliggende objecten.

SET @_module_list = IF('{modules}' LIKE '{%}', '', '{modules}');
SET @_entity_type_list = IF('{entity_types}' LIKE '{%}', '', '{entity_types}');

SELECT *
FROM (
    SELECT
        entity_name AS `name`,
        inputtype AS inputType,
        entity_name AS type,
        IF(
            inputtype = 'item-linker',
            (SELECT GROUP_CONCAT(DISTINCT entity_name) FROM wiser_itemlink WHERE type = `options`->>'$.linkTypeNumber'),
            entity_name
        ) AS entityTypes,
        IF(inputtype = 'item-linker', module_id, 0) AS moduleId
    FROM wiser_entityproperty
    WHERE
        IF(@_module_list = '', 1 = 1, FIND_IN_SET(module_id, @_module_list) > 0)
        AND inputtype IN ('item-linker', 'sub-entities-grid')
        AND IF(
            inputtype = 'item-linker',
            JSON_UNQUOTE(JSON_EXTRACT(`options`, JSON_UNQUOTE(JSON_SEARCH(`options`, 'one', @_entity_type_list)))) IS NOT NULL,
            FIND_IN_SET(`options`->>'$.entityType', @_entity_type_list) > 0
        )

    UNION

    SELECT
        wep.entity_name AS `name`,
        wep.inputtype AS inputType,
        wep.entity_name AS type,
        IF(
            wep.inputtype = 'item-linker',
            (SELECT GROUP_CONCAT(DISTINCT entity_name) FROM wiser_itemlink WHERE type = wep.`options`->>'$.linkTypeNumber'),
            entity_name
        ) AS entityTypes,
        IF(inputtype = 'item-linker', wep.module_id, 0) AS moduleId
    FROM wiser_entity we
	JOIN wiser_entityproperty wep ON wep.entity_name = we.`name` AND wep.inputtype IN ('item-linker', 'sub-entities-grid')
    WHERE
        IF(@_module_list = '', 1 = 1, FIND_IN_SET(we.module_id, @_module_list) > 0)
        AND CompareLists(@_entity_type_list, we.accepted_childtypes)
) t
GROUP BY t.type
ORDER BY t.type");
                TemplateQueryStrings.Add("GET_ENTITY_PROPERTIES_LINKED_TO_DOWN", @"# Onderliggende objecten.

SET @_module_list = IF('{modules}' LIKE '{%}', '', '{modules}');
SET @_entity_type_list = IF('{entity_types}' LIKE '{%}', '', '{entity_types}');

SELECT *
FROM (
    SELECT
        #display_name AS `name`,
        CAST(IF(inputtype = 'item-linker', `options`->>'$.linkTypeNumber', `options`->>'$.entityType') AS CHAR) AS `name`,
        inputtype AS inputType,
        CAST(IF(inputtype = 'item-linker', `options`->>'$.linkTypeNumber', `options`->>'$.entityType') AS CHAR) AS type,
        IF(
            inputtype = 'item-linker',
            IF(
                `options`->>'$.entityTypes' IS NULL,
                `options`->>'$.linkTypeNumber',
                REPLACE(REPLACE(REPLACE(REPLACE(`options`->> '$.entityTypes', '[', ''), ']', ''), '""', '' ), ', ', ',')
            ),
            `options`->>'$.entityType'
        ) AS entityTypes,
        IF(inputtype = 'item-linker', `options`->>'$.moduleId', 0) AS moduleId
    FROM wiser_entityproperty
    WHERE
        IF(@_module_list = '', 1 = 1, FIND_IN_SET(module_id, @_module_list) > 0)
        AND FIND_IN_SET(entity_name, @_entity_type_list) > 0
        AND inputtype IN ('item-linker', 'sub-entities-grid')

    UNION

    SELECT
        #wep.display_name AS `name`,
        CAST(IF(wep.inputtype = 'item-linker', wep.`options`->>'$.linkTypeNumber', wep.`options`->>'$.entityType') AS CHAR) AS `name`,
        wep.inputtype AS inputType,
        CAST(IF(wep.inputtype = 'item-linker', wep.`options`->>'$.linkTypeNumber', wep.`options`->>'$.entityType') AS CHAR) AS type,
        IF(
            wep.inputtype = 'item-linker',
            IF(
                wep.`options`->>'$.entityTypes' IS NULL,
                wep.`options`->>'$.linkTypeNumber',
                REPLACE(REPLACE(REPLACE(REPLACE(wep.`options`->> '$.entityTypes', '[', ''), ']', ''), '""', '' ), ', ', ',')
            ),
            wep.`options`->>'$.entityType'
        ) AS entityTypes,
        IF(wep.inputtype = 'item-linker', wep.`options`->>'$.moduleId', 0) AS moduleId
    FROM wiser_entity we
    JOIN wiser_entityproperty wep ON wep.inputtype IN ('item-linker', 'sub-entities-grid')
    WHERE
        FIND_IN_SET(wep.entity_name, we.accepted_childtypes) > 0
        AND IF(@_module_list = '', 1 = 1, FIND_IN_SET(wep.module_id, @_module_list) > 0)
) t
GROUP BY t.type
ORDER BY t.type");

                TemplateQueryStrings.Add("GET_MODULES", @"SELECT id, name as moduleName
FROM wiser_module
ORDER BY name ASC;
");
                TemplateQueryStrings.Add("GET_MODULE_ROLES", @"
SELECT
	permission.id AS `permission_id`,
	role.id AS `role_id`,
	role.role_name,
	module.id AS `module_id`,
	module.name AS module_name
FROM wiser_roles AS role
LEFT JOIN wiser_permission AS permission ON role.id = permission.role_id
LEFT JOIN wiser_module AS module ON permission.module_id = module.id
WHERE role.id = {role_id}");
                TemplateQueryStrings.Add("DELETE_MODULE_RIGHT_ASSIGNMENT", @"DELETE FROM `wiser_system`.`wiser_permission`
WHERE role_id = {role_id} AND module_id={module_id}");

                TemplateQueryStrings.Add("GET_ROLE_RIGHTS", @"SELECT
	properties.id AS `propertyId`,
	properties.entity_name AS `entityName`,
	properties.display_name as `displayName`,
	IFNULL(permissions.permissions, 15) AS `permission`,
    {roleId} AS `roleId`
FROM `wiser_entityproperty` AS properties
LEFT JOIN `wiser_permission` AS permissions ON permissions.entity_property_id = properties.id AND permissions.role_id = {roleId}
WHERE NULLIF(properties.display_name, '') IS NOT NULL
	AND NULLIF(properties.entity_name, '') IS NOT NULL
GROUP BY properties.id
ORDER BY properties.entity_name, properties.display_name");
                TemplateQueryStrings.Add("GET_MODULE_PERMISSIONS", @"SELECT
	role.id AS `roleId`,
	role.role_name AS `roleName`,
	module.id AS `moduleId`,
	IFNULL(module.name, CONCAT('ModuleID: ',module.id)) AS `moduleName`,
	IFNULL(permission.permissions, 15) AS `permission`
FROM wiser_module AS module
JOIN wiser_roles AS role ON role.id = {roleId}
LEFT JOIN wiser_permission AS permission ON role.id = permission.role_id AND permission.module_id = module.id
ORDER BY moduleName ASC
");
                TemplateQueryStrings.Add("UPDATE_MODULE_PERMISSION", @" INSERT INTO `wiser_permission` (
     `role_id`,
     `entity_name`,
     `item_id`,
     `entity_property_id`,
     `permissions`,
     `module_id`
 ) 
 VALUES (
     {roleId}, 
     '',
     0,
     0,
     {permissionCode},
     {moduleId}
 )
ON DUPLICATE KEY UPDATE permissions = {permissionCode};");

                TemplateQueryStrings.Add("GET_DATA_SELECTOR_BY_ID", @"SET @_id = {id};

SELECT id, `name`, module_selection AS modules, request_json AS requestJson, saved_json AS savedJson, show_in_export_module AS showInExportModule, available_for_rendering AS availableForRendering
FROM wiser_data_selector
WHERE id = @_id");

                TemplateQueryStrings.Add("GET_ALL_ENTITY_TYPES", @"SET @userId = {encryptedUserId:decrypt(true)};

SELECT DISTINCT 
	IF(entity.friendly_name IS NULL OR entity.friendly_name = '', entity.name, entity.friendly_name) AS name,
    entity.name AS value
FROM wiser_entity AS entity

# Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
LEFT JOIN wiser_user_roles user_role ON user_role.user_id = @userId
LEFT JOIN wiser_permission permission ON permission.role_id = user_role.role_id AND permission.entity_name = entity.name

WHERE entity.show_in_search = 1
AND entity.name <> ''
AND (permission.id IS NULL OR (permission.permissions & 1) > 0)
ORDER BY IF(entity.friendly_name IS NULL OR entity.friendly_name = '', entity.name, entity.friendly_name)");
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
                TemplateQueryStrings.Add("SEARCH_ITEMS", @"SET @mid = {moduleid};
SET @parent = '{id:decrypt(true)}';
SET @_entityType = IF('{entityType}' LIKE '{%}', '', '{entityType}');
SET @_searchValue = '{search}';
SET @_searchInTitle = IF('{searchInTitle}' LIKE '{%}' OR '{searchInTitle}' = '1', TRUE, FALSE);
SET @_searchFields = IF('{searchFields}' LIKE '{%}', '', '{searchFields}');
SET @_searchEverywhere = IF('{searchEverywhere}' LIKE '{%}', FALSE, {searchEverywhere});

SELECT 
	i.id,
	i.id AS encryptedId_encrypt_withdate,
	i.title AS name
FROM wiser_item i
LEFT JOIN wiser_itemlink ilp ON ilp.destination_item_id = @parent AND ilp.item_id = i.id
LEFT JOIN wiser_itemdetail id ON id.item_id = i.id
LEFT JOIN wiser_itemlink ilc ON ilc.destination_item_id = i.id
LEFT JOIN wiser_entity we ON we.name = i.entity_type

# Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
LEFT JOIN wiser_user_roles user_role ON user_role.user_id = @userId
LEFT JOIN wiser_permission permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id

WHERE (permission.id IS NULL OR (permission.permissions & 1) > 0)
AND i.entity_type = @_entityType
AND (@_searchEverywhere = TRUE OR ilp.id IS NOT NULL)
AND (
    (NOT @_searchInTitle AND @_searchFields = '')
    OR (@_searchInTitle AND i.title LIKE CONCAT('%', @_searchValue, '%'))
    OR (@_searchFields <> '' AND FIND_IN_SET(id.key, @_searchFields) AND id.value LIKE CONCAT('%', @_searchValue, '%'))
)

GROUP BY i.id
ORDER BY ilp.ordering, i.title");
                TemplateQueryStrings.Add("GET_DESTINATION_ITEMS", @"SET @_itemId = {itemId};
SET @_entityType = IF('{entity_type}' LIKE '{%}', 'item', '{entity_type}');
SET @_linkType = IF('{linkTypeNumber}' LIKE '{%}', '1', '{linkTypeNumber}');
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
	id.`key` AS property_name,
	CONCAT(IFNULL(id.`value`, ''), IFNULL(id.`long_value`, '')) AS property_value,
	il.type AS link_type, 
    il.id AS link_id
FROM wiser_itemlink il
JOIN wiser_item i ON i.id = il.destination_item_id AND i.entity_type = @_entityType

# Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
LEFT JOIN wiser_user_roles user_role ON user_role.user_id = @userId
LEFT JOIN wiser_permission permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id

LEFT JOIN wiser_entityproperty p ON p.entity_name = i.entity_type
LEFT JOIN wiser_itemdetail id ON id.item_id = il.destination_item_id AND ((p.property_name IS NOT NULL AND p.property_name <> '' AND id.`key` = p.property_name) OR ((p.property_name IS NULL OR p.property_name = '') AND id.`key` = p.display_name))
WHERE il.item_id = @_itemId
AND il.type = @_linkType
AND (permission.id IS NULL OR (permission.permissions & 1) > 0)
GROUP BY il.item_id, id.id
ORDER BY il.ordering, i.title, i.id");
                TemplateQueryStrings.Add("GET_DESTINATION_ITEMS_REVERSED", @"SET @_itemId = {itemId};
SET @_entityType = IF('{entity_type}' LIKE '{%}', 'item', '{entity_type}');
SET @_linkType = IF('{linkTypeNumber}' LIKE '{%}', '1', '{linkTypeNumber}');
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
	id.`key` AS property_name,
	CONCAT(IFNULL(id.`value`, ''), IFNULL(id.`long_value`, '')) AS property_value,
	il.type AS link_type,
    il.id AS link_id
FROM wiser_itemlink il
JOIN wiser_item i ON i.id = il.item_id AND i.entity_type = @_entityType

# Check permissions. Default permissions are everything enabled, so if the user has no role or the role has no permissions on this item, they are allowed everything.
LEFT JOIN wiser_user_roles user_role ON user_role.user_id = @userId
LEFT JOIN wiser_permission permission ON permission.role_id = user_role.role_id AND permission.item_id = i.id

LEFT JOIN wiser_entityproperty p ON p.entity_name = i.entity_type
LEFT JOIN wiser_itemdetail id ON id.item_id = il.item_id AND ((p.property_name IS NOT NULL AND p.property_name <> '' AND id.`key` = p.property_name) OR ((p.property_name IS NULL OR p.property_name = '') AND id.`key` = p.display_name))
WHERE il.destination_item_id = @_itemId
AND il.type = @_linkType
AND (permission.id IS NULL OR (permission.permissions & 1) > 0)
GROUP BY il.destination_item_id, id.id
ORDER BY il.ordering, i.title, i.id");
                TemplateQueryStrings.Add("GET_COLUMNS_FOR_TABLE", @"SET @selected_id = {itemId:decrypt(true)}; # 3077

SELECT 
	CONCAT('property_.', CreateJsonSafeProperty(LOWER(IF(p.property_name IS NULL OR p.property_name = '', p.display_name, p.property_name)))) AS field,
    p.display_name AS title,
    p.overview_fieldtype AS fieldType,
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
                TemplateQueryStrings.Add("GET_ITEM_FILES_AND_DIRECTORIES", @"SET @parent = IF('{id}' = '' OR '{id}' LIKE '{%}', '{rootId:decrypt(true)}', '{id:decrypt(true)}');

SELECT
	id AS id_encrypt_withdate,
    id AS plainId,
	file_name AS name,
	content_type AS contentType,
	0 AS isDirectory,
	0 AS childrenCount,
    property_name,
    item_id AS itemId_encrypt_withdate,
    item_id AS itemIdPlain,
    CASE
        WHEN content_type LIKE 'image/%' THEN 'image'
        WHEN content_type = 'text/html' THEN 'html'
        ELSE 'file'
    END AS spriteCssClass,
    IF(content_type IN('text/html', 'application/octet-stream'), CONVERT(content USING utf8), '') AS html
FROM wiser_itemfile
WHERE item_id = @parent

UNION ALL

SELECT
	item.id AS id_encrypt_withdate,
    item.id AS plainId,
	item.title AS name,
	'' AS contentType,
	1 AS isDirectory,
	COUNT(DISTINCT subItem.id) + COUNT(DISTINCT file.id) AS childrenCount,
    '' AS property_name,
	item.id AS itemId_encrypt_withdate,
    item.id AS itemIdPlain,
    'wiserfolderclosed' AS spriteCssClass,
    '' AS html
FROM wiser_item AS item
LEFT JOIN wiser_item AS subItem ON subItem.entity_type = 'filedirectory' AND subItem.parent_item_id = item.id
LEFT JOIN wiser_itemfile AS file ON file.item_id = item.id
WHERE item.entity_type = 'filedirectory'
AND item.parent_item_id = @parent
GROUP BY item.id

ORDER BY isDirectory DESC, name ASC");

                TemplateQueryStrings.Add("GET_DATA_FROM_ENTITY_QUERY", @"SET @_itemId = {myItemId};
SET @entityproperty_id = {propertyid};
SET @querytext = (SELECT REPLACE(REPLACE(IFNULL(data_query, 'SELECT 0 AS id, "" AS name'), '{itemId}', @_itemId), '{itemid}', @_itemId) FROM wiser_entityproperty WHERE id=@entityproperty_id);

PREPARE stmt1 FROM @querytext;
EXECUTE stmt1;");
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
        public async Task<ServiceResult<PublishedEnvironmentModel>> GetTemplateEnvironmentsAsync(int templateId)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id cannot be zero.");
            }

            var versionsAndPublished = await templateDataService.GetPublishedEnvironmentsAsync(templateId);

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
            templateDataService.DecryptEditorValueIfEncrypted((await wiserCustomersService.GetEncryptionKey(identity, true)).ModelObject, templateData);

            return new ServiceResult<TemplateSettingsModel>(templateData);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<int>> PublishToEnvironmentAsync(ClaimsIdentity identity, int templateId, int version, string environment, PublishedEnvironmentModel currentPublished)
        {
            if (templateId <= 0)
            {
                throw new ArgumentException("The Id is invalid");
            }

            if (version <= 0)
            {
                throw new ArgumentException("The version is invalid");
            }

            var newPublished = PublishedEnvironmentHelper.CalculateEnvironmentsToPublish(currentPublished, version, environment);

            var publishLog = PublishedEnvironmentHelper.GeneratePublishLog(templateId, currentPublished, newPublished);

            return new ServiceResult<int>(await templateDataService.UpdatePublishedEnvironmentAsync(templateId, newPublished, publishLog, IdentityHelpers.GetUserName(identity, true)));
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> SaveTemplateVersionAsync(ClaimsIdentity identity, TemplateSettingsModel template, bool skipCompilation = false)
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
                        (terserSuccessful, terserMinifiedScript) = await MinifyJavaScriptWithTerser(template.EditorValue);
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
                            logger.LogWarning(exception, $"An error occurred while trying to minify the JavaScript of template ID {template.TemplateId}");
                        }
                    }

                    break;
                case TemplateTypes.Html:
                    template.EditorValue = await wiserItemsService.ReplaceHtmlForSavingAsync(template.EditorValue);
                    template.MinifiedValue = template.EditorValue;
                    break;
                case TemplateTypes.Xml:
                    if (!template.EditorValue.StartsWith("<Configuration>") && !template.EditorValue.StartsWith("<OAuthConfiguration>"))
                    {
                        break;
                    }

                    template.EditorValue = template.EditorValue.EncryptWithAes((await wiserCustomersService.GetEncryptionKey(identity, true)).ModelObject, useSlowerButMoreSecureMethod: true);

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

            if (template.Type == TemplateTypes.Routine)
            {
                // Also (re-)create the actual routine.
                var (successful, errorMessage) = await CreateDatabaseRoutine(template.Name, template.RoutineType, template.RoutineParameters, template.RoutineReturnType, template.EditorValue);
                if (!successful)
                {
                    throw new Exception($"The template saved successfully, but the routine could not be created due to a syntax error. Error:\n{errorMessage}");
                }
            }

            if (template.Type != TemplateTypes.Scss || !template.IsScssIncludeTemplate || skipCompilation)
            {
                return new ServiceResult<bool>(true);
            }

            // Get all SCSS templates that might use the current template and re-compile them, to make sure they will include the latest version of this template.
            var templates = await templateDataService.GetScssTemplatesThatAreNotIncludesAsync(template.TemplateId);
            foreach (var otherTemplate in templates)
            {
                await SaveTemplateVersionAsync(identity, otherTemplate);
            }

            return new ServiceResult<bool>(true);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<TemplateTreeViewModel>>> GetTreeViewSectionAsync(int parentId)
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

            // Make sure the ordering is correct.
            await templateDataService.FixTreeViewOrderingAsync(parentId);

            // Get templates in correct order.
            var rawSection = await templateDataService.GetTreeViewSectionAsync(parentId);
            var helper = new TreeViewHelper();
            var convertedList = rawSection.Select(treeViewDao => helper.ConvertTemplateTreeViewDAOToTemplateTreeViewModel(treeViewDao)).ToList();

            return new ServiceResult<List<TemplateTreeViewModel>>(convertedList);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<SearchResultModel>>> SearchAsync(ClaimsIdentity identity, string searchValue)
        {
	        var encryptionKey = (await wiserCustomersService.GetEncryptionKey(identity, true)).ModelObject;
            return new ServiceResult<List<SearchResultModel>>(await templateDataService.SearchAsync(searchValue, encryptionKey));
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TemplateHistoryOverviewModel>> GetTemplateHistoryAsync(ClaimsIdentity identity, int templateId)
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
                dynamicContentHistory.Add(dc, (await historyService.GetChangesInComponentAsync(dc.Id)).ModelObject);
            }

            var overview = new TemplateHistoryOverviewModel
            {
                TemplateId = templateId,
                TemplateHistory = await historyService.GetVersionHistoryFromTemplate(identity, templateId, dynamicContentHistory),
                PublishHistory = await historyService.GetPublishHistoryFromTemplate(templateId),
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

            templateDataResponse.ModelObject.LinkedTemplates = linkedTemplatesResponse.ModelObject;
            templateDataResponse.ModelObject.Name = newName;
            return await SaveTemplateVersionAsync(identity, templateDataResponse.ModelObject);
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

            var templateTrees = (await GetTreeViewSectionAsync(parentId)).ModelObject;
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
        public async Task<ServiceResult<string>> GeneratePreviewAsync(ClaimsIdentity identity, int componentId, GenerateTemplatePreviewRequestModel requestModel)
        {
            var component = requestModel.Components.FirstOrDefault(c => c.Id == componentId);
            if (component == null)
            {
                return new ServiceResult<string>("");
            }

            requestModel.Url ??= HttpContextHelpers.GetBaseUri(httpContextAccessor.HttpContext);
            await SetupGclForPreviewAsync(identity, requestModel);

            var html = await gclTemplatesService.GenerateDynamicContentHtmlAsync(component);
            return new ServiceResult<string>((string)html);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> GeneratePreviewAsync(ClaimsIdentity identity, GenerateTemplatePreviewRequestModel requestModel)
        {
            var outputHtml = requestModel?.TemplateSettings?.EditorValue;
            if (String.IsNullOrWhiteSpace(outputHtml) || requestModel.TemplateSettings.Type != TemplateTypes.Html)
            {
                return new ServiceResult<string>(outputHtml);
            }

            var javascriptTemplates = new List<int>();
            var cssTemplates = new List<int>();
            var externalJavascript = new List<string>();
            var externalCss = new List<string>();

            javascriptTemplates.AddRange(requestModel.TemplateSettings.LinkedTemplates.LinkedJavascript.Select(t => t.TemplateId));
            cssTemplates.AddRange(requestModel.TemplateSettings.LinkedTemplates.LinkedCssTemplates.Select(t => t.TemplateId));
            cssTemplates.AddRange(requestModel.TemplateSettings.LinkedTemplates.LinkedScssTemplates.Select(t => t.TemplateId));

            requestModel.Url ??= HttpContextHelpers.GetBaseUri(httpContextAccessor.HttpContext);
            var queryString = QueryHelpers.ParseQuery(requestModel.Url.Query);
            var ombouw = (!queryString.ContainsKey("ombouw") || !String.Equals(queryString["ombouw"].ToString(), "false", StringComparison.OrdinalIgnoreCase)) && !String.Equals(requestModel.PreviewVariables.FirstOrDefault(v => String.Equals(v.Key, "ombouw", StringComparison.OrdinalIgnoreCase))?.Value, "false", StringComparison.OrdinalIgnoreCase);

            await SetupGclForPreviewAsync(identity, requestModel);
            
            var contentToWrite = new StringBuilder();

            // Execute the pre load query before any replacements are being done and before any dynamic components are handled.
            await gclTemplatesService.ExecutePreLoadQueryAndRememberResultsAsync(new Template { PreLoadQuery = requestModel.TemplateSettings.PreLoadQuery });

            // Header template.
            if (ombouw)
            {
                contentToWrite.Append(await pagesService.GetGlobalHeader(requestModel.Url.ToString(), javascriptTemplates, cssTemplates));
            }

            // Content template.
            contentToWrite.Append(outputHtml);

            // Footer template.
            if (ombouw)
            {
                contentToWrite.Append(await pagesService.GetGlobalFooter(requestModel.Url.ToString(), javascriptTemplates, cssTemplates));
            }

            outputHtml = contentToWrite.ToString();
            outputHtml = await stringReplacementsService.DoAllReplacementsAsync(outputHtml, null, requestModel.TemplateSettings.HandleRequests, false, true, false);
            outputHtml = await gclTemplatesService.HandleIncludesAsync(outputHtml, false);
            outputHtml = await gclTemplatesService.HandleImageTemplating(outputHtml);
            outputHtml = await gclTemplatesService.ReplaceAllDynamicContentAsync(outputHtml, requestModel.Components);
            outputHtml = stringReplacementsService.EvaluateTemplate(outputHtml);

            if (!ombouw)
            {
                return new ServiceResult<string>(outputHtml);
            }

            // Generate view model.
            var viewModel = await pagesService.CreatePageViewModelAsync(externalCss, cssTemplates, externalJavascript, javascriptTemplates, outputHtml);

            // Determine main domain, using either the "maindomain" object or the "maindomain_wiser" object.
            var mainDomain = await objectsService.FindSystemObjectByDomainNameAsync("maindomain_wiser");
            if (String.IsNullOrWhiteSpace(mainDomain))
            {
                mainDomain = await objectsService.FindSystemObjectByDomainNameAsync("testdomainjuice");
            }
            if (String.IsNullOrWhiteSpace(mainDomain))
            {
                mainDomain = await objectsService.FindSystemObjectByDomainNameAsync("maindomain");
            }

            if (viewModel.Css != null)
            {
                var cssBuilder = new StringBuilder();
                cssBuilder.AppendLine((await gclTemplatesService.GetGeneralTemplateValueAsync(TemplateTypes.Css)).Content);
                cssBuilder.AppendLine(viewModel.Css.PageInlineHeadCss);
                
                var regex = new Regex("/css/gclcss_(.*).css");
                var match = regex.Match(viewModel.Css.PageStandardCssFileName ?? "");
                if (match.Success)
                {
                    var templateIdsList = match.Groups[1].Value.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(Int32.Parse).ToList();
                    cssBuilder.AppendLine((await gclTemplatesService.GetCombinedTemplateValueAsync(templateIdsList, TemplateTypes.Css)).Content);
                }

                viewModel.Css.PageStandardCssFileName = null;
                
                match = regex.Match(viewModel.Css.PageAsyncFooterCssFileName ?? "");
                if (match.Success)
                {
                    var templateIdsList = match.Groups[1].Value.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(Int32.Parse).ToList();
                    cssBuilder.AppendLine((await gclTemplatesService.GetCombinedTemplateValueAsync(templateIdsList, TemplateTypes.Css)).Content);
                }

                viewModel.Css.PageAsyncFooterCssFileName = null;
                
                match = regex.Match(viewModel.Css.PageSyncFooterCssFileName ?? "");
                if (match.Success)
                {
                    var templateIdsList = match.Groups[1].Value.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(Int32.Parse).ToList();
                    cssBuilder.AppendLine((await gclTemplatesService.GetCombinedTemplateValueAsync(templateIdsList, TemplateTypes.Css)).Content);
                }

                viewModel.Css.PageSyncFooterCssFileName = null;

                viewModel.Css.PageInlineHeadCss = cssBuilder.ToString();
            }

            if (viewModel.Javascript != null)
            {
                viewModel.Javascript.PageInlineHeadJavascript ??= new List<string>();
                viewModel.Javascript.PageInlineHeadJavascript.Insert(0, (await gclTemplatesService.GetGeneralTemplateValueAsync(TemplateTypes.Js)).Content);

                var regex = new Regex("/css/gcljs_(.*).css");
                var match = regex.Match(viewModel.Javascript.PageStandardJavascriptFileName ?? "");
                if (match.Success)
                {
                    var templateIdsList = match.Groups[1].Value.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(Int32.Parse).ToList();
                    viewModel.Javascript.PageInlineHeadJavascript.Add((await gclTemplatesService.GetCombinedTemplateValueAsync(templateIdsList, TemplateTypes.Css)).Content);
                }

                viewModel.Javascript.PageStandardJavascriptFileName = null;
                
                match = regex.Match(viewModel.Javascript.GeneralFooterJavascriptFileName ?? "");
                if (match.Success)
                {
                    var templateIdsList = match.Groups[1].Value.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(Int32.Parse).ToList();
                    viewModel.Javascript.PageInlineHeadJavascript.Add((await gclTemplatesService.GetCombinedTemplateValueAsync(templateIdsList, TemplateTypes.Css)).Content);
                }

                viewModel.Javascript.GeneralFooterJavascriptFileName = null;
                
                match = regex.Match(viewModel.Javascript.PageAsyncFooterJavascriptFileName ?? "");
                if (match.Success)
                {
                    var templateIdsList = match.Groups[1].Value.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(Int32.Parse).ToList();
                    viewModel.Javascript.PageInlineHeadJavascript.Add((await gclTemplatesService.GetCombinedTemplateValueAsync(templateIdsList, TemplateTypes.Css)).Content);
                }

                viewModel.Javascript.PageAsyncFooterJavascriptFileName = null;
                
                match = regex.Match(viewModel.Javascript.PageSyncFooterJavascriptFileName ?? "");
                if (match.Success)
                {
                    var templateIdsList = match.Groups[1].Value.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(Int32.Parse).ToList();
                    viewModel.Javascript.PageInlineHeadJavascript.Add((await gclTemplatesService.GetCombinedTemplateValueAsync(templateIdsList, TemplateTypes.Css)).Content);
                }

                viewModel.Javascript.PageSyncFooterJavascriptFileName = null;
            }

            // Generate HTML from view.
            await using var writer = new StringWriter();
            var executingAssemblyDirectoryAbsolutePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            var executingFilePath = executingAssemblyDirectoryAbsolutePath!.Replace('\\', '/');
            const string viewPath = "~/Modules/Templates/Views/Shared/Template.cshtml";
            var viewResult = razorViewEngine.GetView(executingFilePath, viewPath, true);

            var actionContext = new ActionContext(httpContextAccessor.HttpContext!, new RouteData(), new ActionDescriptor());

            if (viewResult.Success == false)
            {
                return new ServiceResult<string>($"A view with the name {viewResult.ViewName} could not be found");
            }

            var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = viewModel
            };

            var viewContext = new ViewContext(
                actionContext,
                viewResult.View,
                viewDictionary,
                new TempDataDictionary(actionContext.HttpContext, tempDataProvider),
                writer,
                new HtmlHelperOptions()
            );

            await viewResult.View.RenderAsync(viewContext);

            var finalResult = writer.GetStringBuilder().ToString();
            finalResult = finalResult.ReplaceCaseInsensitive("<head>", $"<head><base href='{AddMainDomainToUrl("/", mainDomain)}'>");
            return new ServiceResult<string>(finalResult);
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
                WHERE template.template_type = 1 AND template.removed = 0 AND template.`{fieldName}` = 1 AND template.template_id <> ?templateId {regexWherePart}
                GROUP BY template.template_id
                LIMIT 1";

            var result = await clientDatabaseConnection.GetAsync(query);
            return result.Rows.Count == 0
                ? new ServiceResult<string>(String.Empty)
                : new ServiceResult<string>(result.Rows[0].Field<string>("template_name"));
        }

        /// <summary>
        /// Converts Wiser 1 templates to the Wiser 3 format.
        /// </summary>
        /// <returns></returns>
        public async Task<ServiceResult<bool>> ConvertLegacyTemplatesToNewTemplates()
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
    template.cacheminutes AS cache_minutes,
    template.handlerequest AS handle_request,
    template.handlesession AS handle_session,
    template.handleobjects AS handle_objects,
    template.handlestandards AS handle_standards,
    template.handletranslations AS handle_translations,
    template.handledynamiccontent AS handle_dynamic_content,
    template.handlelogicblocks AS handle_logic_blocks,
    template.handlemutators AS handle_mutators,
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

            await using var reader = await clientDatabaseConnection.GetReaderAsync(query);
            while (await reader.ReadAsync())
            {
                // Get template type.
                var templateType = TemplateTypes.Directory;
                if (!reader.GetBoolean("is_directory"))
                {
                    var path = reader.GetStringHandleNull("path");

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
                var cdnDirectory = reader.GetStringHandleNull("type");
                if (String.Equals(cdnDirectory, "js"))
                {
                    cdnDirectory = "scripts";
                }

                var externalFiles = reader.GetStringHandleNull("external_files");
                var wiserCdnTemplates = reader.GetStringHandleNull("wiser_cdn_templates");
                var allExternalFiles = externalFiles.Split(';').ToList();
                allExternalFiles.AddRange(wiserCdnTemplates.Select(filename => $"https://app.wiser.nl/{cdnDirectory}/cdn/{filename}"));

                externalFiles = String.Join(";", allExternalFiles);

                var content = reader.GetStringHandleNull("template_data");
                var minifiedContent = reader.GetStringHandleNull("template_data_minified");

                // Convert dynamic components placeholders from Wiser 1 to Wiser 3 format.
                if (templateType == TemplateTypes.Html)
                {
                    content = ConvertDynamicComponentsFromLegacyToNewInHtml(content);
                    minifiedContent = ConvertDynamicComponentsFromLegacyToNewInHtml(minifiedContent);
                }

                var urlRegex = reader.GetStringHandleNull("url_regex");

                // Handle routine settings.
                var routineType = RoutineTypes.Unknown;
                string routineParameters = null;
                string routineReturnType = null;

                if (templateType == TemplateTypes.Routine)
                {
                    var legacyType = reader.GetStringHandleNull("type");
                    routineType = legacyType switch
                    {
                        "FUNCTION" => RoutineTypes.Function,
                        "PROCEDURE" => RoutineTypes.Procedure,
                        _ => routineType
                    };

                    routineParameters = reader.GetStringHandleNull("routine_parameters");
                    // Old templates module used the "urlregex" field to store the return type.
                    routineReturnType = urlRegex;

                    // Set url_regex to null in Wiser 3 template for routine templates.
                    urlRegex = null;
                }

                // TODO: Make method for converting legacy replacements (such as {title_htmlencode}) to GCL replacements (such as {title:HtmlEncode}).

                clientDatabaseConnection.ClearParameters();
                clientDatabaseConnection.AddParameter("parent_id", reader.GetValue("parent_id"));
                clientDatabaseConnection.AddParameter("template_name", reader.GetValue("template_name"));
                clientDatabaseConnection.AddParameter("template_data", content);
                clientDatabaseConnection.AddParameter("template_data_minified", minifiedContent);
                clientDatabaseConnection.AddParameter("template_type", templateType);
                clientDatabaseConnection.AddParameter("version", reader.GetValue("version"));
                clientDatabaseConnection.AddParameter("template_id", reader.GetValue("template_id"));
                clientDatabaseConnection.AddParameter("changed_on", reader.GetValue("changed_on"));
                clientDatabaseConnection.AddParameter("changed_by", reader.GetValue("changed_by"));
                clientDatabaseConnection.AddParameter("published_environment", reader.GetValue("published_environment"));
                clientDatabaseConnection.AddParameter("use_cache", reader.GetValue("use_cache"));
                clientDatabaseConnection.AddParameter("cache_minutes", reader.GetValue("cache_minutes"));
                clientDatabaseConnection.AddParameter("handle_request", reader.GetValue("handle_request"));
                clientDatabaseConnection.AddParameter("handle_session", reader.GetValue("handle_session"));
                clientDatabaseConnection.AddParameter("handle_objects", reader.GetValue("handle_objects"));
                clientDatabaseConnection.AddParameter("handle_standards", reader.GetValue("handle_standards"));
                clientDatabaseConnection.AddParameter("handle_translations", reader.GetValue("handle_translations"));
                clientDatabaseConnection.AddParameter("handle_dynamic_content", reader.GetValue("handle_dynamic_content"));
                clientDatabaseConnection.AddParameter("handle_logic_blocks", reader.GetValue("handle_logic_blocks"));
                clientDatabaseConnection.AddParameter("handle_mutators", reader.GetValue("handle_mutators"));
                clientDatabaseConnection.AddParameter("login_required", reader.GetValue("login_required"));
                clientDatabaseConnection.AddParameter("login_session_prefix", reader.GetValue("login_session_prefix"));
                clientDatabaseConnection.AddParameter("linked_templates", reader.GetValue("linked_templates"));
                clientDatabaseConnection.AddParameter("ordering", reader.GetValue("ordering"));
                clientDatabaseConnection.AddParameter("insert_mode", reader.GetValue("insert_mode"));
                clientDatabaseConnection.AddParameter("load_always", reader.GetValue("load_always"));
                clientDatabaseConnection.AddParameter("disable_minifier", reader.GetValue("disable_minifier"));
                clientDatabaseConnection.AddParameter("url_regex", urlRegex);
                clientDatabaseConnection.AddParameter("external_files", externalFiles);
                clientDatabaseConnection.AddParameter("grouping_create_object_instead_of_array", reader.GetValue("grouping_create_object_instead_of_array"));
                clientDatabaseConnection.AddParameter("grouping_prefix", reader.GetValue("grouping_prefix"));
                clientDatabaseConnection.AddParameter("grouping_key", reader.GetValue("grouping_key"));
                clientDatabaseConnection.AddParameter("grouping_key_column_name", reader.GetValue("grouping_key_column_name"));
                clientDatabaseConnection.AddParameter("grouping_value_column_name", reader.GetValue("grouping_value_column_name"));
                clientDatabaseConnection.AddParameter("is_scss_include_template", reader.GetValue("is_scss_include_template"));
                clientDatabaseConnection.AddParameter("use_in_wiser_html_editors", reader.GetValue("use_in_wiser_html_editors"));
                clientDatabaseConnection.AddParameter("routine_type", (int)routineType);
                clientDatabaseConnection.AddParameter("routine_parameters", routineParameters);
                clientDatabaseConnection.AddParameter("routine_return_type", routineReturnType);
                await clientDatabaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(WiserTableNames.WiserTemplate, 0);

                // Convert dynamic content.
                if (templateType == TemplateTypes.Html)
                {
                    query = @"SELECT *
                            FROM easy_dynamiccontent
                            WHERE itemid = ?template_id
                            AND version = ?version";
                    dataTable = await clientDatabaseConnection.GetAsync(query);
                    if (dataTable.Rows.Count == 0)
                    {
                        continue;
                    }

                    var legacyComponentName = dataTable.Rows[0].Field<string>("freefield1");
                    var legacySettingsJson = dataTable.Rows[0].Field<string>("filledvariables");
                    var newSettings = ConvertDynamicComponentSettingsFromLegacyToNew(legacyComponentName, legacySettingsJson);
                }
            }

            return new ServiceResult<bool>(true);
        }

        private static string ConvertDynamicComponentsFromLegacyToNewInHtml(string html)
        {
            var regex = new Regex(@"<img[^>]*?(?:data=['""](?<data>.*?)['""][^>]*?)?contentid=['""](?<contentid>\d+)['""][^>]*?\/?>");
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

                var newElement = $"<div class=\"dynamic-content\" {dataAttribute} component-id=\"{match.Groups["contentId"].Value}\">";
                html = html.Replace(match.Value, newElement);
            }

            return html;
        }

        private static string ConvertDynamicComponentSettingsFromLegacyToNew(string legacyComponentName, string legacySettingsJson)
        {
            string viewComponentName;
            switch (legacyComponentName)
            {
                case "JuiceControlLibrary.MLSimpleMenu":
                case "JuiceControlLibrary.SimpleMenu":
                case "JuiceControlLibrary.ProductModule":
                {
                    viewComponentName = "Repeater";
                    break;
                }
                case "JuiceControlLibrary.AccountWiser2":
                {
                    viewComponentName = "Account";
                    break;
                }
                case "JuiceControlLibrary.ShoppingBasket":
                {
                    viewComponentName = "ShoppingBasket";
                    break;
                }
                case "JuiceControlLibrary.WebPage":
                {
                    viewComponentName = "WebPage";
                    break;
                }
                case "JuiceControlLibrary.Pagination":
                {
                    viewComponentName = "Pagination";
                    break;
                }
                case "JuiceControlLibrary.DynamicFilter":
                {
                    viewComponentName = "Filter";
                    break;
                }
                case "JuiceControlLibrary.Sendform":
                {
                    viewComponentName = "WebForm";
                    break;
                }
                case "JuiceControlLibrary.Configurator":
                {
                    viewComponentName = "Configurator";
                    break;
                }
                case "JuiceControlLibrary.DataSelectorParser":
                {
                    viewComponentName = "DataSelectorParser";
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(legacyComponentName), legacyComponentName);
            }

            // TODO: The main CmsSettingsModel of every component should have a method "ToSettingsModel", which will convert the legacy settings to the new settings. Use that.

            return null;
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
        /// Sets URL and variables in http context, so the GCL can use them for replacements and settings and such while generating a preview for a template and/or dynamic content.
        /// </summary>
        private async Task SetupGclForPreviewAsync(ClaimsIdentity identity, GenerateTemplatePreviewRequestModel requestModel)
        {
            var customer = (await wiserCustomersService.GetSingleAsync(identity)).ModelObject;
            if (requestModel.PreviewVariables != null && httpContextAccessor.HttpContext != null)
            {
                foreach (var previewVariable in requestModel.PreviewVariables)
                {
                    switch (previewVariable.Type.ToUpperInvariant())
                    {
                        case "POST":
                            if (!requestModel.TemplateSettings.HandleRequests)
                            {
                                break;
                            }

                            if (previewVariable.Encrypt)
                            {
                                previewVariable.Value = previewVariable.Value.EncryptWithAesWithSalt(customer.EncryptionKey);
                            }

                            httpContextAccessor.HttpContext.Items.Add(previewVariable.Key, previewVariable.Value);
                            break;
                        case "SESSION":
                            if (!requestModel.TemplateSettings.HandleSession)
                            {
                                break;
                            }

                            if (previewVariable.Encrypt)
                            {
                                previewVariable.Value = previewVariable.Value.EncryptWithAesWithSalt(customer.EncryptionKey);
                            }

                            httpContextAccessor.HttpContext.Items.Add(previewVariable.Key, previewVariable.Value);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(previewVariable.Type), previewVariable.Type);
                    }
                }
            }

            // Let the GCL know that we want to use the given URL for everything, so the generated preview will be just like when a user opened the given URL on the actual website.
            if (httpContextAccessor.HttpContext != null && requestModel.Url != null && requestModel.Url.IsAbsoluteUri)
            {
                httpContextAccessor.HttpContext.Items.Add(Constants.WiserUriOverrideForReplacements, requestModel.Url);
            }

            // Force the GCL environment to development, so that it will always use the latest versions of templates and dynamic components.
            gclSettings.Environment = Environments.Development;
        }

        /// <summary>
        /// Will attempt to create a FUNCTION or PROCEDURE in the client's database.
        /// </summary>
        /// <param name="templateName">The name of the template, which will server as the name of the routine.</param>
        /// <param name="routineType">The type of the routine, which should be either <see cref="RoutineTypes.Function"/> or <see cref="RoutineTypes.Procedure"/>.</param>
        /// <param name="parameters">A string that represent the input parameters. For procedures, OUT and INOUT parameters can also be defined.</param>
        /// <param name="returnType">The data type that is expected. This is only if <paramref name="routineType"/> is set to <see cref="RoutineTypes.Function"/>.</param>
        /// <param name="routineDefinition">The body of the routine.</param>
        /// <returns><see langword="true"/> if the routine was successfully created; otherwise, <see langword="false"/>.</returns>
        private async Task<(bool Successful, string ErrorMessage)> CreateDatabaseRoutine(string templateName, RoutineTypes routineType, string parameters, string returnType, string routineDefinition)
        {
            if (routineType == RoutineTypes.Unknown)
            {
                return (false, "Routine type 'Unknown' is not a valid routine type.");
            }

            var routineName = $"WISER_{templateName}";

            // Check if routine exists.
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("routineName", routineName);
            var getRoutineData = await clientDatabaseConnection.GetAsync(@"
                SELECT COUNT(*) > 0 AS routine_exists
                FROM information_schema.ROUTINES
                WHERE ROUTINE_SCHEMA = DATABASE() AND ROUTINE_NAME = ?routineName");

            var routineExists = getRoutineData.Rows.Count > 0 && Convert.ToBoolean(getRoutineData.Rows[0]["routine_exists"]);

            var routineQueryBuilder = new StringBuilder();

            // Only FUNCTION routines directly return data.
            var returnsPart = routineType == RoutineTypes.Function ? $" RETURNS {returnType}" : String.Empty;

            if (!routineDefinition.Trim().EndsWith(";"))
            {
                routineDefinition = $"{routineDefinition.Trim()};";
            }

            routineQueryBuilder.AppendLine($"CREATE DEFINER=CURRENT_USER {routineType.ToString("G").ToUpper()} `{routineName}` ({parameters}){returnsPart}");
            routineQueryBuilder.AppendLine("    SQL SECURITY INVOKER");
            routineQueryBuilder.AppendLine("BEGIN");
            routineQueryBuilder.AppendLine("##############################################################################");
            routineQueryBuilder.AppendLine("# NOTE: This routine was created in Wiser! Do not edit directly in database! #");
            routineQueryBuilder.AppendLine("##############################################################################");
            routineQueryBuilder.AppendLine(routineDefinition);
            routineQueryBuilder.AppendLine("END");

            var routineQuery = routineQueryBuilder.ToString();

            try
            {
                // If the routine already exists, try to create a new one with a temporary name to check if creating the routine will not fail.
                // That way, the old routine will remain intact.
                if (routineExists)
                {
                    await clientDatabaseConnection.ExecuteAsync($"DROP FUNCTION IF EXISTS `{routineName}_temp`; DROP PROCEDURE IF EXISTS `{routineName}_temp`;");
                    await clientDatabaseConnection.ExecuteAsync(routineQuery.Replace($"`{routineName}`", $"`{routineName}_temp`"));
                }

                // Temp routine creation succeeded. Drop temp routine and current routine, and create the new routine.
                await clientDatabaseConnection.ExecuteAsync($"DROP FUNCTION IF EXISTS `{routineName}_temp`; DROP PROCEDURE IF EXISTS `{routineName}_temp`; DROP FUNCTION IF EXISTS `{routineName}`; DROP PROCEDURE IF EXISTS `{routineName}`;");
                await clientDatabaseConnection.ExecuteAsync(routineQuery);

                return (true, String.Empty);
            }
            catch (MySqlException mySqlException)
            {
                // Remove temporary routine if it was created (it has no use anymore).
                await clientDatabaseConnection.ExecuteAsync($"DROP FUNCTION IF EXISTS `{routineName}_temp`; DROP PROCEDURE IF EXISTS `{routineName}_temp`;");
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
        private async Task<(bool Successful, string MinifiedScript)> MinifyJavaScriptWithTerser(string script)
        {
            if (String.IsNullOrWhiteSpace(script))
            {
                return (false, null);
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
                    // eventArgs.Data holds the response from terser.
                    if (!String.IsNullOrWhiteSpace(eventArgs.Data))
                    {
                        output = eventArgs.Data;
                    }
                };

                process.Start();

                //process.EnableRaisingEvents = true;
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

            return (successful, output);
        }
    }
}