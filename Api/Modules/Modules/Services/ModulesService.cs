using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Interfaces;
using Api.Core.Services;
using Api.Modules.Customers.Interfaces;
using Api.Modules.Grids.Interfaces;
using Api.Modules.Grids.Models;
using Api.Modules.Modules.Interfaces;
using Api.Modules.Modules.Models;
using Api.Modules.Queries.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Exports.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Modules.Services
{
    /// <summary>
    /// Service for getting information / settings for Wiser 2.0+ modules.
    /// </summary>
    public class ModulesService : IModulesService, IScopedService
    {
        private readonly IWiserCustomersService wiserCustomersService;
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IWiserItemsService wiserItemsService;
        private readonly IJsonService jsonService;
        private readonly IGridsService gridsService;
        private readonly IExcelService excelService;
        private readonly IObjectsService objectsService;

        /// <summary>
        /// Creates a new instance of <see cref="ModulesService"/>.
        /// </summary>
        public ModulesService(IWiserCustomersService wiserCustomersService, IGridsService gridsService, IDatabaseConnection clientDatabaseConnection, IWiserItemsService wiserItemsService, IJsonService jsonService, IExcelService excelService, IObjectsService objectsService)
        {
            this.wiserCustomersService = wiserCustomersService;
            this.gridsService = gridsService;
            this.wiserItemsService = wiserItemsService;
            this.jsonService = jsonService;
            this.excelService = excelService;
            this.objectsService = objectsService;
            this.clientDatabaseConnection = clientDatabaseConnection;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<SortedList<string, List<ModuleAccessRightsModel>>>> GetAsync(ClaimsIdentity identity)
        {
            var modulesForAdmins = new List<int>
            {
                700, // Stamgegevens
                706, // Data selector
                709, // Search module
                737, // Admin
                738, // Import / export
                806, // Wiser users
                5505 // Webpagina's
                // TODO: Add the new settings and templates modules here once they are finished.
            };

            var isAdminAccount = IdentityHelpers.IsAdminAccount(identity);

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("userId", IdentityHelpers.GetWiserUserId(identity));

            var query = $@"(
                            SELECT
	                            permission.module_id,
	                            MAX(permission.permissions) AS permissions,
                                ordering.`order`,
                                module.name,
                                module.icon,
                                module.color,
                                module.type,
                                module.group,
                                IF(NOT JSON_VALID(module.options), 'false', JSON_EXTRACT(module.options, '$.onlyOneInstanceAllowed')) AS onlyOneInstanceAllowed
                            FROM {WiserTableNames.WiserUserRoles} AS user_role
                            JOIN {WiserTableNames.WiserRoles} AS role ON role.id = user_role.role_id
                            JOIN {WiserTableNames.WiserPermission} AS permission ON permission.role_id = role.id AND permission.module_id > 0
                            LEFT JOIN {WiserTableNames.WiserModule} AS module ON module.id = permission.module_id
                            LEFT JOIN {WiserTableNames.WiserOrdering} AS ordering ON ordering.user_id = user_role.user_id AND ordering.module_id = permission.module_id
                            WHERE user_role.user_id = ?userId
                            GROUP BY permission.module_id
                            ORDER BY permission.module_id, permission.permissions
                        )";

            if (isAdminAccount)
            {
                query += $@"
                        UNION
                        (
                            SELECT
                                module.id AS module_id,
                                15 AS permissions,
                                ordering.`order`,
                                module.name,
                                module.icon,
                                module.color,
                                module.type,
                                module.group,
                                IF(NOT JSON_VALID(module.options), 'false', JSON_EXTRACT(module.options, '$.onlyOneInstanceAllowed')) AS onlyOneInstanceAllowed
                            FROM {WiserTableNames.WiserModule} AS module
                            LEFT JOIN {WiserTableNames.WiserOrdering} AS ordering ON ordering.user_id = ?userId AND ordering.module_id = module.id
                            WHERE module.id IN ({String.Join(",", modulesForAdmins)})
                        )";
            }

            var dataTable = await clientDatabaseConnection.GetAsync(query);
            var results = new SortedList<string, List<ModuleAccessRightsModel>>();
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<SortedList<string, List<ModuleAccessRightsModel>>>(results);
            }

            var onlyOneInstanceAllowedGlobal = String.Equals(await objectsService.FindSystemObjectByDomainNameAsync("wiser_modules_OnlyOneInstanceAllowed", "false"), "true", StringComparison.OrdinalIgnoreCase);
            foreach (DataRow dataRow in dataTable.Rows)
            {
                var moduleId = dataRow.Field<int>("module_id");
                var groupName = dataRow.Field<string>("group") ?? "";
                var permissionsBitMask = (AccessRights)Convert.ToInt32(dataRow["permissions"]);
                var ordering = dataRow.Field<int?>("order");

                var canRead = (permissionsBitMask & AccessRights.Read) == AccessRights.Read;
                var canCreate = (permissionsBitMask & AccessRights.Create) == AccessRights.Create;
                var canUpdate = (permissionsBitMask & AccessRights.Update) == AccessRights.Update;
                var canDelete = (permissionsBitMask & AccessRights.Delete) == AccessRights.Delete;

                if (!results.ContainsKey(groupName))
                {
                    results.Add(groupName, new List<ModuleAccessRightsModel>());
                }

                var rightsModel = results[groupName].FirstOrDefault(r => r.ModuleId == moduleId) ?? new ModuleAccessRightsModel { ModuleId = moduleId };

                rightsModel.CanRead = rightsModel.CanRead || canRead;
                rightsModel.CanCreate = rightsModel.CanCreate || canCreate;
                rightsModel.CanWrite = rightsModel.CanWrite || canUpdate;
                rightsModel.CanDelete = rightsModel.CanDelete || canDelete;
                rightsModel.Show = rightsModel.Show || (ordering ?? 0) > 0;
                rightsModel.MetroOrder = rightsModel.MetroOrder <= 0 ? (ordering ?? 0) : rightsModel.MetroOrder;
                rightsModel.Name = dataRow.Field<string>("name");
                rightsModel.Icon = dataRow.Field<string>("icon");
                rightsModel.Color = dataRow.Field<string>("color");
                rightsModel.Type = dataRow.Field<string>("type");
                rightsModel.Group = groupName;

                var onlyOneInstanceAllowed = dataRow.Field<string>("onlyOneInstanceAllowed");
                rightsModel.OnlyOneInstanceAllowed = (onlyOneInstanceAllowedGlobal && !String.Equals(onlyOneInstanceAllowed, "false", StringComparison.OrdinalIgnoreCase)) || String.Equals(onlyOneInstanceAllowed, "true", StringComparison.OrdinalIgnoreCase) || onlyOneInstanceAllowed == "1";

                results[groupName].Add(rightsModel);
            }

            // Make sure that we add certain modules for admins, even if those modules don't exist in wiser_module for this customer.
            if (isAdminAccount)
            {
                foreach (var moduleId in modulesForAdmins.Where(moduleId => !results.Any(g => g.Value.Any(m => m.ModuleId == moduleId))))
                {
                    // TODO: Add the new settings and templates modules here once they are finished.
                    switch (moduleId)
                    {
                        case 700: // Stamgegevens
                            if (!results.ContainsKey("Instellingen"))
                            {
                                results.Add("Instellingen", new List<ModuleAccessRightsModel>());
                            }

                            results["Instellingen"].Add(new ModuleAccessRightsModel
                            {
                                Group = "Instellingen",
                                CanCreate = true,
                                CanDelete = true,
                                CanRead = true,
                                CanWrite = true,
                                Icon = "controls",
                                ModuleId = moduleId,
                                Name = "Stamgegevens",
                                Type = "DynamicItems",
                                Show = true
                            });
                            break;
                        case 706: // Data selector
                            if (!results.ContainsKey("Contentbeheer"))
                            {
                                results.Add("Contentbeheer", new List<ModuleAccessRightsModel>());
                            }

                            results["Contentbeheer"].Add(new ModuleAccessRightsModel
                            {
                                Group = "Contentbeheer",
                                CanCreate = true,
                                CanDelete = true,
                                CanRead = true,
                                CanWrite = true,
                                Icon = "settings",
                                ModuleId = moduleId,
                                Name = "Data selector",
                                Type = "DataSelector",
                                Show = true
                            });
                            break;
                        case 709: // Search
                            if (!results.ContainsKey("Contentbeheer"))
                            {
                                results.Add("Contentbeheer", new List<ModuleAccessRightsModel>());
                            }

                            results["Contentbeheer"].Add(new ModuleAccessRightsModel
                            {
                                Group = "Contentbeheer",
                                CanCreate = true,
                                CanDelete = true,
                                CanRead = true,
                                CanWrite = true,
                                Icon = "search",
                                ModuleId = moduleId,
                                Name = "Zoeken",
                                Type = "Search",
                                Show = true
                            });
                            break;
                        case 737: // Admin
                            if (!results.ContainsKey("Instellingen"))
                            {
                                results.Add("Instellingen", new List<ModuleAccessRightsModel>());
                            }

                            results["Instellingen"].Add(new ModuleAccessRightsModel
                            {
                                Group = "Instellingen",
                                CanCreate = true,
                                CanDelete = true,
                                CanRead = true,
                                CanWrite = true,
                                Icon = "database",
                                ModuleId = moduleId,
                                Name = "Wiser beheer",
                                Type = "Admin",
                                Show = true
                            });
                            break;
                        case 738: // Import/export
                            if (!results.ContainsKey("Contentbeheer"))
                            {
                                results.Add("Contentbeheer", new List<ModuleAccessRightsModel>());
                            }

                            results["Contentbeheer"].Add(new ModuleAccessRightsModel
                            {
                                Group = "Contentbeheer",
                                CanCreate = true,
                                CanDelete = true,
                                CanRead = true,
                                CanWrite = true,
                                Icon = "database",
                                ModuleId = moduleId,
                                Name = "Import/export",
                                Type = "ImportExport",
                                Show = true
                            });
                            break;
                        case 806: // Wiser users
                            if (!results.ContainsKey("Instellingen"))
                            {
                                results.Add("Instellingen", new List<ModuleAccessRightsModel>());
                            }

                            results["Instellingen"].Add(new ModuleAccessRightsModel
                            {
                                Group = "Gebruikers - Wiser",
                                CanCreate = true,
                                CanDelete = true,
                                CanRead = true,
                                CanWrite = true,
                                Icon = "users",
                                ModuleId = moduleId,
                                Name = "Wiser beheer",
                                Type = "DynamicItems",
                                Show = true
                            });
                            break;
                        case 5505: // Webpagina's
                            if (!results.ContainsKey("Contentbeheer"))
                            {
                                results.Add("Contentbeheer", new List<ModuleAccessRightsModel>());
                            }

                            results["Contentbeheer"].Add(new ModuleAccessRightsModel
                            {
                                Group = "Contentbeheer",
                                CanCreate = true,
                                CanDelete = true,
                                CanRead = true,
                                CanWrite = true,
                                Icon = "document-web",
                                ModuleId = moduleId,
                                Name = "Webpagina's 2.0",
                                Type = "DynamicItems",
                                Show = true
                            });
                            break;
                        default:
                            throw new NotImplementedException($"Trying to hard-code add module '{moduleId}' to list for admin account, but no case has been added for this module in the switch statement.");
                    }
                }
            }

            // Sort all the groups.
            var allKeys = results.Keys.ToList();
            foreach (var key in allKeys)
            {
                results[key] = results[key].OrderBy(m => m.Name).ToList();
            }

            return new ServiceResult<SortedList<string, List<ModuleAccessRightsModel>>>(results);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<ModuleSettingsModel>>> GetSettingsAsync(ClaimsIdentity identity)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync(); 
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("userId", IdentityHelpers.GetWiserUserId(identity));
            
            var results = new List<ModuleSettingsModel>();
            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT
	                                                                            module.id,
	                                                                            module.name
                                                                            FROM {WiserTableNames.WiserUserRoles} AS user_role
                                                                            JOIN {WiserTableNames.WiserRoles} AS role ON role.id = user_role.role_id
                                                                            JOIN {WiserTableNames.WiserPermission} AS permission ON permission.role_id = role.id AND permission.module_id > 0
                                                                            JOIN {WiserTableNames.WiserModule} AS module ON module.id = permission.module_id
                                                                            LEFT JOIN {WiserTableNames.WiserOrdering} AS ordering ON ordering.user_id = user_role.user_id AND ordering.module_id = permission.module_id
                                                                            WHERE user_role.user_id = ?userId
                                                                            GROUP BY permission.module_id
                                                                            ORDER BY permission.module_id, permission.permissions");
                        
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<List<ModuleSettingsModel>>(results);
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                var id = dataRow.Field<int>("id");
                var userItemPermissions = await wiserItemsService.GetUserModulePermissions(id, IdentityHelpers.GetWiserUserId(identity));
                results.Add(new ModuleSettingsModel
                {
                    Id = id,
                    Name = $"{dataTable.Rows[0].Field<string>("name")} ({id})",
                    CanRead = (userItemPermissions & AccessRights.Read) == AccessRights.Read,
                    CanCreate = (userItemPermissions & AccessRights.Create) == AccessRights.Create,
                    CanWrite = (userItemPermissions & AccessRights.Update) == AccessRights.Update,
                    CanDelete = (userItemPermissions & AccessRights.Delete) == AccessRights.Delete
                });
            }

            return new ServiceResult<List<ModuleSettingsModel>>(results);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<ModuleSettingsModel>> GetSettingsAsync(int id, ClaimsIdentity identity)
        {
            var customer = await wiserCustomersService.GetSingleAsync(identity);
            var encryptionKey = customer.ModelObject.EncryptionKey;

            var result = new ModuleSettingsModel { Id = id };
            
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            var userItemPermissions = await wiserItemsService.GetUserModulePermissions(id, IdentityHelpers.GetWiserUserId(identity));

            result.CanRead = (userItemPermissions & AccessRights.Read) == AccessRights.Read;
            result.CanCreate = (userItemPermissions & AccessRights.Create) == AccessRights.Create;
            result.CanWrite = (userItemPermissions & AccessRights.Update) == AccessRights.Update;
            result.CanDelete = (userItemPermissions & AccessRights.Delete) == AccessRights.Delete;

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", id);

            var query = $@"SELECT id, custom_query, count_query, `options`, `name`, icon, color, type, `group` FROM wiser_module WHERE id = ?id";
            var dataTable = await clientDatabaseConnection.GetAsync(query);

            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<ModuleSettingsModel>(result);
            }

            result.CustomQuery = dataTable.Rows[0].Field<string>("custom_query");
            result.CountQuery = dataTable.Rows[0].Field<string>("count_query");
            result.Name = dataTable.Rows[0].Field<string>("name");
            result.Icon = dataTable.Rows[0].Field<string>("icon");
            result.Color = dataTable.Rows[0].Field<string>("color");
            result.Type = dataTable.Rows[0].Field<string>("type");
            result.Group = dataTable.Rows[0].Field<string>("group");
            
            var optionsJson = dataTable.Rows[0].Field<string>("options");

            if (string.IsNullOrWhiteSpace(optionsJson))
            {
                return new ServiceResult<ModuleSettingsModel>(result);
            }

            var parsedOptionsJson = JToken.Parse(optionsJson);
            jsonService.EncryptValuesInJson(parsedOptionsJson, encryptionKey, new List<string> { "itemId" });

            result.Options = parsedOptionsJson;

            return new ServiceResult<ModuleSettingsModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<int>> CreateAsync(string name, ClaimsIdentity identity)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("name", name);
            
            var query = $@"
                        SET @newID = (SELECT MAX(id)+ 1 FROM wiser_module);
                        INSERT INTO {WiserTableNames.WiserModule}(id,`name`)
                        VALUES (@newID, ?name); 
                        INSERT IGNORE INTO {WiserTableNames.WiserPermission}(role_id,entity_name,item_id,entity_property_id, permissions,module_id)
                        VALUES (1, '', 0, 0, 15, @newID);
                        SELECT @newID;";
            
            var dataTable = await clientDatabaseConnection.GetAsync(query);
            var id = Convert.ToInt32(dataTable.Rows[0][0]);

            return new ServiceResult<int>(id);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<byte[]>> ExportAsync(int id, ClaimsIdentity identity)
        {
            var gridResult = await gridsService.GetOverviewGridDataAsync(id, new GridReadOptionsModel(), identity, true);
            if (gridResult.StatusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<byte[]>
                {
                    ErrorMessage = gridResult.ErrorMessage,
                    ReasonPhrase = gridResult.ReasonPhrase,
                    StatusCode = gridResult.StatusCode
                };
            }

            var newData = new JArray();
            var data = gridResult.ModelObject.Data;
            var columns = gridResult.ModelObject.Columns;
            foreach (var item in data)
            {
                var newObject = new JObject();
                foreach (var column in columns)
                {
                    newObject.Add(new JProperty(column.Title, item[column.Field]));
                }
                newData.Add(newObject);
            }

            var result = excelService.JsonArrayToExcel(newData);
            return new ServiceResult<byte[]>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> UpdateSettingsAsync(int id, ClaimsIdentity identity, ModuleSettingsModel moduleSettingsModel)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", id);
            clientDatabaseConnection.AddParameter("custom_query", moduleSettingsModel.CustomQuery);
            clientDatabaseConnection.AddParameter("count_query", moduleSettingsModel.CountQuery);
            clientDatabaseConnection.AddParameter("options", moduleSettingsModel.Options.ToString());
            clientDatabaseConnection.AddParameter("name", moduleSettingsModel.Name);
            clientDatabaseConnection.AddParameter("icon", moduleSettingsModel.Icon);
            clientDatabaseConnection.AddParameter("color", moduleSettingsModel.Color);
            clientDatabaseConnection.AddParameter("type", moduleSettingsModel.Type);
            clientDatabaseConnection.AddParameter("group", moduleSettingsModel.Group);

            var query = $@"UPDATE {WiserTableNames.WiserModule}
                            SET `custom_query` = ?custom_query,
                                `count_query` = ?count_query,
                                `options` = ?options,
                                `name` = ?name,
                                `icon` = ?icon,
                                `color` = ?color,
                                `type` = ?type,
                                `group` = ?group
                        WHERE id = ?id";
            try
            {
                await clientDatabaseConnection.ExecuteAsync(query);
                return new ServiceResult<bool>(true)
                {
                    StatusCode = HttpStatusCode.NoContent
                };
            }
            catch (Exception e)
            {
                return new ServiceResult<bool>(false)
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = e.Message
                };
            }
        }
    }
}
