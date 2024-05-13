using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Api.Modules.Templates.Models.Template;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Models;
using Newtonsoft.Json;

namespace Api.Modules.Templates.Helpers;

/// <summary>
/// Helpers for templates.
/// </summary>
public class TemplateHelpers
{
    /// <summary>
    /// Create a new <see cref="TemplateSettingsModel"/> from a <see cref="DataRow" />.
    /// This expects the <see cref="DataRow" /> to contain specific columns, check the source code for more information.
    /// </summary>
    /// <param name="dataRow">The <see cref="DataRow" /> that contains the data for the <see cref="TemplateSettingsModel"/>.</param>
    /// <returns>The <see cref="TemplateSettingsModel"/>.</returns>
    public static TemplateSettingsModel DataRowToTemplateSettingsModel(DataRow dataRow)
    {
        var dataTable = dataRow.Table;

        var templateData = new TemplateSettingsModel
        {
            TemplateId = dataRow.Field<int>("template_id"),
            ParentId = dataRow.Field<int?>("parent_id"),
            Type = dataRow.Field<TemplateTypes>("template_type"),
            Name = dataRow.Field<string>("template_name"),
            EditorValue = dataRow.Field<string>("template_data"),
            Version = dataRow.Field<int>("version"),
            AddedOn = dataRow.Field<DateTime>("added_on"),
            AddedBy = dataRow.Field<string>("added_by"),
            ChangedOn = dataRow.Field<DateTime>("changed_on"),
            ChangedBy = dataRow.Field<string>("changed_by"),
            CachePerUrl = dataRow.Field<bool>("cache_per_url"),
            CachePerQueryString = dataRow.Field<bool>("cache_per_querystring"),
            CacheUsingRegex = dataRow.Field<bool>("cache_using_regex"),
            CachePerHostName = dataRow.Field<bool>("cache_per_hostname"),
            CacheMinutes = dataRow.Field<int>("cache_minutes"),
            CachePerUser = dataRow.Field<bool>("cache_per_user"),
            CacheLocation = (TemplateCachingLocations) dataRow.Field<int>("cache_location"),
            CacheRegex = dataRow.Field<string>("cache_regex"),
            IsDefaultFooter = dataRow.Field<bool>("is_default_footer"),
            IsDefaultHeader = dataRow.Field<bool>("is_default_header"),
            DefaultHeaderFooterRegex = dataRow.Field<string>("default_header_footer_regex"),
            IsPartial = dataRow.Field<bool>("is_partial"),
            LoginRequired = Convert.ToBoolean(dataRow["login_required"]),
            LoginRedirectUrl = dataRow.Field<string>("login_redirect_url"),
            Ordering = dataRow.Field<int>("ordering"),
            InsertMode = dataRow.Field<ResourceInsertModes>("insert_mode"),
            LoadAlways = Convert.ToBoolean(dataRow["load_always"]),
            DisableMinifier = Convert.ToBoolean(dataRow["disable_minifier"]),
            UrlRegex = dataRow.Field<string>("url_regex"),
            GroupingCreateObjectInsteadOfArray = Convert.ToBoolean(dataRow["grouping_create_object_instead_of_array"]),
            GroupingPrefix = dataRow.Field<string>("grouping_prefix"),
            GroupingKey = dataRow.Field<string>("grouping_key"),
            GroupingKeyColumnName = dataRow.Field<string>("grouping_key_column_name"),
            GroupingValueColumnName = dataRow.Field<string>("grouping_value_column_name"),
            IsScssIncludeTemplate = Convert.ToBoolean(dataRow["is_scss_include_template"]),
            UseInWiserHtmlEditors = Convert.ToBoolean(dataRow["use_in_wiser_html_editors"]),
            LinkedTemplates = new LinkedTemplatesModel
            {
                RawLinkList = dataRow.Field<string>("linked_templates") ?? ""
            },
            PreLoadQuery = dataRow.Field<string>("pre_load_query"),
            ReturnNotFoundWhenPreLoadQueryHasNoData = Convert.ToBoolean(dataRow["return_not_found_when_pre_load_query_has_no_data"]),
            WidgetContent = dataRow.Field<string>("widget_content") ?? "",
            WidgetLocation = (PageWidgetLocations) Convert.ToInt32(dataRow["widget_location"])
        };

        // Set routine properties.
        if (dataTable.Columns.Contains("routine_type"))
        {
            templateData.RoutineType = dataRow.Field<RoutineTypes>("routine_type");
        }
        if (dataTable.Columns.Contains("routine_parameters"))
        {
            templateData.RoutineParameters = dataRow.Field<string>("routine_parameters");
        }
        if (dataTable.Columns.Contains("routine_return_type"))
        {
            templateData.RoutineReturnType = dataRow.Field<string>("routine_return_type");
        }

        if (dataTable.Columns.Contains("external_files_json"))
        {
            var jsonString = dataRow.Field<string>("external_files_json");
            if (!String.IsNullOrEmpty(jsonString))
            {
                templateData.ExternalFiles = JsonConvert.DeserializeObject<List<PageResourceModel>>(jsonString);
            }
        }

        var externalFiles = dataRow.Field<string>("external_files")?.Split(new[] {';', ','}, StringSplitOptions.RemoveEmptyEntries);
        if (externalFiles != null && externalFiles.Any())
        {
            foreach (var file in externalFiles)
            {
                if (!Uri.TryCreate(file, UriKind.RelativeOrAbsolute, out var uri))
                {
                    continue;
                }

                templateData.ExternalFiles.Add(new PageResourceModel
                {
                    Uri = uri,
                    Ordering = templateData.ExternalFiles.Count + 1
                });
            }
        }

        var loginRolesString = dataRow.Field<string>("login_role");
        if (!String.IsNullOrWhiteSpace(loginRolesString))
        {
            templateData.LoginRoles = loginRolesString.Split(",").Select(Int32.Parse).ToList();
        }

        return templateData;
    }
}