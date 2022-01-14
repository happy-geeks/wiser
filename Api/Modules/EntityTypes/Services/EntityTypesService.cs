using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Customers.Interfaces;
using Api.Modules.EntityTypes.Interfaces;
using Api.Modules.EntityTypes.Models;
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
	                        entity.module_id
                        FROM {WiserTableNames.WiserEntity} AS entity
                        LEFT JOIN {WiserTableNames.WiserModule} AS module ON module.id = entity.module_id
                        WHERE entity.`name` <> ''
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
                    ModuleId = dataRow.Field<int>("module_id")
                });
            }

            return new ServiceResult<List<EntityTypeModel>>(result);
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
