﻿using System;
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
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace Api.Modules.EntityTypes.Services
{
    /// <inheritdoc cref="IEntityTypesService" />
    public class EntityTypesService : IEntityTypesService, IScopedService
    {
        private readonly IWiserCustomersService wiserCustomersService;
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IWiserItemsService wiserItemsService;

        /// <summary>
        /// Creates a new instance of <see cref="EntityTypesService"/>.
        /// </summary>
        public EntityTypesService(IWiserCustomersService wiserCustomersService, IDatabaseConnection clientDatabaseConnection, IWiserItemsService wiserItemsService)
        {
            this.wiserCustomersService = wiserCustomersService;
            this.clientDatabaseConnection = clientDatabaseConnection;
            this.wiserItemsService = wiserItemsService;
        }
        
        /// <inheritdoc />
        public async Task<ServiceResult<List<EntityTypeModel>>> GetAsync(ClaimsIdentity identity, bool onlyEntityTypesWithDisplayName = true)
        {
            var result = new List<EntityTypeModel>();
            var query = $@"SELECT 
	                        entity.name, 
	                        IF(entity.friendly_name IS NULL OR entity.friendly_name = '', entity.name, entity.friendly_name) AS displayName,
	                        entity.module_id,
                            module.`name` AS moduleName
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

            result.AddRange(dataTable.Rows.Cast<DataRow>().Select(dataRow => new EntityTypeModel
            {
                DisplayName = dataRow.Field<string>("displayName"), 
                Id = dataRow.Field<string>("name"), 
                ModuleId = dataRow.Field<int>("module_id"), 
                ModuleName = dataRow.Field<string>("moduleName")
            }));

            return new ServiceResult<List<EntityTypeModel>>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<EntitySettingsModel>> GetAsync(ClaimsIdentity identity, string entityType, int moduleId = 0)
        {
            var result = await wiserItemsService.GetEntityTypeSettingsAsync(entityType, moduleId);
            if (String.IsNullOrEmpty(result?.EntityType))
            {
                return new ServiceResult<EntitySettingsModel>
                {
                    StatusCode = HttpStatusCode.NotFound
                };
            }

            return new ServiceResult<EntitySettingsModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<EntityTypeModel>>> GetAvailableEntityTypesAsync(ClaimsIdentity identity, int moduleId, string parentId = null)
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

            var result = new List<EntityTypeModel>();
            var query = $@"SELECT 
                                childEntity.name, 
                                IF(childEntity.friendly_name IS NULL OR childEntity.friendly_name = '', childEntity.name, childEntity.friendly_name) AS displayName
                            FROM {WiserTableNames.WiserEntity} AS entity
                            LEFT JOIN {WiserTableNames.WiserItem} AS item ON item.entity_type = entity.name AND item.moduleid = entity.module_id
                            JOIN {WiserTableNames.WiserEntity} AS childEntity ON childEntity.module_id = ?moduleId AND childEntity.name <> '' AND FIND_IN_SET(childEntity.name, entity.accepted_childtypes)
                            WHERE entity.module_id = ?moduleId
                            AND ((?parentId = 0 AND entity.name = '') OR (?parentId > 0 AND item.id = ?parentId))
                            GROUP BY childEntity.name
                            ORDER BY childEntity.name";

            var dataTable = await clientDatabaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<List<EntityTypeModel>>(result);
            }

            result.AddRange(dataTable.Rows.Cast<DataRow>().Select(dataRow => new EntityTypeModel
            {
                Id = dataRow.Field<string>("name"),
                DisplayName = dataRow.Field<string>("displayName")
            }));

            return new ServiceResult<List<EntityTypeModel>>(result);
        }
    }
}
