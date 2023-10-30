using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Services;
using Api.Modules.Tenants.Interfaces;
using Api.Modules.Queries.Interfaces;
using Api.Modules.Queries.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Queries.Services
{
    //TODO Verify comments
    /// <summary>
    /// Service for getting queries for the Wiser modules.
    /// </summary>
    public class QueriesService : IQueriesService, IScopedService
    {
        private readonly IWiserTenantsService wiserTenantsService;
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IWiserItemsService wiserItemsService;

        /// <summary>
        /// Creates a new instance of <see cref="QueriesService"/>.
        /// </summary>
        public QueriesService(IWiserTenantsService wiserTenantsService, IDatabaseConnection clientDatabaseConnection, IWiserItemsService wiserItemsService)
        {
            this.wiserTenantsService = wiserTenantsService;
            this.clientDatabaseConnection = clientDatabaseConnection;
            this.wiserItemsService = wiserItemsService;
        }


        /// <inheritdoc />
        public async Task<ServiceResult<List<QueryModel>>> GetForExportModuleAsync(ClaimsIdentity identity)
        {
            var results = await GetQueriesForModuleAsync(identity, "show_in_export_module").ToListAsync();

            return new ServiceResult<List<QueryModel>>(results);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<QueryModel>>> GetForCommunicationModuleAsync(ClaimsIdentity identity)
        {
            var results = await GetQueriesForModuleAsync(identity, "show_in_communication_module").ToListAsync();

            return new ServiceResult<List<QueryModel>>(results);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<QueryModel>>> GetAsync(ClaimsIdentity identity)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT
    query.id,
    query.description,
    query.query,
    query.show_in_export_module,
    query.show_in_communication_module,
    IFNULL(GROUP_CONCAT(permission.role_id), '') AS roles_with_permissions
FROM {WiserTableNames.WiserQuery} AS query
LEFT JOIN {WiserTableNames.WiserPermission} AS permission ON permission.query_id = query.id
GROUP BY query.id");

            var results = new List<QueryModel>();
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<List<QueryModel>>(results);
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                results.Add(new QueryModel
                {
                    Id = dataRow.Field<int>("id"),
                    EncryptedId = await wiserTenantsService.EncryptValue(dataRow.Field<int>("id").ToString(), identity),
                    Description = dataRow.Field<string>("description"),
                    Query = dataRow.Field<string>("query"),
                    ShowInExportModule = Convert.ToBoolean(dataRow["show_in_export_module"]),
                    ShowInCommunicationModule = Convert.ToBoolean(dataRow["show_in_communication_module"]),
                    RolesWithPermissions = dataRow.Field<string>("roles_with_permissions")
                });
            }

            return new ServiceResult<List<QueryModel>>(results);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<QueryModel>> GetAsync(ClaimsIdentity identity, int id)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", id);
            var query = $@"SELECT
    query.id,
    query.description,
    query.query,
    query.show_in_export_module,
    query.show_in_communication_module,
    IFNULL(GROUP_CONCAT(permission.role_id), '') AS roles_with_permissions
FROM {WiserTableNames.WiserQuery} AS query
LEFT JOIN {WiserTableNames.WiserPermission} AS permission ON permission.query_id = query.id
WHERE query.id = ?id";
            var dataTable = await clientDatabaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<QueryModel>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = $"Wiser query with ID '{id}' does not exist."
                };
            }
                
            var dataRow = dataTable.Rows[0];
            var result = new QueryModel()
            {
                Id = dataRow.Field<int>("id"),
                EncryptedId = await wiserTenantsService.EncryptValue(dataRow.Field<int>("id").ToString(), identity),
                Description = dataRow.Field<string>("description"),
                Query = dataRow.Field<string>("query"),
                ShowInExportModule = Convert.ToBoolean(dataRow["show_in_export_module"]),
                ShowInCommunicationModule = Convert.ToBoolean(dataRow["show_in_communication_module"]),
                RolesWithPermissions = dataRow.Field<string>("roles_with_permissions")
            };

            return new ServiceResult<QueryModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<QueryModel>> CreateAsync(ClaimsIdentity identity, string description)
        {
            if (String.IsNullOrEmpty(description))
            {
                return new ServiceResult<QueryModel>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "'Description' must contain a value."
                };
            }

            var queryModel = new QueryModel
            {
                Description = description,
                Query = "",
                ShowInExportModule = false,
                ShowInCommunicationModule = false,
                RolesWithPermissions = ""
            };

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("description", queryModel.Description);
            clientDatabaseConnection.AddParameter("query", queryModel.Query);
            clientDatabaseConnection.AddParameter("show_in_export_module", queryModel.ShowInExportModule);
            clientDatabaseConnection.AddParameter("show_in_communication_module", queryModel.ShowInCommunicationModule);
            
            var query = $@"INSERT INTO {WiserTableNames.WiserQuery}
(
    description,
    query,
    show_in_export_module,
    show_in_communication_module
)
VALUES
(
    ?description,
    ?query,
    ?show_in_export_module,
    ?show_in_communication_module
);
SELECT LAST_INSERT_ID();";

            try
            {
                var dataTable = await clientDatabaseConnection.GetAsync(query);
                queryModel.Id = Convert.ToInt32(dataTable.Rows[0][0]);
            }
            catch (MySqlException mySqlException)
            {
                if (mySqlException.Number == (int)MySqlErrorCode.DuplicateKeyEntry)
                {
                    return new ServiceResult<QueryModel>
                    {
                        StatusCode = HttpStatusCode.Conflict,
                        ErrorMessage = $"An entry already exists with {nameof(queryModel.Description)} = '{queryModel.Description}'"
                    };
                }

                throw;
            }

            return new ServiceResult<QueryModel>(queryModel);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> UpdateAsync(ClaimsIdentity identity, QueryModel queryModel)
        {
            if (queryModel?.Description == null)
            {
                return new ServiceResult<bool>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "Either 'Query' or 'Description' must contain a value."
                };
            }
            
            // Check if query exists.
            var queryResult = await GetAsync(identity, queryModel.Id);
            if (queryResult.StatusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<bool>
                {
                    ErrorMessage = queryResult.ErrorMessage,
                    StatusCode = queryResult.StatusCode
                };
            }

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", queryModel.Id);
            clientDatabaseConnection.AddParameter("description", queryModel.Description);
            clientDatabaseConnection.AddParameter("query", queryModel.Query);
            clientDatabaseConnection.AddParameter("show_in_export_module", queryModel.ShowInExportModule);
            clientDatabaseConnection.AddParameter("show_in_communication_module", queryModel.ShowInCommunicationModule);

            var query = $@"UPDATE {WiserTableNames.WiserQuery}
SET description = ?description,
    query = ?query,
    show_in_export_module = ?show_in_export_module,
    show_in_communication_module = ?show_in_communication_module
WHERE id = ?id";

            await clientDatabaseConnection.ExecuteAsync(query);

            // Add the permissions for the roles that have been marked. Will only add new ones to preserve limited permissions.
            query = $@"INSERT IGNORE INTO {WiserTableNames.WiserPermission} (role_id, query_id, permissions)
VALUES(?roleId, ?id, 15)";

            foreach (var role in queryModel.RolesWithPermissions.Split(","))
            {
                clientDatabaseConnection.AddParameter("roleId", role);
                await clientDatabaseConnection.ExecuteAsync(query);
            }
            
            // Delete permissions for the roles that are missing in the allowed roles.
            clientDatabaseConnection.AddParameter("roles_with_permissions", queryModel.RolesWithPermissions);
            query = $"DELETE FROM {WiserTableNames.WiserPermission} WHERE query_id = ?id AND query_id != 0 AND NOT FIND_IN_SET(role_id, ?roles_with_permissions)";
            await clientDatabaseConnection.ExecuteAsync(query);
            
            return new ServiceResult<bool>
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> DeleteAsync(ClaimsIdentity identity, int id)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            
            // Check if query exists.
            var queryResult = await GetAsync(identity, id);
            if (queryResult.StatusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<bool>
                {
                    ErrorMessage = queryResult.ErrorMessage,
                    StatusCode = queryResult.StatusCode
                };
            }

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", id);

            var query = $@"DELETE FROM {WiserTableNames.WiserQuery} WHERE id = ?id;
DELETE FROM {WiserTableNames.WiserPermission} WHERE query_id = ?id AND query_id != 0";
            await clientDatabaseConnection.ExecuteAsync(query);

            return new ServiceResult<bool>
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }

        /// <inheritdoc />
        public async Task<ServiceResult<JToken>> GetQueryResultAsJsonAsync(ClaimsIdentity identity, int id, bool asKeyValuePair, List<KeyValuePair<string, object>> parameters)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", id);
            var query = $"SELECT query FROM {WiserTableNames.WiserQuery} WHERE id = ?id";
            var dataTable = await clientDatabaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<JToken>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = $"Wiser query with ID '{id}' does not exist."
                };
            }

            if ((await wiserItemsService.GetUserQueryPermissionsAsync(id, IdentityHelpers.GetWiserUserId(identity)) & AccessRights.Read) == AccessRights.Nothing)
            {
                return new ServiceResult<JToken>
                {
                    StatusCode = HttpStatusCode.Unauthorized,
                    ErrorMessage = $"Wiser user '{IdentityHelpers.GetUserName(identity)}' has no permission to execute this query."
                };
            }

            query = dataTable.Rows[0].Field<string>("query");

            clientDatabaseConnection.ClearParameters();
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    clientDatabaseConnection.AddParameter(DatabaseHelpers.CreateValidParameterName(parameter.Key), parameter.Value);
                }
            }

            dataTable = await clientDatabaseConnection.GetAsync(query);
            var result = dataTable.Rows.Count == 0 ? new JArray() : dataTable.ToJsonArray(skipNullValues: true);

            if (!asKeyValuePair)
            {
                return new ServiceResult<JToken>(result);
            }

            var combinedResult = new JObject();

            foreach (var item in result)
            {
                combinedResult.Add(item["key"].ToString(), item["value"]);
            }
            
            return new ServiceResult<JToken>(combinedResult);
        }

        /// <summary>
        /// Get queries for a specific module.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="showInModuleColumnName">The column name to check whether a query should be shown in a module. Example: "show_in_export_module".</param>
        /// <returns>The queries for the requested module.</returns>
        private async IAsyncEnumerable<QueryModel> GetQueriesForModuleAsync(ClaimsIdentity identity, string showInModuleColumnName)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT
    id,
    description
FROM {WiserTableNames.WiserQuery}
WHERE `{showInModuleColumnName}` = 1
ORDER BY description ASC");

            if (dataTable.Rows.Count == 0)
            {
                yield break;
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                yield return new QueryModel
                {
                    Id = dataRow.Field<int>("id"),
                    EncryptedId = await wiserTenantsService.EncryptValue(dataRow.Field<int>("id").ToString(), identity),
                    Description = dataRow.Field<string>("description")
                };
            }
        }
    }
}
