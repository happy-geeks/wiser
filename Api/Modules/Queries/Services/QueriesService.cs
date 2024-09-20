using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Services;
using Api.Modules.Branches.Interfaces;
using Api.Modules.Items.Models;
using Api.Modules.Queries.Interfaces;
using Api.Modules.Queries.Models;
using Api.Modules.Tenants.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
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
        private readonly IServiceProvider serviceProvider;
        private readonly IBranchesService branchesService;
        private readonly IDatabaseHelpersService databaseHelpersService;

        /// <summary>
        /// Creates a new instance of <see cref="QueriesService"/>.
        /// </summary>
        public QueriesService(IWiserTenantsService wiserTenantsService, IDatabaseConnection clientDatabaseConnection, IWiserItemsService wiserItemsService, IServiceProvider serviceProvider, IBranchesService branchesService, IDatabaseHelpersService databaseHelpersService)
        {
            this.wiserTenantsService = wiserTenantsService;
            this.clientDatabaseConnection = clientDatabaseConnection;
            this.wiserItemsService = wiserItemsService;
            this.serviceProvider = serviceProvider;
            this.branchesService = branchesService;
            this.databaseHelpersService = databaseHelpersService;
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
            
            await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> {WiserTableNames.WiserIdMappings});
            
            var tenant = await wiserTenantsService.GetSingleAsync(identity);
            var entityTypeSettings = await wiserItemsService.GetEntityTypeSettingsAsync("Query");
            var tablePrefix = wiserItemsService.GetTablePrefixForEntity(entityTypeSettings);
            
            var isOnBranch = !branchesService.IsMainBranch(tenant.ModelObject).ModelObject; 
            
            var mainTenant = await wiserTenantsService.GetSingleAsync(tenant.ModelObject.TenantId, true);
            
            var id = -1;

            if (isOnBranch)
            {
                // When we are on a branch always create the id on both branch and main database.
                using var scope = serviceProvider.CreateScope();
                var mainDatabaseConnection = scope.ServiceProvider.GetRequiredService<IDatabaseConnection>();
                var mainBranchConnectionString = wiserTenantsService.GenerateConnectionStringFromTenant(mainTenant.ModelObject);
                await mainDatabaseConnection.ChangeConnectionStringsAsync(mainBranchConnectionString);
                
                queryModel.Id = (int)await branchesService.GenerateNewIdAsync($"{tablePrefix}{WiserTableNames.WiserQuery}",mainDatabaseConnection,clientDatabaseConnection);
                
                try
                {
                    await LockTables(mainDatabaseConnection);
                    await LockTables(clientDatabaseConnection);
                    await CreateAsyncOnDataBase(mainDatabaseConnection, queryModel, identity, description);
                    await CreateAsyncOnDataBase(clientDatabaseConnection, queryModel, identity, description);
                }
                finally
                {
                    await UnlockTables(mainDatabaseConnection);
                    await UnlockTables(clientDatabaseConnection);
                }
            }
            else
            {
                queryModel.Id = (int)await branchesService.GenerateNewIdAsync($"{tablePrefix}{WiserTableNames.WiserQuery}",clientDatabaseConnection);
                await CreateAsyncOnDataBase(clientDatabaseConnection, queryModel, identity, description);
            }
            
            return new ServiceResult<QueryModel>(queryModel);
        }

        private async Task<bool> LockTables(IDatabaseConnection targetDatabase)
        {
            try
            {
                var lockQuery = $"""
                                 LOCK TABLES
                                 {WiserTableNames.WiserQuery} WRITE,
                                 {WiserTableNames.WiserQuery} item READ,
                                 {WiserTableNames.WiserUserRoles} user_role READ,
                                 {WiserTableNames.WiserPermission} permission READ,
                                 {WiserTableNames.WiserIdMappings} WRITE
                                 """;

                await targetDatabase.ExecuteAsync(lockQuery);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private async Task<bool> UnlockTables(IDatabaseConnection targetDatabase)
        {
            try
            {
                var unlockQuery = "UNLOCK TABLES";
                await targetDatabase.ExecuteAsync(unlockQuery);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private async Task<ServiceResult<QueryModel>> CreateAsyncOnDataBase(IDatabaseConnection targetDatabase, QueryModel queryModel, ClaimsIdentity identity, string description)
        {
            var tenant = await wiserTenantsService.GetSingleAsync(identity);
           
            using var scope = serviceProvider.CreateScope();
            
            try
            {
                var lockQuery = $"""
                                 LOCK TABLES
                                 {WiserTableNames.WiserQuery} WRITE,
                                 {WiserTableNames.WiserQuery} item READ,
                                 {WiserTableNames.WiserUserRoles} user_role READ,
                                 {WiserTableNames.WiserPermission} permission READ,
                                 {WiserTableNames.WiserIdMappings} WRITE
                                 """;

                await targetDatabase.ExecuteAsync(lockQuery);
                
                var itemInsertQuery = $@"INSERT INTO {WiserTableNames.WiserQuery}
                (
                    id,
                    description,
                    query,
                    show_in_export_module,
                    show_in_communication_module
                )
                VALUES
                (
                    ?id,
                    ?description,
                    ?query,
                    ?show_in_export_module,
                    ?show_in_communication_module
                );";
                
                await targetDatabase.EnsureOpenConnectionForReadingAsync();
                targetDatabase.ClearParameters();
                targetDatabase.AddParameter("id", queryModel.Id);
                targetDatabase.AddParameter("description", queryModel.Description);
                targetDatabase.AddParameter("query", queryModel.Query);
                targetDatabase.AddParameter("show_in_export_module", queryModel.ShowInExportModule);
                targetDatabase.AddParameter("show_in_communication_module", queryModel.ShowInCommunicationModule);
                await targetDatabase.ExecuteAsync(itemInsertQuery);

                targetDatabase.AddParameter("tableName", $"{WiserTableNames.WiserQuery}");
                targetDatabase.AddParameter("id", queryModel.Id);
           
                var insertQuery =
                    $"INSERT INTO {WiserTableNames.WiserIdMappings} (table_name, our_id, production_id) VALUES (?tableName, ?id, ?id)";
                await targetDatabase.ExecuteAsync(insertQuery);
            }
            finally
            {
                var unlockQuery = "UNLOCK TABLES";
                await targetDatabase.ExecuteAsync(unlockQuery);
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
            
            if (!dataTable.Columns.Contains("key") || !dataTable.Columns.Contains("value"))
            {
                return new ServiceResult<JToken>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "The query result does not contain the expected columns 'key' and 'value'."
                };
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