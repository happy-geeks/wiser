using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Services;
using Api.Modules.Tenants.Interfaces;
using Api.Modules.TaskAlerts.Interfaces;
using Api.Modules.TaskAlerts.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace Api.Modules.TaskAlerts.Services
{
    //TODO Verify comments
    /// <summary>
    /// Service for getting task alerts.
    /// </summary>
    public class TaskAlertsService : ITaskAlertsService, IScopedService
    {
        private readonly IWiserTenantsService wiserTenantsService;
        private readonly IDatabaseConnection clientDatabaseConnection;

        /// <summary>
        /// Creates a new instance of <see cref="TaskAlertsService"/>.
        /// </summary>
        /// <param name="wiserTenantsService"></param>
        /// <param name="clientDatabaseConnection"></param>
        public TaskAlertsService(IWiserTenantsService wiserTenantsService, IDatabaseConnection clientDatabaseConnection)
        {
            this.wiserTenantsService = wiserTenantsService;
            this.clientDatabaseConnection = clientDatabaseConnection;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<TaskAlertModel>>> GetAsync(ClaimsIdentity identity, bool getAllUsers = false, string branchDatabaseName = null)
        {
            var tenant = (await wiserTenantsService.GetSingleAsync(identity)).ModelObject;
            var userId = IdentityHelpers.GetWiserUserId(identity);

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("userId", userId);
            clientDatabaseConnection.AddParameter("now", DateTime.Now);

            // The database portion which will be placed in front of the table names of the FROM and JOIN statements.
            var queryDatabasePart = !String.IsNullOrWhiteSpace(branchDatabaseName) ? $"`{branchDatabaseName}`." : String.Empty;

            var userJoinPart = getAllUsers ? "" : "AND userId.`value` = ?userId";
            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT
    taskAlert.id,
    taskAlert.moduleid,
    DATE(checkedOn.value) AS checkedOn,
    content.value AS content,
    DATE(createdOn.value) AS createdOn,
    userId.value AS userId,
    `user`.title AS userName,
    status.value AS status,
    linkedItemId.value AS linkedItemId,
    linkedItemModuleId.value AS linkedItemModuleId,
    linkedItemEntityType.value AS linkedItemEntityType,
    placedBy.value AS placedBy,
    placedById.value AS placedById
FROM {queryDatabasePart}{WiserTableNames.WiserItem} AS taskAlert
JOIN {queryDatabasePart}{WiserTableNames.WiserItemDetail} AS userId ON userId.item_id = taskAlert.id AND userId.`key` = 'userid'{userJoinPart}
JOIN {queryDatabasePart}{WiserTableNames.WiserItem} AS `user` ON `user`.id = userId.`value` AND `user`.entity_type = 'wiseruser'
JOIN {queryDatabasePart}{WiserTableNames.WiserItemDetail} AS createdOn ON createdOn.item_id = taskAlert.id AND createdOn.`key` = 'agendering_date' AND createdOn.value <= ?now
LEFT JOIN {queryDatabasePart}{WiserTableNames.WiserItemDetail} AS checkedOn ON checkedOn.item_id = taskAlert.id AND checkedOn.`key` = 'checkedon'
LEFT JOIN {queryDatabasePart}{WiserTableNames.WiserItemDetail} AS content ON content.item_id = taskAlert.id AND content.`key` = 'content'
LEFT JOIN {queryDatabasePart}{WiserTableNames.WiserItemDetail} AS status ON status.item_id = taskAlert.id AND status.`key` = 'status'
LEFT JOIN {queryDatabasePart}{WiserTableNames.WiserItemDetail} AS linkedItemId ON linkedItemId.item_id = taskAlert.id AND linkedItemId.`key` = 'linked_item_id'
LEFT JOIN {queryDatabasePart}{WiserTableNames.WiserItemDetail} AS linkedItemModuleId ON linkedItemModuleId.item_id = taskAlert.id AND linkedItemModuleId.`key` = 'linked_item_module_id'
LEFT JOIN {queryDatabasePart}{WiserTableNames.WiserItemDetail} AS linkedItemEntityType ON linkedItemEntityType.item_id = taskAlert.id AND linkedItemEntityType.`key` = 'linked_item_entity_type'
LEFT JOIN {queryDatabasePart}{WiserTableNames.WiserItemDetail} AS placedBy ON placedBy.item_id = taskAlert.id AND placedBy.`key` = 'placed_by'
LEFT JOIN {queryDatabasePart}{WiserTableNames.WiserItemDetail} AS placedById ON placedById.item_id = taskAlert.id AND placedById.`key` = 'placed_by_id'
WHERE taskAlert.entity_type = 'agendering'
AND taskAlert.published_environment > 0
AND (checkedOn.value IS NULL OR checkedOn.value = '')");

            var results = new List<TaskAlertModel>();
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<List<TaskAlertModel>>(results);
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                if (dataRow.IsNull("createdOn"))
                {
                    continue;
                }

                var id = dataRow.Field<ulong>("id");
                var linkedItemId = dataRow.Field<string>("linkedItemId");
                var linkedItemModuleId = dataRow.Field<string>("linkedItemModuleId");
                var placedById = dataRow.Field<string>("placedById");

                results.Add(new TaskAlertModel
                {
                    CheckedOn = dataRow.Field<DateTime?>("checkedOn"),
                    Content = dataRow.Field<string>("content") ?? "",
                    CreatedOn = dataRow.Field<DateTime>("createdOn"),
                    EncryptedId = id.ToString().EncryptWithAesWithSalt(tenant.EncryptionKey, true),
                    Id = id,
                    LinkedItemEntityType = dataRow.Field<string>("linkedItemEntityType") ?? "",
                    LinkedItemId = linkedItemId.EncryptWithAesWithSalt(tenant.EncryptionKey, true),
                    LinkedItemModuleId = !Int32.TryParse(linkedItemModuleId, out var parsedLinkedItemModuleId) ? (int?)null : parsedLinkedItemModuleId,
                    ModuleId = dataRow.Field<int>("moduleid"),
                    PlacedBy = dataRow.Field<string>("placedBy") ?? "",
                    PlacedById = !UInt64.TryParse(placedById, out var parsedPlacedById) ? 0 : parsedPlacedById,
                    Status = dataRow.Field<string>("status") ?? "",
                    UserId = userId,
                    UserName = dataTable.Columns.Contains("userName") ? dataRow.Field<string>("userName") : ""
                });
            }

            return new ServiceResult<List<TaskAlertModel>>(results);
        }
    }
}
