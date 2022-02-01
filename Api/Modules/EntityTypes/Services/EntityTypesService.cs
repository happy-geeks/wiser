using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Customers.Interfaces;
using Api.Modules.EntityTypes.Interfaces;
using Api.Modules.EntityTypes.Models;
using Api.Modules.Queries.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace Api.Modules.EntityTypes.Services
{
    public class EntityTypesService : IEntityTypesService, IScopedService
    {
        private readonly IWiserCustomersService wiserCustomersService;
        private readonly IDatabaseConnection clientDatabaseConnection;

        /// <summary>
        /// Creates a new instance of <see cref="EntityTypesService"/>.
        /// </summary>
        public EntityTypesService(IWiserCustomersService wiserCustomersService, IDatabaseConnection clientDatabaseConnection)
        {
            this.wiserCustomersService = wiserCustomersService;
            this.clientDatabaseConnection = clientDatabaseConnection;
        }
        
        /// <inheritdoc />
        public async Task<ServiceResult<List<EntityTypeModel>>> GetAsync(ClaimsIdentity identity, bool onlyEntityTypesWithDisplayName = true)
        {
            var result = new List<EntityTypeModel>();
            var query = $@"SELECT 
	                        entity.name, 
	                        CONCAT(IF(entity.friendly_name IS NULL OR entity.friendly_name = '', entity.name, entity.friendly_name), IF(module.`name` IS NULL, '', CONCAT(' (', module.`name`, ')'))) AS displayName,
	                        entity.module_id,
	                        entity.name,
	                        entity.accepted_childtypes,
	                        entity.icon,
	                        entity.icon_add,
	                        entity.icon_expanded,
	                        entity.show_in_tree_view,
	                        entity.query_after_insert,
	                        entity.query_after_update,
	                        entity.query_before_update,
	                        entity.query_before_delete,
	                        entity.color,
	                        entity.show_in_search,
	                        entity.show_overview_tab,
	                        entity.save_title_as_seo,
	                        api_after_insert,
	                        api_after_update,
	                        api_before_update
	                        api_before_delete,
	                        entity.show_title_field,
	                        entity.friendly_name,
	                        entity.save_history,
	                        entity.default_ordering
                        FROM {WiserTableNames.WiserEntity} AS entity
                        LEFT JOIN {WiserTableNames.WiserModule} AS module ON module.id = entity.module_id
                        WHERE IFNULL(entity.`name`, '') <> ''
                        {(onlyEntityTypesWithDisplayName ? "AND entity.friendly_name IS NOT NULL AND entity.friendly_name <> ''" : "")}
                        GROUP BY entity.`name`
                        ORDER BY CONCAT(IF(entity.friendly_name IS NULL OR entity.friendly_name = '', entity.name, entity.friendly_name), IF(module.`name` IS NULL, '', CONCAT(' (', module.`name`, ')'))) ASC";

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            var dataTable = await clientDatabaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<List<EntityTypeModel>>(result);
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                result.Add(new EntityTypeModel
                {
                    DisplayName = dataRow.Field<string>("displayName"),
                    Id = dataRow.Field<string>("name"),
                    ModuleId = dataRow.Field<int>("module_id"),
                    AcceptedChildTypes = dataRow.Field<string>("accepted_childtypes")?.Split(",").ToList(),
                    Icon = dataRow.Field<string>("icon"),
                    IconAdd = dataRow.Field<string>("icon_add"),
                    IconExpanded = dataRow.Field<string>("icon_expanded"),
                    QueryAfterInsert = dataRow.Field<string>("query_after_insert"),
                    QueryAfterUpdate = dataRow.Field<string>("query_after_update"),
                    QueryBeforeDelete = dataRow.Field<string>("query_before_update"),
                    QueryBeforeUpdate = dataRow.Field<string>("query_before_delete"),
                    FriendlyName = dataRow.Field<string>("friendly_name"),
                    Color = dataRow.Field<string>("color"),
                    ShowInTreeView = dataRow.Field<bool>("show_in_tree_view"),
                    ShowInSearch = dataRow.Field<bool>("show_in_search"),
                    ShowOverviewTab = dataRow.Field<bool>("show_overview_tab"),
                    SaveTitleAsSeo = dataRow.Field<bool>("save_title_as_seo"),
                    ShowTitleField = dataRow.Field<bool>("show_title_field"),
                    SaveHistory = dataRow.Field<bool>("save_history"),
                    DefaultOrdering = dataRow.Field<string>("default_ordering")
                });
            }

            return new ServiceResult<List<EntityTypeModel>>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<EntityTypeModel>> GetAsync(ClaimsIdentity identity, string id, int moduleId)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", id);
            clientDatabaseConnection.AddParameter("module_id", moduleId);

            var query = $@"SELECT 
	                        entity.name, 
	                        CONCAT(IF(entity.friendly_name IS NULL OR entity.friendly_name = '', entity.name, entity.friendly_name), IF(module.`name` IS NULL, '', CONCAT(' (', module.`name`, ')'))) AS displayName,
	                        entity.module_id,
	                        entity.name,
	                        entity.accepted_childtypes,
	                        entity.icon,
	                        entity.icon_add,
	                        entity.icon_expanded,
	                        entity.show_in_tree_view,
	                        entity.query_after_insert,
	                        entity.query_after_update,
	                        entity.query_before_update,
	                        entity.query_before_delete,
	                        entity.color,
	                        entity.show_in_search,
	                        entity.show_overview_tab,
	                        entity.save_title_as_seo,
	                        api_after_insert,
	                        api_after_update,
	                        api_before_update
	                        api_before_delete,
	                        entity.show_title_field,
	                        entity.friendly_name,
	                        entity.save_history,
	                        entity.default_ordering
                        FROM {WiserTableNames.WiserEntity} AS entity
                        LEFT JOIN {WiserTableNames.WiserModule} AS module ON module.id = entity.module_id
                        WHERE entity.`name` = ?id
                            AND entity.module_id = ?module_id;";

            var dataTable = await clientDatabaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<EntityTypeModel>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = $"Entity type with ID '{id}' and module id '{moduleId}' does not exist.",
                    ReasonPhrase = $"Entity type with ID '{id}' and module id '{moduleId}' does not exist."
                };
            }

            var dataRow = dataTable.Rows[0];
            var result = new EntityTypeModel()
            {
                DisplayName = dataRow.Field<string>("displayName"),
                Id = dataRow.Field<string>("name"),
                ModuleId = dataRow.Field<int>("module_id"),
                AcceptedChildTypes = dataRow.Field<string>("accepted_childtypes")?.Split(",").ToList(),
                Icon = dataRow.Field<string>("icon"),
                IconAdd = dataRow.Field<string>("icon_add"),
                IconExpanded = dataRow.Field<string>("icon_expanded"),
                QueryAfterInsert = dataRow.Field<string>("query_after_insert"),
                QueryAfterUpdate = dataRow.Field<string>("query_after_update"),
                QueryBeforeDelete = dataRow.Field<string>("query_before_delete"),
                QueryBeforeUpdate = dataRow.Field<string>("query_before_update"),
                FriendlyName = dataRow.Field<string>("friendly_name"),
                Color = dataRow.Field<string>("color"),
                ShowInTreeView = dataRow.Field<bool>("show_in_tree_view"),
                ShowInSearch = dataRow.Field<bool>("show_in_search"),
                ShowOverviewTab = dataRow.Field<bool>("show_overview_tab"),
                SaveTitleAsSeo = dataRow.Field<bool>("save_title_as_seo"),
                ShowTitleField = dataRow.Field<bool>("show_title_field"),
                SaveHistory = dataRow.Field<bool>("save_history"),
                DefaultOrdering = dataRow.Field<string>("default_ordering")
            };

            return new ServiceResult<EntityTypeModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> UpdateAsync(ClaimsIdentity identity, string id, EntityTypeModel entityTypeModel)
        {
            if (entityTypeModel == null || entityTypeModel.Id == null)
            {
                return new ServiceResult<bool>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "Either 'entity type' or 'Id' must contain a value.",
                    ReasonPhrase = "Either 'entity type' or 'Id' must contain a value."
                };
            }

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("name", id);
            clientDatabaseConnection.AddParameter("module_id", entityTypeModel.ModuleId);
            clientDatabaseConnection.AddParameter("accepted_childtypes", string.Join(",",entityTypeModel.AcceptedChildTypes));
            clientDatabaseConnection.AddParameter("icon", entityTypeModel.Icon);
            clientDatabaseConnection.AddParameter("icon_add", entityTypeModel.IconAdd);
            clientDatabaseConnection.AddParameter("icon_expanded", entityTypeModel.IconExpanded);
            clientDatabaseConnection.AddParameter("show_in_tree_view", entityTypeModel.ShowInTreeView ? 1 : 0);
            clientDatabaseConnection.AddParameter("query_after_insert", entityTypeModel.QueryAfterInsert);
            clientDatabaseConnection.AddParameter("query_after_update", entityTypeModel.QueryAfterUpdate);
            clientDatabaseConnection.AddParameter("query_before_update", entityTypeModel.QueryBeforeUpdate);
            clientDatabaseConnection.AddParameter("query_before_delete", entityTypeModel.QueryBeforeDelete);
            clientDatabaseConnection.AddParameter("color", entityTypeModel.Color);
            clientDatabaseConnection.AddParameter("show_in_search", entityTypeModel.ShowInSearch ? 1 : 0);
            clientDatabaseConnection.AddParameter("show_overview_tab", entityTypeModel.ShowOverviewTab ? 1 : 0);
            clientDatabaseConnection.AddParameter("save_title_as_seo", entityTypeModel.SaveTitleAsSeo ? 1 : 0);
            clientDatabaseConnection.AddParameter("show_title_field", entityTypeModel.ShowTitleField ? 1 : 0);
            clientDatabaseConnection.AddParameter("save_history", entityTypeModel.SaveHistory ? 1 : 0);
            clientDatabaseConnection.AddParameter("friendly_name", entityTypeModel.FriendlyName);
            clientDatabaseConnection.AddParameter("default_ordering", entityTypeModel.DefaultOrdering.ToString());

            var query  = $@"UPDATE {WiserTableNames.WiserEntity} SET
                module_id = ?module_id,
                name = ?name,
                accepted_childtypes = ?accepted_childtypes,
                icon = ?icon,
                icon_add = ?icon_add,
                icon_expanded = ?icon_expanded,
                show_in_tree_view = ?show_in_tree_view,
                query_after_insert = ?query_after_insert,
                query_after_update = ?query_after_update,
                query_before_update = ?query_before_update,
                query_before_delete = ?query_before_delete,
                color = ?color,
                show_in_search = ?show_in_search,
                show_overview_tab = ?show_overview_tab,
                save_title_as_seo = ?save_title_as_seo,
                show_title_field = ?show_title_field,
                save_history = ?save_history,
                friendly_name = ?friendly_name,
                default_ordering = ?default_ordering
                WHERE 
                    name = ?name AND  
                    module_id = ?module_id;";
            
            await clientDatabaseConnection.ExecuteAsync(query);

            return new ServiceResult<bool>(true)
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<string>>> GetAvailableEntityTypesAsync(ClaimsIdentity identity, int moduleId, string parentId = null)
        {
            ulong actualParentId;
            if (String.IsNullOrWhiteSpace(parentId))
            {
                actualParentId = 0;
            }
            else if (!UInt64.TryParse(parentId, out actualParentId))
            {
                actualParentId = await wiserCustomersService.DecryptValue<ulong>(parentId, identity);
            }
            
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("moduleId", moduleId);
            clientDatabaseConnection.AddParameter("parentId", actualParentId);

            var result = new List<string>();
            var query = $@"SELECT DISTINCT(e2.name) AS name
                            FROM {WiserTableNames.WiserEntity} e
                            LEFT JOIN {WiserTableNames.WiserItem} i ON i.entity_type = e.name AND i.moduleid = e.module_id
                            JOIN {WiserTableNames.WiserEntity} e2 ON e2.module_id = ?moduleId AND e2.name <> '' AND FIND_IN_SET(e2.name, e.accepted_childtypes)
                            WHERE e.module_id = ?moduleId
                            AND ((?parentId = 0 AND e.name = '') OR (?parentId > 0 AND i.id = ?parentId))
                            ORDER BY e2.name";

            var dataTable = await clientDatabaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<List<string>>(result);
            }

            result.AddRange(dataTable.Rows.Cast<DataRow>().Select(dataRow => dataRow.Field<string>("name")));

            return new ServiceResult<List<string>>(result);
        }
    }
}
