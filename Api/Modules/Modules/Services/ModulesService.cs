using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Api.Core.Helpers;
using Api.Core.Interfaces;
using Api.Core.Services;
using Api.Modules.Tenants.Interfaces;
using Api.Modules.Grids.Interfaces;
using Api.Modules.Kendo.Models;
using Api.Modules.Modules.Interfaces;
using Api.Modules.Modules.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Exports.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Extensions;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Modules.Services
{
    /// <summary>
    /// Service for getting information / settings for Wiser modules.
    /// </summary>
    public class ModulesService : IModulesService, IScopedService
    {
        private readonly IWiserTenantsService wiserTenantsService;
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IWiserItemsService wiserItemsService;
        private readonly IJsonService jsonService;
        private readonly IGridsService gridsService;
        private readonly IExcelService excelService;
        private readonly IObjectsService objectsService;
        private readonly IUsersService usersService;
        private readonly IStringReplacementsService stringReplacementsService;
        private readonly ILogger<ModulesService> logger;
        private readonly IDatabaseHelpersService databaseHelpersService;
        private readonly ICsvService csvService;
        
        private const string DefaultModulesGroupName = "Overig";
        private const string PinnedModulesGroupName = "Vastgepind";

        /// <summary>
        /// Creates a new instance of <see cref="ModulesService"/>.
        /// </summary>
        public ModulesService(IWiserTenantsService wiserTenantsService, IGridsService gridsService,
            IDatabaseConnection clientDatabaseConnection, IWiserItemsService wiserItemsService,
            IJsonService jsonService, IExcelService excelService, IObjectsService objectsService,
            IUsersService usersService, IStringReplacementsService stringReplacementsService,
            ILogger<ModulesService> logger, IDatabaseHelpersService databaseHelpersService,
            ICsvService csvService)
        {
            this.wiserTenantsService = wiserTenantsService;
            this.gridsService = gridsService;
            this.wiserItemsService = wiserItemsService;
            this.jsonService = jsonService;
            this.excelService = excelService;
            this.objectsService = objectsService;
            this.usersService = usersService;
            this.stringReplacementsService = stringReplacementsService;
            this.logger = logger;
            this.databaseHelpersService = databaseHelpersService;
            this.csvService = csvService;
            this.clientDatabaseConnection = clientDatabaseConnection;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<Dictionary<string, List<ModuleAccessRightsModel>>>> GetAsync(ClaimsIdentity identity)
        {
            var modulesForAdmins = new List<int>
            {
                Constants.DefaultMasterDataModuleId,
                Constants.DefaultDataSelectorModuleId,
                Constants.DefaultSearchModuleId,
                Constants.DefaultAdminModuleId,
                Constants.DefaultImportExportModuleId,
                Constants.DefaultWiserUsersModuleId,
                Constants.DefaultWebpagesModuleId,
                Constants.DefaultTemplatesModuleId,
                Constants.DefaultVersionControlModuleId
            };

            var isAdminAccount = IdentityHelpers.IsAdminAccount(identity);
            var pinnedModules = (await usersService.GetPinnedModulesAsync(identity)).ModelObject;
            var autoLoadModules = (await usersService.GetAutoLoadModulesAsync(identity)).ModelObject;

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

            var lastTableUpdates = await databaseHelpersService.GetLastTableUpdatesAsync(clientDatabaseConnection.ConnectedDatabase);

            // If the table changes don't contain wiser_itemdetail, it means this is an older database.
            // We can assume that this database already has the able, otherwise nothing would work.
            // We add that table to the list of last table updates, so that the GCL doesn't try to add any indexes that might be missing,
            // because that takes a very long time for old databases with lots of data.
            if (!lastTableUpdates.ContainsKey(WiserTableNames.WiserItemDetail))
            {
                await clientDatabaseConnection.ExecuteAsync($"INSERT IGNORE INTO {WiserTableNames.WiserTableChanges} (name, last_update) VALUES ('{WiserTableNames.WiserItemDetail}', '{DateTime.Now:yyyy-MM-dd HH:mm:ss}')");
                lastTableUpdates.TryAdd(WiserTableNames.WiserItemDetail, DateTime.Now);
            }

            // Make sure that Wiser tables are up-to-date.
            const string TriggersName = "wiser_triggers";
            await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> {
                WiserTableNames.WiserEntity,
                WiserTableNames.WiserEntityProperty,
                WiserTableNames.WiserLink
            });
            await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> {
                WiserTableNames.WiserItem,
                WiserTableNames.WiserItemDetail,
                WiserTableNames.WiserModule ,
                WiserTableNames.WiserItemFile,
                WiserTableNames.WiserItemLink,
                WiserTableNames.WiserItemLinkDetail,
                WiserTableNames.WiserDataSelector,
                WiserTableNames.WiserTemplate,
                WiserTableNames.WiserDynamicContent,
                WiserTableNames.WiserTemplateDynamicContent,
                WiserTableNames.WiserTemplatePublishLog,
                WiserTableNames.WiserPreviewProfiles,
                WiserTableNames.WiserDynamicContentPublishLog,
                WiserTableNames.WiserQuery,
                WiserTableNames.WiserPermission,
                WiserTableNames.WiserCommunication,
                WiserTableNames.WiserStyledOutput,
                GeeksCoreLibrary.Modules.Databases.Models.Constants.DatabaseConnectionLogTableName
            });

            // Make sure that all triggers for Wiser tables are up-to-date.
            if (!lastTableUpdates.ContainsKey(TriggersName) || lastTableUpdates[TriggersName] < new DateTime(2023, 12, 14))
            {
                var createTriggersQuery = await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.CreateTriggers.sql");
                await clientDatabaseConnection.ExecuteAsync(createTriggersQuery);

                // Update wiser_table_changes.
                clientDatabaseConnection.AddParameter("tableName", TriggersName);
                clientDatabaseConnection.AddParameter("lastUpdate", DateTime.Now);
                await clientDatabaseConnection.ExecuteAsync($@"INSERT INTO {WiserTableNames.WiserTableChanges} (name, last_update) 
VALUES (?tableName, ?lastUpdate) 
ON DUPLICATE KEY UPDATE last_update = VALUES(last_update)");
                clientDatabaseConnection.ClearParameters();
            }

            const string TemplateCachingName = "wiser_template_caching";
            if (!lastTableUpdates.TryGetValue(TemplateCachingName, out var updateDate) || updateDate < new DateTime(2023, 6, 2))
            {
                if (await databaseHelpersService.ColumnExistsAsync(WiserTableNames.WiserTemplate, "use_cache"))
                {
                    const string CacheUpdateQuery = @$"UPDATE {WiserTableNames.WiserTemplate}
SET cache_per_url = IF(use_cache >= 3, 1, 0),
    cache_per_querystring = IF(use_cache >= 4, 1, 0),
    cache_per_hostname = IF(use_cache = 5, 1, 0),
    cache_using_regex = IF(use_cache = 6, 1, 0),
    cache_minutes = IF(use_cache = 0 AND cache_minutes <= 0, -1, cache_minutes)";
                    await clientDatabaseConnection.ExecuteAsync(CacheUpdateQuery);
                }

                // Update wiser_table_changes.
                clientDatabaseConnection.AddParameter("tableName", TemplateCachingName);
                clientDatabaseConnection.AddParameter("lastUpdate", DateTime.Now);
                await clientDatabaseConnection.ExecuteAsync($@"INSERT INTO {WiserTableNames.WiserTableChanges} (name, last_update) 
VALUES (?tableName, ?lastUpdate) 
ON DUPLICATE KEY UPDATE last_update = VALUES(last_update)");
                clientDatabaseConnection.ClearParameters();
            }

            clientDatabaseConnection.AddParameter("userId", IdentityHelpers.GetWiserUserId(identity));

            var query = $@"(
    SELECT
	    permission.module_id,
	    MAX(permission.permissions) AS permissions,
        module.name,
        module.icon,
        module.type,
        module.group,
        module.options,
        module.custom_query
    FROM {WiserTableNames.WiserUserRoles} AS user_role
    JOIN {WiserTableNames.WiserRoles} AS role ON role.id = user_role.role_id
    JOIN {WiserTableNames.WiserPermission} AS permission ON permission.role_id = role.id AND permission.module_id > 0
    JOIN {WiserTableNames.WiserModule} AS module ON module.id = permission.module_id
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
        module.name,
        module.icon,
        module.type,
        module.group,
        module.options,
        module.custom_query
    FROM {WiserTableNames.WiserModule} AS module
    WHERE module.id IN ({String.Join(",", modulesForAdmins)})
)";
            }

            var dataTable = await clientDatabaseConnection.GetAsync(query);
            var results = new Dictionary<string, List<ModuleAccessRightsModel>>();
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<Dictionary<string, List<ModuleAccessRightsModel>>>(results);
            }

            var onlyOneInstanceAllowedGlobal = String.Equals(await objectsService.FindSystemObjectByDomainNameAsync("wiser_modules_OnlyOneInstanceAllowed",                                                                                                                     "false"), "true", StringComparison.OrdinalIgnoreCase);

            foreach (DataRow dataRow in dataTable.Rows)
            {
                var moduleId = dataRow.Field<int>("module_id");
                var originalGroupName = dataRow.Field<string>("group");
                var groupName = pinnedModules.Contains(moduleId)
                    ? PinnedModulesGroupName
                    : dataRow.Field<string>("group");
                var permissionsBitMask = (AccessRights) Convert.ToInt32(dataRow["permissions"]);
                var options = dataRow.Field<string>("options");

                var canRead = (permissionsBitMask & AccessRights.Read) == AccessRights.Read;
                if (!canRead)
                {
                    // Don't add the module if the user has no read permissions for it.
                    continue;
                }

                var canCreate = (permissionsBitMask & AccessRights.Create) == AccessRights.Create;
                var canUpdate = (permissionsBitMask & AccessRights.Update) == AccessRights.Update;
                var canDelete = (permissionsBitMask & AccessRights.Delete) == AccessRights.Delete;

                var hasCustomQuery = !String.IsNullOrWhiteSpace(dataRow.Field<string>("custom_query"));

                if (String.IsNullOrWhiteSpace(groupName))
                {
                    groupName = DefaultModulesGroupName;
                }

                if (String.IsNullOrWhiteSpace(originalGroupName))
                {
                    originalGroupName = DefaultModulesGroupName;
                }

                if (!results.ContainsKey(groupName))
                {
                    results.Add(groupName, new List<ModuleAccessRightsModel>());
                }

                var rightsModel = results[groupName].FirstOrDefault(r => r.ModuleId == moduleId) ?? new ModuleAccessRightsModel {ModuleId = moduleId};

                rightsModel.CanRead = rightsModel.CanRead || canRead;
                rightsModel.CanCreate = rightsModel.CanCreate || canCreate;
                rightsModel.CanWrite = rightsModel.CanWrite || canUpdate;
                rightsModel.CanDelete = rightsModel.CanDelete || canDelete;
                rightsModel.Name = dataRow.Field<string>("name");
                rightsModel.Icon = dataRow.Field<string>("icon");
                rightsModel.Type = dataRow.Field<string>("type");
                rightsModel.Group = originalGroupName;
                rightsModel.Pinned = pinnedModules.Contains(moduleId);
                rightsModel.AutoLoad = autoLoadModules.Contains(moduleId);
                rightsModel.PinnedGroup = PinnedModulesGroupName;
                rightsModel.HasCustomQuery = hasCustomQuery;

                if (String.IsNullOrWhiteSpace(rightsModel.Icon))
                {
                    rightsModel.Icon = "question";
                }

                if (!String.IsNullOrWhiteSpace(options))
                {
                    var optionsObject = new JObject();
                    try
                    {
                        optionsObject = JObject.Parse(options);
                    }
                    catch (JsonReaderException exception)
                    {
                        logger.LogWarning(exception, $"An error occurred while parsing options JSON of module {rightsModel.ModuleId}");
                    }

                    var onlyOneInstanceAllowed = optionsObject.Value<bool?>("onlyOneInstanceAllowed");
                    rightsModel.OnlyOneInstanceAllowed = (onlyOneInstanceAllowedGlobal && (!onlyOneInstanceAllowed.HasValue || onlyOneInstanceAllowed.Value)) || (onlyOneInstanceAllowed.HasValue && onlyOneInstanceAllowed.Value);

                    if (rightsModel.Type.Equals("Iframe", StringComparison.OrdinalIgnoreCase))
                    {
                        var url = optionsObject.Value<string>("url");
                        if (String.IsNullOrWhiteSpace(url))
                        {
                            continue;
                        }

                        try
                        {
                            var moduleQuery = dataRow.Field<string>("custom_query");
                            if (!String.IsNullOrWhiteSpace(moduleQuery))
                            {
                                moduleQuery = moduleQuery.ReplaceCaseInsensitive("{userId}", IdentityHelpers.GetWiserUserId(identity).ToString());
                                moduleQuery = moduleQuery.ReplaceCaseInsensitive("{userId_encrypt}", HttpUtility.UrlEncode(IdentityHelpers.GetWiserUserId(identity).ToString().Encrypt()));
                                moduleQuery = moduleQuery.ReplaceCaseInsensitive("{userId_encrypt_withdate}", (string)HttpUtility.UrlEncode(IdentityHelpers.GetWiserUserId(identity).ToString().Encrypt(true)));
                                moduleQuery = moduleQuery.ReplaceCaseInsensitive("{username}", IdentityHelpers.GetUserName(identity) ?? "");
                                moduleQuery = moduleQuery.ReplaceCaseInsensitive("{userEmailAddress}", IdentityHelpers.GetEmailAddress(identity) ?? "");
                                moduleQuery = moduleQuery.ReplaceCaseInsensitive("{userType}", IdentityHelpers.GetRoles(identity) ?? "");

                                var moduleDataTable = await clientDatabaseConnection.GetAsync(moduleQuery);
                                if (moduleDataTable.Rows.Count > 0)
                                {
                                    url = stringReplacementsService.DoReplacements(url, moduleDataTable.Rows[0]);
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            logger.LogWarning(exception, $"An error occurred while executing query of module {rightsModel.ModuleId}");
                        }

                        rightsModel.IframeUrl = url;
                    }
                }

                results[groupName].Add(rightsModel);
            }

            // Make sure that we add certain modules for admins, even if those modules don't exist in wiser_module for this tenant.
            if (isAdminAccount)
            {
                foreach (var moduleId in modulesForAdmins.Where(moduleId => !results.Any(g => g.Value.Any(m => m.ModuleId == moduleId))))
                {
                    string groupName;
                    var isPinned = pinnedModules.Contains(moduleId);
                    switch (moduleId)
                    {
                        case Constants.DefaultMasterDataModuleId:
                            groupName = isPinned ? PinnedModulesGroupName : "Instellingen";
                            if (!results.ContainsKey(groupName))
                            {
                                results.Add(groupName, new List<ModuleAccessRightsModel>());
                            }

                            results[groupName].Add(new ModuleAccessRightsModel
                            {
                                Group = "Instellingen",
                                CanCreate = true,
                                CanDelete = true,
                                CanRead = true,
                                CanWrite = true,
                                Icon = "line-sliders",
                                ModuleId = moduleId,
                                Name = "Stamgegevens",
                                Type = "MasterData",
                                Pinned = isPinned,
                                PinnedGroup = PinnedModulesGroupName
                            });
                            break;
                        case Constants.DefaultDataSelectorModuleId:
                            groupName = isPinned ? PinnedModulesGroupName : "Contentbeheer";
                            if (!results.ContainsKey(groupName))
                            {
                                results.Add(groupName, new List<ModuleAccessRightsModel>());
                            }

                            results[groupName].Add(new ModuleAccessRightsModel
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
                                Pinned = isPinned,
                                PinnedGroup = PinnedModulesGroupName
                            });
                            break;
                        case Constants.DefaultSearchModuleId:
                            groupName = isPinned ? PinnedModulesGroupName : "Contentbeheer";
                            if (!results.ContainsKey(groupName))
                            {
                                results.Add(groupName, new List<ModuleAccessRightsModel>());
                            }

                            results[groupName].Add(new ModuleAccessRightsModel
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
                                Pinned = isPinned,
                                PinnedGroup = PinnedModulesGroupName
                            });
                            break;
                        case Constants.DefaultAdminModuleId:
                            groupName = isPinned ? PinnedModulesGroupName : "Instellingen";
                            if (!results.ContainsKey(groupName))
                            {
                                results.Add(groupName, new List<ModuleAccessRightsModel>());
                            }

                            results[groupName].Add(new ModuleAccessRightsModel
                            {
                                Group = groupName,
                                CanCreate = true,
                                CanDelete = true,
                                CanRead = true,
                                CanWrite = true,
                                Icon = "line-settings",
                                ModuleId = moduleId,
                                Name = "Wiser beheer",
                                Type = "Admin",
                                Pinned = isPinned,
                                PinnedGroup = PinnedModulesGroupName
                            });
                            break;
                        case Constants.DefaultImportExportModuleId:
                            groupName = isPinned ? PinnedModulesGroupName : "Contentbeheer";
                            if (!results.ContainsKey(groupName))
                            {
                                results.Add(groupName, new List<ModuleAccessRightsModel>());
                            }

                            results[groupName].Add(new ModuleAccessRightsModel
                            {
                                Group = "Contentbeheer",
                                CanCreate = true,
                                CanDelete = true,
                                CanRead = true,
                                CanWrite = true,
                                Icon = "im-ex",
                                ModuleId = moduleId,
                                Name = "Import/export",
                                Type = "ImportExport",
                                Pinned = isPinned,
                                PinnedGroup = PinnedModulesGroupName
                            });
                            break;
                        case Constants.DefaultWiserUsersModuleId:
                            groupName = isPinned ? PinnedModulesGroupName : "Instellingen";
                            if (!results.ContainsKey(groupName))
                            {
                                results.Add(groupName, new List<ModuleAccessRightsModel>());
                            }

                            results[groupName].Add(new ModuleAccessRightsModel
                            {
                                Group = "Gebruikers - Wiser",
                                CanCreate = true,
                                CanDelete = true,
                                CanRead = true,
                                CanWrite = true,
                                Icon = "users",
                                ModuleId = moduleId,
                                Name = "Wiser beheer",
                                Type = "Users",
                                Pinned = isPinned,
                                PinnedGroup = PinnedModulesGroupName
                            });
                            break;
                        case Constants.DefaultWebpagesModuleId:
                            groupName = isPinned ? PinnedModulesGroupName : "Contentbeheer";
                            if (!results.ContainsKey(groupName))
                            {
                                results.Add(groupName, new List<ModuleAccessRightsModel>());
                            }

                            results[groupName].Add(new ModuleAccessRightsModel
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
                                Pinned = isPinned,
                                PinnedGroup = PinnedModulesGroupName
                            });
                            break;
                        case Constants.DefaultVersionControlModuleId:
                            groupName = isPinned ? PinnedModulesGroupName : "Systeem";
                            if (!results.ContainsKey(groupName))
                            {
                                results.Add(groupName, new List<ModuleAccessRightsModel>());
                            }
                            results[groupName].Add(new ModuleAccessRightsModel
                            {
                                Group = "Systeem",
                                CanCreate = true,
                                CanDelete = true,
                                CanRead = true,
                                CanWrite = true,
                                Icon = "git",
                                ModuleId = moduleId,
                                Name = "Versiebeheer",
                                Type = "VersionControl",
                                Pinned = isPinned,
                                PinnedGroup = PinnedModulesGroupName
                            });
                            break;
                        case Constants.DefaultTemplatesModuleId:
                            groupName = isPinned ? PinnedModulesGroupName : "Systeem";
                            if (!results.ContainsKey(groupName))
                            {
                                results.Add(groupName, new List<ModuleAccessRightsModel>());
                            }
                            results[groupName].Add(new ModuleAccessRightsModel
                            {
                                Group = "Systeem",
                                CanCreate = true,
                                CanDelete = true,
                                CanRead = true,
                                CanWrite = true,
                                Icon = "document-fold",
                                ModuleId = moduleId,
                                Name = "Templates",
                                Type = "Templates",
                                Pinned = isPinned,
                                PinnedGroup = PinnedModulesGroupName
                            });
                            break;
                        default:
                            throw new NotImplementedException($"Trying to hard-code add module '{moduleId}' to list for admin account, but no case has been added for this module in the switch statement.");
                    }
                }
            }

            // Everyone should always have the configuration module.
            if (results.All(g => g.Value.All(m => m.Type != "Configuration")))
            {
                var isPinned = pinnedModules.Contains(0);
                var groupName = isPinned ? PinnedModulesGroupName : "Systeem";
                if (!results.ContainsKey(groupName))
                {
                    results.Add(groupName, new List<ModuleAccessRightsModel>());
                }
                results[groupName].Add(new ModuleAccessRightsModel
                {
                    Group = "Systeem",
                    CanCreate = true,
                    CanDelete = true,
                    CanRead = true,
                    CanWrite = true,
                    Icon = "config",
                    ModuleId = 0,
                    Name = "Wiser Configuratie",
                    Type = "Configuration",
                    Pinned = isPinned,
                    PinnedGroup = PinnedModulesGroupName
                });
            }

            // Sort all the groups.
            var allKeys = results.Keys.ToList();
            foreach (var key in allKeys)
            {
                results[key] = results[key].OrderBy(m => m.Name).ToList();
            }

            results = results.OrderBy(g => g.Key == PinnedModulesGroupName ? "-" : g.Key).ToDictionary(g => g.Key, g => g.Value);

            return new ServiceResult<Dictionary<string, List<ModuleAccessRightsModel>>>(results);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<ModuleSettingsModel>>> GetSettingsAsync(ClaimsIdentity identity)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("userId", IdentityHelpers.GetWiserUserId(identity));

            var isAdminAccount = IdentityHelpers.IsAdminAccount(identity);
            var query = isAdminAccount
                ? $@"SELECT
	                        module.id,
	                        module.name
                        FROM {WiserTableNames.WiserModule} AS module
                        ORDER BY module.id ASC;"
                : $@"SELECT
	                        module.id,
	                        module.name
                        FROM {WiserTableNames.WiserUserRoles} AS user_role
                        JOIN {WiserTableNames.WiserRoles} AS role ON role.id = user_role.role_id
                        JOIN {WiserTableNames.WiserPermission} AS permission ON permission.role_id = role.id AND permission.module_id > 0
                        JOIN {WiserTableNames.WiserModule} AS module ON module.id = permission.module_id
                        WHERE user_role.user_id = ?userId
                        GROUP BY permission.module_id
                        ORDER BY permission.module_id, permission.permissions;";

            var results = new List<ModuleSettingsModel>();
            var dataTable = await clientDatabaseConnection.GetAsync(query);

            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<List<ModuleSettingsModel>>(results);
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                var moduleId = dataRow.Field<int>("id");
                var userItemPermissions = await wiserItemsService.GetUserModulePermissions(moduleId, IdentityHelpers.GetWiserUserId(identity));

                results.Add(new ModuleSettingsModel
                {
                    Id = moduleId,
                    Name = dataRow.Field<string>("name"),
                    CanRead = (userItemPermissions & AccessRights.Read) == AccessRights.Read,
                    CanCreate = (userItemPermissions & AccessRights.Create) == AccessRights.Create,
                    CanWrite = (userItemPermissions & AccessRights.Update) == AccessRights.Update,
                    CanDelete = (userItemPermissions & AccessRights.Delete) == AccessRights.Delete
                });
            }

            return new ServiceResult<List<ModuleSettingsModel>>(results);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<ModuleSettingsModel>> GetSettingsAsync(int id, ClaimsIdentity identity, bool encryptValues = true)
        {
            var tenant = await wiserTenantsService.GetSingleAsync(identity);
            var encryptionKey = tenant.ModelObject.EncryptionKey;

            var result = new ModuleSettingsModel {Id = id};

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            var userItemPermissions = await wiserItemsService.GetUserModulePermissions(id, IdentityHelpers.GetWiserUserId(identity));

            result.CanRead = (userItemPermissions & AccessRights.Read) == AccessRights.Read;
            result.CanCreate = (userItemPermissions & AccessRights.Create) == AccessRights.Create;
            result.CanWrite = (userItemPermissions & AccessRights.Update) == AccessRights.Update;
            result.CanDelete = (userItemPermissions & AccessRights.Delete) == AccessRights.Delete;

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", id);

            var query = $@"SELECT id, custom_query, count_query, `options`, `name`, icon, type, `group` FROM {WiserTableNames.WiserModule} WHERE id = ?id";
            var dataTable = await clientDatabaseConnection.GetAsync(query);

            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<ModuleSettingsModel>
                {
                    StatusCode = HttpStatusCode.NotFound
                };
            }

            result.CustomQuery = dataTable.Rows[0].Field<string>("custom_query");
            result.CountQuery = dataTable.Rows[0].Field<string>("count_query");
            result.Name = dataTable.Rows[0].Field<string>("name");
            result.Icon = dataTable.Rows[0].Field<string>("icon");
            result.Type = dataTable.Rows[0].Field<string>("type");
            result.Group = dataTable.Rows[0].Field<string>("group");

            var optionsJson = dataTable.Rows[0].Field<string>("options");

            if (String.IsNullOrWhiteSpace(optionsJson))
            {
                return new ServiceResult<ModuleSettingsModel>(result);
            }

            var parsedOptionsJson = JToken.Parse(optionsJson);

            if (encryptValues)
            {
                jsonService.EncryptValuesInJson(parsedOptionsJson, encryptionKey, new List<string> {"itemId"});
            }

            result.Options = parsedOptionsJson;

            return new ServiceResult<ModuleSettingsModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<int>> CreateAsync(string name, ClaimsIdentity identity)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("name", name);

            var query = $@"SET @newID = (SELECT MAX(id)+ 1 FROM wiser_module);
INSERT INTO {WiserTableNames.WiserModule} (id,`name`)
VALUES (@newID, ?name); 
INSERT IGNORE INTO {WiserTableNames.WiserPermission}(role_id,entity_name,item_id,entity_property_id, permissions,module_id)
VALUES (1, '', 0, 0, 15, @newID);
SELECT @newID;";

            var dataTable = await clientDatabaseConnection.GetAsync(query);
            var id = Convert.ToInt32(dataTable.Rows[0][0]);

            return new ServiceResult<int>(id);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<byte[]>> ExportToExcelAsync(int id, ClaimsIdentity identity)
        {
            var gridResult = await gridsService.GetOverviewGridDataAsync(id, new GridReadOptionsModel(), identity, true);
            if (gridResult.StatusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<byte[]>
                {
                    ErrorMessage = gridResult.ErrorMessage,
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
                    if (String.IsNullOrWhiteSpace(column.Field))
                    {
                        continue;
                    }

                    newObject.Add(new JProperty(column.Title, item[column.Field.ToLowerInvariant()]));
                }

                newData.Add(newObject);
            }

            var result = excelService.JsonArrayToExcel(newData);
            return new ServiceResult<byte[]>(result);
        }
        
        /// <inheritdoc />
        public async Task<ServiceResult<byte[]>> ExportToCsvAsync(int id, ClaimsIdentity identity, char separator)
        {
            var gridResult = await gridsService.GetOverviewGridDataAsync(id, new GridReadOptionsModel(), identity, true);
            if (gridResult.StatusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<byte[]>
                {
                    ErrorMessage = gridResult.ErrorMessage,
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
                    if (String.IsNullOrWhiteSpace(column.Field))
                    {
                        continue;
                    }

                    newObject.Add(new JProperty(column.Title, item[column.Field.ToLowerInvariant()]));
                }

                newData.Add(newObject);
            }

            var csvString = csvService.JsonArrayToCsv(newData);
            var result = Encoding.UTF8.GetBytes(csvString);
            return new ServiceResult<byte[]>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<string>>> GetModuleGroupsAsync(ClaimsIdentity identity)
        {
            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT DISTINCT `group`
FROM {WiserTableNames.WiserModule}
WHERE `group` IS NOT NULL
AND `group` <> ''
ORDER BY `group` ASC");
            var results = dataTable.Rows.Cast<DataRow>().Select(dataRow => dataRow.Field<string>("group"));
            return new ServiceResult<List<string>>(results.ToList());
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> DeleteAsync(ClaimsIdentity identity, int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("Id must be greater than 0.", nameof(id));
            }

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.AddParameter("id", id);

            // Delete the module itself and any permissions that have been set for the module.
            var query = $@"DELETE FROM {WiserTableNames.WiserModule} WHERE id = ?id;
DELETE FROM {WiserTableNames.WiserPermission} WHERE module_id = ?id;";
            await clientDatabaseConnection.ExecuteAsync(query);

            return new ServiceResult<bool>(true)
            {
                StatusCode = HttpStatusCode.NoContent
            };
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
            clientDatabaseConnection.AddParameter("type", moduleSettingsModel.Type);
            clientDatabaseConnection.AddParameter("group", moduleSettingsModel.Group);

            var query = $@"UPDATE {WiserTableNames.WiserModule}
SET `custom_query` = ?custom_query,
    `count_query` = ?count_query,
    `options` = IF(?options != '' AND ?options IS NOT NULL AND JSON_VALID(?options), ?options, ''),
    `name` = ?name,
    `icon` = ?icon,
    `type` = ?type,
    `group` = ?group
WHERE id = ?id";

            await clientDatabaseConnection.ExecuteAsync(query);
            return new ServiceResult<bool>(true)
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }
    }
}