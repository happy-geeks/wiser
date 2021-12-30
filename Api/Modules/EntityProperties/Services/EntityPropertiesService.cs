using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.EntityProperties.Enums;
using Api.Modules.EntityProperties.Interfaces;
using Api.Modules.EntityProperties.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using MySql.Data.MySqlClient;

namespace Api.Modules.EntityProperties.Services
{
    /// <summary>
    /// Service for all CRUD operations for entity properties (from the table wiser_entityproperty).
    /// </summary>
    public class EntityPropertiesService : IEntityPropertiesService, IScopedService
    {
        private readonly IDatabaseConnection clientDatabaseConnection;

        /// <summary>
        /// Create a new instance of <see cref="EntityPropertiesService"/>
        /// </summary>
        /// <param name="clientDatabaseConnection"></param>
        public EntityPropertiesService(IDatabaseConnection clientDatabaseConnection)
        {
            this.clientDatabaseConnection = clientDatabaseConnection;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<EntityPropertyModel>>> GetAsync(ClaimsIdentity identity)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            var query = $"SELECT * FROM {WiserTableNames.WiserEntityProperty} ORDER BY entity_name ASC, link_type ASC, ordering ASC";
            var dataTable = await clientDatabaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<List<EntityPropertyModel>>(new List<EntityPropertyModel>());
            }

            var results = dataTable.Rows.Cast<DataRow>().Select(FromDataRow).ToList();

            return new ServiceResult<List<EntityPropertyModel>>(results);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<EntityPropertyModel>> GetAsync(ClaimsIdentity identity, int id)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", id);
            var query = $"SELECT * FROM {WiserTableNames.WiserEntityProperty} WHERE id = ?id";
            var dataTable = await clientDatabaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<EntityPropertyModel>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = $"Entity property with ID '{id}' does not exist.",
                    ReasonPhrase = $"Entity property with ID '{id}' does not exist."
                };
            }

            var result = FromDataRow(dataTable.Rows[0]);

            return new ServiceResult<EntityPropertyModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<EntityPropertyModel>>> GetPropertiesOfEntityAsync(ClaimsIdentity identity, string entityName, bool onlyEntityTypesWithDisplayName = true, bool onlyEntityTypesWithPropertyName = true, bool addIdProperty = false)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("entityName", entityName);
            var query = $@"SELECT *
                            FROM {WiserTableNames.WiserEntityProperty}
                            WHERE entity_name = ?entityName
                            {(onlyEntityTypesWithDisplayName ? "AND display_name IS NOT NULL AND display_name <> ''" : "")}
                            {(onlyEntityTypesWithPropertyName ? "AND property_name IS NOT NULL AND property_name <> ''" : "")}
                            ORDER BY display_name ASC";
            var dataTable = await clientDatabaseConnection.GetAsync(query);

            var results = new List<EntityPropertyModel>();

            if (addIdProperty)
            {
                results.Add(new EntityPropertyModel()
                {
                    PropertyName = "id",
                    DisplayName = "Id"
                });
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                results.Add(FromDataRow(dataRow));
            }

            return new ServiceResult<List<EntityPropertyModel>>(results);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<EntityPropertyModel>> CreateAsync(ClaimsIdentity identity, EntityPropertyModel entityProperty)
        {
            if (entityProperty == null || (String.IsNullOrWhiteSpace(entityProperty.EntityType) && entityProperty.LinkType <= 0))
            {
                return new ServiceResult<EntityPropertyModel>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "Either 'EntityType' or 'LinkType' must contain a value.",
                    ReasonPhrase = "Either 'EntityType' or 'LinkType' must contain a value."
                };
            }

            if (String.IsNullOrWhiteSpace(entityProperty.PropertyName))
            {
                return new ServiceResult<EntityPropertyModel>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "PropertyName is required.",
                    ReasonPhrase = "PropertyName is required."
                };
            }

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("module_id", entityProperty.ModuleId);
            clientDatabaseConnection.AddParameter("entity_name", entityProperty.EntityType ?? "");
            clientDatabaseConnection.AddParameter("visible_in_overview", entityProperty.Overview?.Visible ?? false);
            clientDatabaseConnection.AddParameter("overview_fieldtype", entityProperty.Overview?.FieldType ?? "");
            clientDatabaseConnection.AddParameter("overview_width", entityProperty.Overview?.Width ?? 100);
            clientDatabaseConnection.AddParameter("tab_name", entityProperty.TabName ?? "");
            clientDatabaseConnection.AddParameter("group_name", entityProperty.GroupName ?? "");
            clientDatabaseConnection.AddParameter("inputtype", ToDatabaseValue(entityProperty.InputType));
            clientDatabaseConnection.AddParameter("display_name", entityProperty.DisplayName ?? "");
            clientDatabaseConnection.AddParameter("property_name", entityProperty.PropertyName ?? "");
            clientDatabaseConnection.AddParameter("explanation", entityProperty.Explanation ?? "");
            clientDatabaseConnection.AddParameter("ordering", entityProperty.Ordering);
            clientDatabaseConnection.AddParameter("regex_validation", entityProperty.RegexValidation ?? "");
            clientDatabaseConnection.AddParameter("mandatory", entityProperty.Mandatory);
            clientDatabaseConnection.AddParameter("readonly", entityProperty.ReadOnly);
            clientDatabaseConnection.AddParameter("default_value", entityProperty.DefaultValue ?? "");
            clientDatabaseConnection.AddParameter("width", entityProperty.Width);
            clientDatabaseConnection.AddParameter("height", entityProperty.Height);
            clientDatabaseConnection.AddParameter("options", entityProperty.Options ?? "");
            clientDatabaseConnection.AddParameter("data_query", entityProperty.DataQuery ?? "");
            clientDatabaseConnection.AddParameter("action_query", entityProperty.ActionQuery ?? "");
            clientDatabaseConnection.AddParameter("search_query", entityProperty.SearchQuery ?? "");
            clientDatabaseConnection.AddParameter("search_count_query", entityProperty.SearchCountQuery ?? "");
            clientDatabaseConnection.AddParameter("grid_delete_query", entityProperty.GridDeleteQuery ?? "");
            clientDatabaseConnection.AddParameter("grid_insert_query", entityProperty.GridInsertQuery ?? "");
            clientDatabaseConnection.AddParameter("grid_update_query", entityProperty.GridUpdateQuery ?? "");
            clientDatabaseConnection.AddParameter("depends_on_field", entityProperty.DependsOn?.Field ?? "");
            clientDatabaseConnection.AddParameter("depends_on_operator", ToDatabaseValue(entityProperty.DependsOn?.Operator));
            clientDatabaseConnection.AddParameter("depends_on_value", entityProperty.DependsOn?.Value ?? "");
            clientDatabaseConnection.AddParameter("depends_on_action", entityProperty.DependsOn?.Action);
            clientDatabaseConnection.AddParameter("language_code", entityProperty.LanguageCode ?? "");
            clientDatabaseConnection.AddParameter("custom_script", entityProperty.CustomScript ?? "");
            clientDatabaseConnection.AddParameter("also_save_seo_value", entityProperty.AlsoSaveSeoValue);
            clientDatabaseConnection.AddParameter("save_on_change", entityProperty.SaveOnChange);
            clientDatabaseConnection.AddParameter("link_type", entityProperty.LinkType);
            clientDatabaseConnection.AddParameter("extended_explanation", entityProperty.ExtendedExplanation);
            clientDatabaseConnection.AddParameter("label_style", ToDatabaseValue(entityProperty.LabelStyle));
            clientDatabaseConnection.AddParameter("label_width", entityProperty.LabelWidth);

            var query = $@"INSERT INTO {WiserTableNames.WiserEntityProperty}
                        (
                            module_id,
                            entity_name,
                            visible_in_overview,
                            overview_fieldtype,
                            overview_width,
                            tab_name,
                            group_name,
                            inputtype,
                            display_name,
                            property_name,
                            explanation,
                            ordering,
                            regex_validation,
                            mandatory,
                            readonly,
                            default_value,
                            width,
                            height,
                            options,
                            data_query,
                            action_query,
                            search_query,
                            search_count_query,
                            grid_delete_query,
                            grid_insert_query,
                            grid_update_query,
                            depends_on_field,
                            depends_on_operator,
                            depends_on_value,
                            language_code,
                            custom_script,
                            also_save_seo_value,
                            depends_on_action,
                            save_on_change,
                            link_type,
                            extended_explanation,
                            label_style,
                            label_width
                        )
                        VALUES
                        (
                            ?module_id,
                            ?entity_name,
                            ?visible_in_overview,
                            ?overview_fieldtype,
                            ?overview_width,
                            ?tab_name,
                            ?group_name,
                            ?inputtype,
                            ?display_name,
                            ?property_name,
                            ?explanation,
                            ?ordering,
                            ?regex_validation,
                            ?mandatory,
                            ?readonly,
                            ?default_value,
                            ?width,
                            ?height,
                            ?options,
                            ?data_query,
                            ?action_query,
                            ?search_query,
                            ?search_count_query,
                            ?grid_delete_query,
                            ?grid_insert_query,
                            ?grid_update_query,
                            ?depends_on_field,
                            ?depends_on_operator,
                            ?depends_on_value,
                            ?language_code,
                            ?custom_script,
                            ?also_save_seo_value,
                            ?depends_on_action,
                            ?save_on_change,
                            ?link_type,
                            ?extended_explanation,
                            ?label_style,
                            ?label_width
                        ); SELECT LAST_INSERT_ID();";

            try
            {
                var dataTable = await clientDatabaseConnection.GetAsync(query);
                entityProperty.Id = Convert.ToInt32(dataTable.Rows[0][0]);
            }
            catch (MySqlException mySqlException)
            {
                if (mySqlException.Number == (int)MySqlErrorCode.DuplicateKeyEntry)
                {
                    return new ServiceResult<EntityPropertyModel>
                    {
                        StatusCode = HttpStatusCode.Conflict,
                        ErrorMessage = $"An entry already exists with {nameof(entityProperty.EntityType)} = '{entityProperty.EntityType}', {nameof(entityProperty.LinkType)} = '{entityProperty.LinkType}', {nameof(entityProperty.DisplayName)} = '{entityProperty.DisplayName}', {nameof(entityProperty.PropertyName)} = '{entityProperty.PropertyName}' and {nameof(entityProperty.LanguageCode)} = '{entityProperty.LanguageCode}'",
                        ReasonPhrase = "And entry already exists with this data."
                    };
                }

                throw;
            }

            return new ServiceResult<EntityPropertyModel>(entityProperty);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> UpdateAsync(ClaimsIdentity identity, int id, EntityPropertyModel entityProperty)
        {
            if (entityProperty == null || (String.IsNullOrWhiteSpace(entityProperty.EntityType) && entityProperty.LinkType <= 0))
            {
                return new ServiceResult<bool>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "Either 'EntityType' or 'LinkType' must contain a value.",
                    ReasonPhrase = "Either 'EntityType' or 'LinkType' must contain a value."
                };
            }

            if (String.IsNullOrWhiteSpace(entityProperty.PropertyName))
            {
                return new ServiceResult<bool>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "PropertyName is required.",
                    ReasonPhrase = "PropertyName is required."
                };
            }

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", id);
            clientDatabaseConnection.AddParameter("module_id", entityProperty.ModuleId);
            clientDatabaseConnection.AddParameter("entity_name", entityProperty.EntityType ?? "");
            clientDatabaseConnection.AddParameter("visible_in_overview", entityProperty.Overview?.Visible ?? false);
            clientDatabaseConnection.AddParameter("overview_fieldtype", entityProperty.Overview?.FieldType ?? "");
            clientDatabaseConnection.AddParameter("overview_width", entityProperty.Overview?.Width ?? 100);
            clientDatabaseConnection.AddParameter("tab_name", entityProperty.TabName ?? "");
            clientDatabaseConnection.AddParameter("group_name", entityProperty.GroupName ?? "");
            clientDatabaseConnection.AddParameter("inputtype", ToDatabaseValue(entityProperty.InputType));
            clientDatabaseConnection.AddParameter("display_name", entityProperty.DisplayName ?? "");
            clientDatabaseConnection.AddParameter("property_name", entityProperty.PropertyName ?? "");
            clientDatabaseConnection.AddParameter("explanation", entityProperty.Explanation ?? "");
            clientDatabaseConnection.AddParameter("ordering", entityProperty.Ordering);
            clientDatabaseConnection.AddParameter("regex_validation", entityProperty.RegexValidation ?? "");
            clientDatabaseConnection.AddParameter("mandatory", entityProperty.Mandatory);
            clientDatabaseConnection.AddParameter("readonly", entityProperty.ReadOnly);
            clientDatabaseConnection.AddParameter("default_value", entityProperty.DefaultValue ?? "");
            clientDatabaseConnection.AddParameter("width", entityProperty.Width);
            clientDatabaseConnection.AddParameter("height", entityProperty.Height);
            clientDatabaseConnection.AddParameter("options", entityProperty.Options ?? "");
            clientDatabaseConnection.AddParameter("data_query", entityProperty.DataQuery ?? "");
            clientDatabaseConnection.AddParameter("action_query", entityProperty.ActionQuery ?? "");
            clientDatabaseConnection.AddParameter("search_query", entityProperty.SearchQuery ?? "");
            clientDatabaseConnection.AddParameter("search_count_query", entityProperty.SearchCountQuery ?? "");
            clientDatabaseConnection.AddParameter("grid_delete_query", entityProperty.GridDeleteQuery ?? "");
            clientDatabaseConnection.AddParameter("grid_insert_query", entityProperty.GridInsertQuery ?? "");
            clientDatabaseConnection.AddParameter("grid_update_query", entityProperty.GridUpdateQuery ?? "");
            clientDatabaseConnection.AddParameter("depends_on_field", entityProperty.DependsOn?.Field ?? "");
            clientDatabaseConnection.AddParameter("depends_on_operator", ToDatabaseValue(entityProperty.DependsOn?.Operator));
            clientDatabaseConnection.AddParameter("depends_on_value", entityProperty.DependsOn?.Value ?? "");
            clientDatabaseConnection.AddParameter("depends_on_action", entityProperty.DependsOn?.Action);
            clientDatabaseConnection.AddParameter("language_code", entityProperty.LanguageCode ?? "");
            clientDatabaseConnection.AddParameter("custom_script", entityProperty.CustomScript ?? "");
            clientDatabaseConnection.AddParameter("also_save_seo_value", entityProperty.AlsoSaveSeoValue);
            clientDatabaseConnection.AddParameter("save_on_change", entityProperty.SaveOnChange);
            clientDatabaseConnection.AddParameter("link_type", entityProperty.LinkType);
            clientDatabaseConnection.AddParameter("extended_explanation", entityProperty.ExtendedExplanation);
            clientDatabaseConnection.AddParameter("label_style", ToDatabaseValue(entityProperty.LabelStyle));
            clientDatabaseConnection.AddParameter("label_width", entityProperty.LabelWidth);

            var query = $@"UPDATE {WiserTableNames.WiserEntityProperty}
                        SET module_id = ?module_id,
                            entity_name = ?entity_name,
                            visible_in_overview = ?visible_in_overview,
                            overview_fieldtype = ?overview_fieldtype,
                            overview_width = ?overview_width,
                            tab_name = ?tab_name,
                            group_name = ?group_name,
                            inputtype = ?inputtype,
                            display_name = ?display_name,
                            property_name = ?property_name,
                            explanation = ?explanation,
                            ordering = ?ordering,
                            regex_validation = ?regex_validation,
                            mandatory = ?mandatory,
                            readonly = ?readonly,
                            default_value = ?default_value,
                            width = ?width,
                            height = ?height,
                            options = ?options,
                            data_query = ?data_query,
                            action_query = ?action_query,
                            search_query = ?search_query,
                            search_count_query = ?search_count_query,
                            grid_delete_query = ?grid_delete_query,
                            grid_insert_query = ?grid_insert_query,
                            grid_update_query = ?grid_update_query,
                            depends_on_field = ?depends_on_field,
                            depends_on_operator = ?depends_on_operator,
                            depends_on_value = ?depends_on_value,
                            language_code = ?language_code,
                            custom_script = ?custom_script,
                            also_save_seo_value = ?also_save_seo_value,
                            depends_on_action = ?depends_on_action,
                            save_on_change = ?save_on_change,
                            link_type = ?link_type,
                            extended_explanation = ?extended_explanation,
                            label_style = ?label_style,
                            label_width = ?label_width
                        WHERE id = ?id";

            try
            {
                await clientDatabaseConnection.ExecuteAsync(query);
            }
            catch (MySqlException mySqlException)
            {
                if (mySqlException.Number == (int)MySqlErrorCode.DuplicateKeyEntry)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = HttpStatusCode.Conflict,
                        ErrorMessage = $"An entry already exists with {nameof(entityProperty.EntityType)} = '{entityProperty.EntityType}', {nameof(entityProperty.LinkType)} = '{entityProperty.LinkType}', {nameof(entityProperty.DisplayName)} = '{entityProperty.DisplayName}', {nameof(entityProperty.PropertyName)} = '{entityProperty.PropertyName}' and {nameof(entityProperty.LanguageCode)} = '{entityProperty.LanguageCode}'",
                        ReasonPhrase = "And entry already exists with this data."
                    };
                }

                throw;
            }

            return new ServiceResult<bool>(true)
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> DeleteAsync(ClaimsIdentity identity, int id)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", id);

            var query = $"DELETE FROM {WiserTableNames.WiserEntityProperty} WHERE id = ?id";
            await clientDatabaseConnection.ExecuteAsync(query);
            return new ServiceResult<bool>(true)
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }

        private static FilterOperators? ToFilterOperator(string value)
        {
            switch (value)
            {
                case null:
                    return null;
                case "eq":
                    return FilterOperators.Equals;
                case "neq":
                    return FilterOperators.NotEquals;
                case "contains":
                    return FilterOperators.Contains;
                case "doesnotcontain":
                    return FilterOperators.DoesNotContain;
                case "startswith":
                    return FilterOperators.StartsWith;
                case "doesnotstartwith":
                    return FilterOperators.DoesNotStartWith;
                case "endswith":
                    return FilterOperators.EndsWith;
                case "doesnotendwith":
                    return FilterOperators.DoesNotEndWith;
                case "isempty":
                    return FilterOperators.IsEmpty;
                case "isnotempty":
                    return FilterOperators.IsNotEmpty;
                case "gte":
                    return FilterOperators.GreaterThanOrEqualTo;
                case "gt":
                    return FilterOperators.GreaterThan;
                case "lte":
                    return FilterOperators.LessThanOrEqualTo;
                case "lt":
                    return FilterOperators.LessThan;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        private static string ToDatabaseValue(FilterOperators? value)
        {
            switch (value)
            {
                case null:
                    return null;
                case FilterOperators.Equals:
                    return "eq";
                case FilterOperators.NotEquals:
                    return "neq";
                case FilterOperators.Contains:
                    return "contains";
                case FilterOperators.DoesNotContain:
                    return "doesnotcontain";
                case FilterOperators.StartsWith:
                    return "startswith";
                case FilterOperators.DoesNotStartWith:
                    return "doesnotstartwith";
                case FilterOperators.EndsWith:
                    return "endswith";
                case FilterOperators.DoesNotEndWith:
                    return "doesnotendwith";
                case FilterOperators.IsEmpty:
                    return "isempty";
                case FilterOperators.IsNotEmpty:
                    return "isnotempty";
                case FilterOperators.GreaterThanOrEqualTo:
                    return "gte";
                case FilterOperators.GreaterThan:
                    return "gt";
                case FilterOperators.LessThanOrEqualTo:
                    return "lte";
                case FilterOperators.LessThan:
                    return "lt";
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        private static EntityPropertyLabelStyles? ToLabelStyle(string value)
        {
            switch (value?.ToLowerInvariant())
            {
                case null:
                    return null;
                case "normal":
                    return EntityPropertyLabelStyles.Normal;
                case "inline":
                    return EntityPropertyLabelStyles.Inline;
                case "float":
                    return EntityPropertyLabelStyles.Float;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        private static string ToDatabaseValue(EntityPropertyLabelStyles? value)
        {
            switch (value)
            {
                case null:
                    return "normal";
                case EntityPropertyLabelStyles.Normal:
                    return "normal";
                case EntityPropertyLabelStyles.Inline:
                    return "inline";
                case EntityPropertyLabelStyles.Float:
                    return "float";
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        private static DependencyActions? ToDependencyAction(string value)
        {
            switch (value?.ToLowerInvariant())
            {
                case null:
                case "":
                    return null;
                case "toggle-visibility":
                    return DependencyActions.ToggleVisibility;
                case "refresh":
                    return DependencyActions.Refresh;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        private static string ToDatabaseValue(DependencyActions? value)
        {
            switch (value)
            {
                case null:
                    return null;
                case DependencyActions.ToggleVisibility:
                    return "toggle-visibility";
                case DependencyActions.Refresh:
                    return "refresh";
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        private static EntityPropertyInputTypes ToInputType(string value)
        {
            switch (value.ToLowerInvariant())
            {
                case "input":
                    return EntityPropertyInputTypes.Input;
                case "secure-input":
                    return EntityPropertyInputTypes.SecureInput;
                case "textbox":
                    return EntityPropertyInputTypes.TextBox;
                case "radiobutton":
                    return EntityPropertyInputTypes.RadioButton;
                case "checkbox":
                    return EntityPropertyInputTypes.CheckBox;
                case "combobox":
                    return EntityPropertyInputTypes.ComboBox;
                case "multiselect":
                    return EntityPropertyInputTypes.MultiSelect;
                case "numeric-input":
                    return EntityPropertyInputTypes.NumericInput;
                case "file-upload":
                    return EntityPropertyInputTypes.FileUpload;
                case "htmleditor":
                    return EntityPropertyInputTypes.HtmlEditor;
                case "querybuilder":
                    return EntityPropertyInputTypes.QueryBuilder;
                case "date-time picker":
                    return EntityPropertyInputTypes.DateTimePicker;
                case "grid":
                    return EntityPropertyInputTypes.Grid;
                case "imagecoords":
                    return EntityPropertyInputTypes.ImageCoordinates;
                case "image-upload":
                    return EntityPropertyInputTypes.ImageUpload;
                case "gpslocation":
                    return EntityPropertyInputTypes.GpsLocation;
                case "daterange":
                    return EntityPropertyInputTypes.DateRange;
                case "sub-entities-grid":
                    return EntityPropertyInputTypes.SubEntitiesGrid;
                case "item-linker":
                    return EntityPropertyInputTypes.ItemLinker;
                case "color-picker":
                    return EntityPropertyInputTypes.ColorPicker;
                case "auto-increment":
                    return EntityPropertyInputTypes.AutoIncrement;
                case "linked-item":
                    return EntityPropertyInputTypes.LinkedItem;
                case "action-button":
                    return EntityPropertyInputTypes.ActionButton;
                case "data-selector":
                    return EntityPropertyInputTypes.DataSelector;
                case "chart":
                    return EntityPropertyInputTypes.Chart;
                case "scheduler":
                    return EntityPropertyInputTypes.Scheduler;
                case "timeline":
                    return EntityPropertyInputTypes.TimeLine;
                case "empty":
                    return EntityPropertyInputTypes.Empty;
                case "qr":
                    return EntityPropertyInputTypes.Qr;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        private static string ToDatabaseValue(EntityPropertyInputTypes value)
        {
            switch (value)
            {
                case EntityPropertyInputTypes.Input:
                    return "input";
                case EntityPropertyInputTypes.SecureInput:
                    return "secure-input";
                case EntityPropertyInputTypes.TextBox:
                    return "textbox";
                case EntityPropertyInputTypes.RadioButton:
                    return "radiobutton";
                case EntityPropertyInputTypes.CheckBox:
                    return "checkbox";
                case EntityPropertyInputTypes.ComboBox:
                    return "combobox";
                case EntityPropertyInputTypes.MultiSelect:
                    return "multiselect";
                case EntityPropertyInputTypes.NumericInput:
                    return "numeric-input";
                case EntityPropertyInputTypes.FileUpload:
                    return "file-upload";
                case EntityPropertyInputTypes.HtmlEditor:
                    return "HTMLeditor";
                case EntityPropertyInputTypes.QueryBuilder:
                    return "querybuilder";
                case EntityPropertyInputTypes.DateTimePicker:
                    return "date-time picker";
                case EntityPropertyInputTypes.Grid:
                    return "grid";
                case EntityPropertyInputTypes.ImageCoordinates:
                    return "imagecoords";
                case EntityPropertyInputTypes.ImageUpload:
                    return "image-upload";
                case EntityPropertyInputTypes.GpsLocation:
                    return "gpslocation";
                case EntityPropertyInputTypes.DateRange:
                    return "daterange";
                case EntityPropertyInputTypes.SubEntitiesGrid:
                    return "sub-entities-grid";
                case EntityPropertyInputTypes.ItemLinker:
                    return "item-linker";
                case EntityPropertyInputTypes.ColorPicker:
                    return "color-picker";
                case EntityPropertyInputTypes.AutoIncrement:
                    return "auto-increment";
                case EntityPropertyInputTypes.LinkedItem:
                    return "linked-item";
                case EntityPropertyInputTypes.ActionButton:
                    return "action-button";
                case EntityPropertyInputTypes.DataSelector:
                    return "data-selector";
                case EntityPropertyInputTypes.Chart:
                    return "chart";
                case EntityPropertyInputTypes.Scheduler:
                    return "scheduler";
                case EntityPropertyInputTypes.TimeLine:
                    return "timeline";
                case EntityPropertyInputTypes.Empty:
                    return "empty";
                case EntityPropertyInputTypes.Qr:
                    return "qr";
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        private static EntityPropertyModel FromDataRow(DataRow dataRow)
        {
            var row = new EntityPropertyModel();
            row.Id = dataRow.Field<int>("id");
            row.ModuleId = Convert.ToInt32(dataRow["module_id"]);
            row.EntityType = dataRow.Field<string>("entity_name");
            row.LinkType = dataRow.Field<int>("link_type");
            row.PropertyName = dataRow.Field<string>("property_name");
            row.LanguageCode = dataRow.Field<string>("language_code");
            row.TabName = dataRow.Field<string>("tab_name");
            row.GroupName = dataRow.Field<string>("group_name");
            row.InputType = ToInputType(dataRow.Field<string>("inputtype"));
            row.DisplayName = dataRow.Field<string>("display_name");
            row.Ordering = Convert.ToInt32(dataRow["ordering"]);
            row.Explanation = dataRow.Field<string>("explanation");
            row.ExtendedExplanation = Convert.ToBoolean(dataRow["extended_explanation"]);
            row.RegexValidation = dataRow.Field<string>("regex_validation");
            row.Mandatory = Convert.ToBoolean(dataRow["mandatory"]);
            row.ReadOnly = Convert.ToBoolean(dataRow["readonly"]);
            row.DefaultValue = dataRow.Field<string>("default_value");
            row.Width = Convert.ToInt32(dataRow["width"]);
            row.Height = Convert.ToInt32(dataRow["height"]);
            row.Options = dataRow.Field<string>("options");
            row.DataQuery = dataRow.Field<string>("data_query");
            row.ActionQuery = dataRow.Field<string>("action_query");
            row.SearchQuery = dataRow.Field<string>("search_query");
            row.SearchCountQuery = dataRow.Field<string>("search_count_query");
            row.GridInsertQuery = dataRow.Field<string>("grid_insert_query");
            row.GridUpdateQuery = dataRow.Field<string>("grid_update_query");
            row.GridDeleteQuery = dataRow.Field<string>("grid_delete_query");
            row.CustomScript = dataRow.Field<string>("custom_script");
            row.AlsoSaveSeoValue = Convert.ToBoolean(dataRow["also_save_seo_value"]);
            row.SaveOnChange = Convert.ToBoolean(dataRow["save_on_change"]);
            row.LabelStyle = ToLabelStyle(dataRow.Field<string>("label_style"));
            row.LabelWidth = Convert.ToInt32(dataRow.Field<object>("label_width"));
            row.Overview = new EntityPropertyOverviewModel();
            row.Overview.Visible = Convert.ToBoolean(dataRow["visible_in_overview"]);
            row.Overview.FieldType = dataRow.Field<string>("overview_fieldtype");
            row.Overview.Width = Convert.ToInt32(dataRow["overview_width"]);
            row.DependsOn = new EntityPropertyDependencyModel();
            row.DependsOn.Action = ToDependencyAction(dataRow.Field<string>("depends_on_action"));
            row.DependsOn.Field = dataRow.Field<string>("depends_on_field");
            row.DependsOn.Operator = ToFilterOperator(dataRow.Field<string>("depends_on_operator"));
            row.DependsOn.Value = dataRow.Field<string>("depends_on_value");
            return row;
        }
    }
}
