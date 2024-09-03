using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Models;
using Api.Core.Services;
using Api.Modules.Branches.Interfaces;
using Api.Modules.Branches.Models;
using Api.Modules.Tenants.Enums;
using Api.Modules.Tenants.Interfaces;
using Api.Modules.Tenants.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Branches.Enumerations;
using GeeksCoreLibrary.Modules.Branches.Helpers;
using GeeksCoreLibrary.Modules.Branches.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Newtonsoft.Json;
using Constants = Api.Modules.Branches.Models.Constants;

namespace Api.Modules.Branches.Services
{
    /// <inheritdoc cref="IBranchesService" />
    public class BranchesService : IBranchesService, IScopedService
    {
        private readonly IWiserTenantsService wiserTenantsService;
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IDatabaseHelpersService databaseHelpersService;
        private readonly ILogger<BranchesService> logger;
        private readonly IWiserItemsService wiserItemsService;
        private readonly ApiSettings apiSettings;
        private readonly IDatabaseConnection wiserDatabaseConnection;

        /// <summary>
        /// Creates a new instance of <see cref="BranchesService"/>.
        /// </summary>
        public BranchesService(IWiserTenantsService wiserTenantsService, IDatabaseConnection connection, IDatabaseHelpersService databaseHelpersService, ILogger<BranchesService> logger, IWiserItemsService wiserItemsService, IOptions<ApiSettings> apiSettings)
        {
            this.wiserTenantsService = wiserTenantsService;
            this.clientDatabaseConnection = connection;
            this.databaseHelpersService = databaseHelpersService;
            this.logger = logger;
            this.wiserItemsService = wiserItemsService;
            this.apiSettings = apiSettings.Value;

            if (clientDatabaseConnection is ClientDatabaseConnection databaseConnection)
            {
                wiserDatabaseConnection = databaseConnection.WiserDatabaseConnection;
            }
        }

        /// <inheritdoc />
        public async Task<ServiceResult<TenantModel>> CreateAsync(ClaimsIdentity identity, CreateBranchSettingsModel settings)
        {
            if (String.IsNullOrWhiteSpace(settings?.Name))
            {
                return new ServiceResult<TenantModel>
                {
                    ErrorMessage = "Name is empty",
                    StatusCode = HttpStatusCode.BadRequest
                };
            }

            // Make sure the queue table exists and is up-to-date.
            await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> {WiserTableNames.WiserBranchesQueue});

            var currentTenant = (await wiserTenantsService.GetSingleAsync(identity, true)).ModelObject;
            var subDomain = currentTenant.SubDomain;
            var newTenantName = $"{currentTenant.Name} - {settings.Name}";
            var newTenantTitle = $"{currentTenant.WiserTitle} - {settings.Name}";

            // If the ID is not the same as the Tenant ID, it means this is not the main/production environment of this Tenant.
            // Then we want to get the sub domain of the main/production environment of the Tenant, to use as base for the new sub domain for the new environment.
            if (currentTenant.Id != currentTenant.TenantId)
            {
                wiserDatabaseConnection.AddParameter("tenantId", currentTenant.TenantId);
                var dataTable = await wiserDatabaseConnection.GetAsync($"SELECT subdomain, name, wiser_title FROM {ApiTableNames.WiserTenants} WHERE id = ?tenantId");
                if (dataTable.Rows.Count == 0)
                {
                    // This should never happen, hence the exception.
                    throw new Exception("Tenant not found");
                }

                subDomain = dataTable.Rows[0].Field<string>("subdomain");
                newTenantName = $"{dataTable.Rows[0].Field<string>("name")} - {settings.Name}";
                newTenantTitle = $"{dataTable.Rows[0].Field<string>("wiser_title")} - {settings.Name}";
            }

            // Create a valid database and sub domain name for the new environment.
            var databaseNameBuilder = new StringBuilder(settings.Name.Trim().ToLowerInvariant());
            databaseNameBuilder = Path.GetInvalidFileNameChars().Aggregate(databaseNameBuilder, (current, invalidChar) => current.Replace(invalidChar.ToString(), ""));
            databaseNameBuilder = databaseNameBuilder.Replace(@"\", "_").Replace(@"/", "_").Replace(".", "_").Replace(" ", "_").Replace(@"'", "_");

            var databaseName = $"{currentTenant.Database.DatabaseName}_{databaseNameBuilder}".ToMySqlSafeValue(false);
            if (databaseName.Length > 54)
            {
                databaseName = $"{databaseName[..54]}{DateTime.Now:yyMMddHHmm}";
            }

            subDomain += $"_{databaseNameBuilder}";

            // Make sure no tenant exists yet with this name and/or sub domain.
            var tenantExists = await wiserTenantsService.TenantExistsAsync(newTenantName, subDomain);
            if (tenantExists.StatusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<TenantModel>
                {
                    ErrorMessage = tenantExists.ErrorMessage,
                    StatusCode = tenantExists.StatusCode
                };
            }

            if (tenantExists.ModelObject != TenantExistsResults.Available)
            {
                return new ServiceResult<TenantModel>
                {
                    StatusCode = HttpStatusCode.Conflict,
                    ErrorMessage = $"Een branch met de naam '{settings.Name}' bestaat al."
                };
            }

            // Make sure the database doesn't exist yet.
            if (await databaseHelpersService.DatabaseExistsAsync(databaseName))
            {
                return new ServiceResult<TenantModel>
                {
                    StatusCode = HttpStatusCode.Conflict,
                    ErrorMessage = $"We hebben geprobeerd een database aan te maken met de naam '{databaseName}', echter bestaat deze al. Kies a.u.b. een andere naam, of neem contact op met ons."
                };
            }

            settings.NewCustomerName = newTenantName;
            settings.SubDomain = subDomain;
            settings.WiserTitle = newTenantTitle;
            settings.DatabaseName = databaseName;

            // Add the new tenant environment to easy_customers. We do this here already so that the WTS doesn't need access to the main wiser database.
            var newTenant = new TenantModel
            {
                TenantId = currentTenant.TenantId,
                Name = newTenantName,
                EncryptionKey = SecurityHelpers.GenerateRandomPassword(20),
                SubDomain = subDomain,
                WiserTitle = newTenantTitle,
                Database = new ConnectionInformationModel
                {
                    Host = currentTenant.Database.Host,
                    Password = currentTenant.Database.Password,
                    Username = currentTenant.Database.Username,
                    PortNumber = currentTenant.Database.PortNumber,
                    DatabaseName = databaseName
                }
            };

            if (!String.IsNullOrWhiteSpace(settings.DatabaseHost))
            {
                newTenant.Database.Host = settings.DatabaseHost;
                settings.DatabaseHost = settings.DatabaseHost.EncryptWithAesWithSalt(currentTenant.EncryptionKey, useSlowerButMoreSecureMethod: true);
            }
            if (settings.DatabasePort is > 0)
            {
                newTenant.Database.PortNumber = settings.DatabasePort.Value;
            }
            if (!String.IsNullOrWhiteSpace(settings.DatabaseUsername))
            {
                newTenant.Database.Username = settings.DatabaseUsername;
                settings.DatabaseUsername = settings.DatabaseUsername.EncryptWithAesWithSalt(currentTenant.EncryptionKey, useSlowerButMoreSecureMethod: true);
            }
            if (!String.IsNullOrWhiteSpace(settings.DatabasePassword))
            {
                newTenant.Database.Password = settings.DatabasePassword.EncryptWithAesWithSalt(apiSettings.DatabasePasswordEncryptionKey);
                settings.DatabasePassword = settings.DatabasePassword.EncryptWithAesWithSalt(currentTenant.EncryptionKey, useSlowerButMoreSecureMethod: true);
            }

            await wiserTenantsService.CreateOrUpdateTenantAsync(newTenant);

            // Clear some data that we don't want to return to client.
            newTenant.Database.Host = null;
            newTenant.Database.Password = null;
            newTenant.Database.Username = null;
            newTenant.Database.PortNumber = 0;

            // Add the creation of the branch to the queue, so that the WTS can process it.
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("name", settings.Name);
            clientDatabaseConnection.AddParameter("action", "create");
            clientDatabaseConnection.AddParameter("branch_id", newTenant.Id);
            clientDatabaseConnection.AddParameter("data", JsonConvert.SerializeObject(settings));
            clientDatabaseConnection.AddParameter("added_on", DateTime.Now);
            clientDatabaseConnection.AddParameter("start_on", settings.StartOn ?? DateTime.Now);
            clientDatabaseConnection.AddParameter("added_by", IdentityHelpers.GetUserName(identity, true));
            clientDatabaseConnection.AddParameter("user_id", IdentityHelpers.GetWiserUserId(identity));
            await clientDatabaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(WiserTableNames.WiserBranchesQueue, 0);

            return new ServiceResult<TenantModel>(newTenant);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<TenantModel>>> GetAsync(ClaimsIdentity identity)
        {
            // Make sure the queue table exists and is up-to-date.
            await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> {WiserTableNames.WiserBranchesQueue});

            var currentTenant = (await wiserTenantsService.GetSingleAsync(identity, true)).ModelObject;

            var query = $@"SELECT id, name, subdomain, db_dbname
FROM {ApiTableNames.WiserTenants}
WHERE customerId = ?id
AND id <> ?id
ORDER BY id DESC";

            wiserDatabaseConnection.AddParameter("id", currentTenant.TenantId);
            var dataTable = await wiserDatabaseConnection.GetAsync(query);
            var results = new List<TenantModel>();
            foreach (DataRow dataRow in dataTable.Rows)
            {
                results.Add(new TenantModel
                {
                    Id = dataRow.Field<int>("id"),
                    TenantId = currentTenant.TenantId,
                    Name = dataRow.Field<string>("name"),
                    SubDomain = dataRow.Field<string>("subdomain"),
                    Database = new ConnectionInformationModel
                    {
                        DatabaseName = dataRow.Field<string>("db_dbname")
                    }
                });
            }

            // Get the status of create branches.
            query = $@"SELECT
    branch_id,
    started_on,
    finished_on,
    success
FROM {WiserTableNames.WiserBranchesQueue}
WHERE action = 'create'";
            dataTable = await clientDatabaseConnection.GetAsync(query);
            foreach (DataRow dataRow in dataTable.Rows)
            {
                var id = dataRow.Field<int>("branch_id");
                var tenantModel = results.FirstOrDefault(tenant => tenant.Id == id);
                if (tenantModel == null)
                {
                    continue;
                }

                var startedOn = dataRow.Field<DateTime?>("started_on");
                var finishedOn = dataRow.Field<DateTime?>("finished_on");
                var success = !dataRow.IsNull("success") && Convert.ToBoolean(dataRow["success"]);
                var statusMessage = "";

                if (startedOn.HasValue && finishedOn.HasValue && !success)
                {
                    statusMessage = "Branch aanmaken mislukt";
                }
                else if (startedOn.HasValue && !finishedOn.HasValue)
                {
                    statusMessage = $"Nog bezig met aanmaken, begonnen om {startedOn.Value:dd-MM-yyyy HH:mm:ss}";
                }
                else if (!startedOn.HasValue)
                {
                    statusMessage = "Staat nog in wachtrij";
                }

                if (String.IsNullOrEmpty(statusMessage))
                {
                    continue;
                }

                tenantModel.Name += $" (Status: {statusMessage})";
            }

            return new ServiceResult<List<TenantModel>>(results);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> IsMainBranchAsync(ClaimsIdentity identity)
        {
            var currentBranch = (await wiserTenantsService.GetSingleAsync(identity, true)).ModelObject;

            return IsMainBranch(currentBranch);
        }

        /// <inheritdoc />
        public ServiceResult<bool> IsMainBranch(TenantModel branch)
        {
            return new ServiceResult<bool>(branch.Id == branch.TenantId);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<ChangesAvailableForMergingModel>> GetChangesAsync(ClaimsIdentity identity, int id, List<string> entityTypes)
        {
            entityTypes ??= new List<string>();
            var currentTenant = (await wiserTenantsService.GetSingleAsync(identity, true)).ModelObject;

            var result = new ChangesAvailableForMergingModel();

            // If the id is 0, then get the current branch where the user is authenticated, otherwise get the branch of the given ID.
            var selectedEnvironmentTenant = id <= 0
                ? currentTenant
                : (await wiserTenantsService.GetSingleAsync(id, true)).ModelObject;

            // Only allow users to get the changes of their own branches.
            if (currentTenant.TenantId != selectedEnvironmentTenant.TenantId)
            {
                return new ServiceResult<ChangesAvailableForMergingModel>
                {
                    StatusCode = HttpStatusCode.Forbidden
                };
            }

            await using var branchConnection = new MySqlConnection(wiserTenantsService.GenerateConnectionStringFromTenant(selectedEnvironmentTenant));
            await branchConnection.OpenAsync();

            // Get some data that we'll need later.
            var allLinkTypeSettings = await wiserItemsService.GetAllLinkTypeSettingsAsync();
            var tablePrefixes = new Dictionary<string, string>();

            var tablePrefixDataTable = new DataTable();
            await using (var branchCommand = branchConnection.CreateCommand())
            {
                branchCommand.CommandText = $"SELECT name, dedicated_table_prefix FROM {WiserTableNames.WiserEntity}";
                using var branchAdapter = new MySqlDataAdapter(branchCommand);
                branchAdapter.Fill(tablePrefixDataTable);
            }

            foreach (DataRow dataRow in tablePrefixDataTable.Rows)
            {
                var prefix = dataRow.Field<string>("dedicated_table_prefix");
                if (!String.IsNullOrWhiteSpace(prefix) && !prefix.EndsWith("_"))
                {
                    prefix += "_";
                }

                tablePrefixes.TryAdd(dataRow.Field<string>("name"), prefix);
            }

            var filesData = new Dictionary<string, Dictionary<ulong, (ulong itemId, ulong itemLinkId)>>();
            foreach (var (_, tablePrefix) in tablePrefixes)
            {
                var tableName = $"{tablePrefix}{WiserTableNames.WiserItemFile}";
                if (filesData.ContainsKey(tableName))
                {
                    continue;
                }

                var entityDictionary = new Dictionary<ulong, (ulong itemId, ulong itemLinkId)>();
                filesData.Add(tableName, entityDictionary);

                var filesTable = new DataTable();
                await using (var branchCommand = branchConnection.CreateCommand())
                {
                    branchCommand.CommandTimeout = Constants.SqlCommandTimeout;
                    branchCommand.CommandText = @$"SELECT id, item_id, itemlink_id FROM {tableName}
UNION ALL
SELECT id, item_id, itemlink_id FROM {tableName}{WiserTableNames.ArchiveSuffix}";
                    using var branchAdapter = new MySqlDataAdapter(branchCommand);
                    branchAdapter.Fill(filesTable);
                }

                foreach (DataRow dataRow in filesTable.Rows)
                {
                    entityDictionary.Add(Convert.ToUInt64(dataRow["id"]), (Convert.ToUInt64(dataRow["item_id"]), Convert.ToUInt64(dataRow["itemlink_id"])));
                }
            }

            // Get all history since last synchronisation.
            var dataTable = new DataTable();
            await using (var branchCommand = branchConnection.CreateCommand())
            {
                branchCommand.CommandTimeout = Constants.SqlCommandTimeout;
                branchCommand.CommandText = $"SELECT action, tablename, item_id, field, oldvalue, newvalue FROM `{WiserTableNames.WiserHistory}` ORDER BY id ASC";
                using var branchAdapter = new MySqlDataAdapter(branchCommand);
                branchAdapter.Fill(dataTable);
            }

            // Create lists for keeping track of changed items/settings, so that multiple changes to a single item/setting only get counted as one changed item/setting, because we're counting the amount of changed items/settings, not the amount of changes.
            var createdItems = new List<(string TablePrefix, string EntityType, ulong ItemId)>();
            var updatedItems = new List<(string TablePrefix, string EntityType, ulong ItemId)>();
            var deletedItems = new List<(string TablePrefix, string EntityType, ulong ItemId)>();
            var createdSettings = new Dictionary<WiserSettingTypes, List<ulong>>();
            var updatedSettings = new Dictionary<WiserSettingTypes, List<ulong>>();
            var deletedSettings = new Dictionary<WiserSettingTypes, List<ulong>>();

            // Local function that adds an item to one of the 3 lists above, to keep track of how many items have been created/updated/deleted.
            void AddItemToMutationList(ICollection<(string TablePrefix, string EntityType, ulong ItemId)> list, string tablePrefix, ulong itemId, string entityType = null)
            {
                var item = list.SingleOrDefault(x => x.TablePrefix == tablePrefix && x.ItemId == itemId);
                if (item.ItemId > 0)
                {
                    if (!String.IsNullOrWhiteSpace(entityType) && String.IsNullOrWhiteSpace(item.EntityType))
                    {
                        item.EntityType = entityType;
                    }

                    return;
                }

                list.Add((tablePrefix, entityType, itemId));
            }

            // Local function that adds a setting to one of the 3 lists above, to keep track of how many items have been created/updated/deleted.
            void AddSettingToMutationList(IDictionary<WiserSettingTypes, List<ulong>> list, WiserSettingTypes settingType, ulong settingId)
            {
                if (!list.ContainsKey(settingType))
                {
                    list.Add(settingType, new List<ulong>());
                }

                if (list[settingType].Contains(settingId))
                {
                    return;
                }

                list[settingType].Add(settingId);
            }

            // Local function to get a model for counting changes in Wiser settings.
            SettingsChangesModel GetOrAddWiserSettingCounter(WiserSettingTypes settingType)
            {
                var settingsChangesModel = result.Settings.FirstOrDefault(setting => setting.Type == settingType);
                if (settingsChangesModel != null)
                {
                    return settingsChangesModel;
                }

                settingsChangesModel = new SettingsChangesModel
                {
                    Type = settingType,
                    DisplayName = settingType switch
                    {
                        WiserSettingTypes.ApiConnection => "Verbindingen met API's",
                        WiserSettingTypes.DataSelector => "Dataselectors",
                        WiserSettingTypes.Entity => "Entiteiten",
                        WiserSettingTypes.EntityProperty => "Velden van entiteiten",
                        WiserSettingTypes.FieldTemplates => "Templates van velden",
                        WiserSettingTypes.Link => "Koppelingen",
                        WiserSettingTypes.Module => "Modules",
                        WiserSettingTypes.Permission => "Rechten",
                        WiserSettingTypes.Query => "Query's",
                        WiserSettingTypes.Role => "Rollen",
                        WiserSettingTypes.UserRole => "Koppelingen tussen gebruikers en rollen",
                        WiserSettingTypes.StyledOutput => "Styled output (Wiser API query output configuraties)",
                        _ => throw new ArgumentOutOfRangeException(nameof(settingType), settingType, null)
                    }
                };

                result.Settings.Add(settingsChangesModel);

                return settingsChangesModel;
            }

            // Local function to get a model for counting changes in an entity type.
            async Task<EntityChangesModel> GetOrAddEntityTypeCounterAsync(string entityType)
            {
                entityType ??= "unknown";

                var entityChangesModel = result.Entities.FirstOrDefault(setting => String.Equals(setting.EntityType, entityType, StringComparison.OrdinalIgnoreCase));
                if (entityChangesModel != null)
                {
                    return entityChangesModel;
                }

                var entityTypeSettings = await wiserItemsService.GetEntityTypeSettingsAsync(entityType);
                entityChangesModel = new EntityChangesModel
                {
                    EntityType = entityType,
                    DisplayName = entityType == "unknown" ? "Onbekend" : entityTypeSettings.DisplayName
                };

                if (String.IsNullOrWhiteSpace(entityChangesModel.DisplayName))
                {
                    entityChangesModel.DisplayName = entityType;
                }

                result.Entities.Add(entityChangesModel);

                return entityChangesModel;
            }

            // Local function to get the entity type of an item.
            var idToEntityTypeMappings = new Dictionary<ulong, string>();

            async Task<string> GetEntityTypeFromIdAsync(ulong itemId, string tablePrefix, MySqlConnection branchconnection)
            {
                if (idToEntityTypeMappings.TryGetValue(itemId, out var async))
                {
                    return async;
                }

                // Get the entity type from [prefix]wiser_item or [prefix]wiser_itemarchive if it doesn't exist in the first one.
                var getEntityTypeDataTable = new DataTable();
                await using (var environmentCommand = branchconnection.CreateCommand())
                {
                    environmentCommand.Parameters.AddWithValue("id", itemId);
                    environmentCommand.CommandText = $@"SELECT entity_type FROM `{tablePrefix}{WiserTableNames.WiserItem}` WHERE id = ?id
UNION ALL
SELECT entity_type FROM `{tablePrefix}{WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix}` WHERE id = ?id
LIMIT 1";
                    using var environmentAdapter = new MySqlDataAdapter(environmentCommand);
                    environmentAdapter.Fill(getEntityTypeDataTable);
                }

                var entityType = getEntityTypeDataTable.Rows.Count == 0 ? null : getEntityTypeDataTable.Rows[0].Field<string>("entity_type");
                idToEntityTypeMappings.Add(itemId, entityType);
                return entityType;
            }

            // Get the entity types and table prefixes for both items in a link.
            async Task<(string SourceType, string SourceTablePrefix, string DestinationType, string DestinationTablePrefix)?> GetEntityTypesOfLinkAsync(ulong sourceId, ulong destinationId, int linkType, MySqlConnection mySqlConnection, List<LinkSettingsModel> allLinkTypeSettings, Dictionary<string, string> tablePrefixes)
            {
                var currentLinkTypeSettings = allLinkTypeSettings.Where(l => l.Type == linkType).ToList();

                // If there are no settings for this link, we assume that the links are from items in the normal wiser_item table and not a table with a prefix.
                if (!currentLinkTypeSettings.Any())
                {
                    // Check if the source item exists in this table.
                    var sourceEntityType = await GetEntityTypeFromIdAsync(sourceId, "", mySqlConnection);

                    // Check if the destination item exists in this table.
                    var destinationEntityType = await GetEntityTypeFromIdAsync(destinationId, "", mySqlConnection);

                    return (sourceEntityType, "", destinationEntityType, "");
                }

                // It's possible that there are multiple link types that use the same number, so we have to check all of them.
                foreach (var linkTypeSettings in currentLinkTypeSettings)
                {
                    if (!tablePrefixes.TryGetValue(linkTypeSettings.SourceEntityType, out var sourceTablePrefix))
                    {
                        sourceTablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(linkTypeSettings.SourceEntityType);
                        tablePrefixes.Add(linkTypeSettings.SourceEntityType, sourceTablePrefix);
                    }

                    if (!tablePrefixes.TryGetValue(linkTypeSettings.DestinationEntityType, out var destinationTablePrefix))
                    {
                        destinationTablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(linkTypeSettings.DestinationEntityType);
                        tablePrefixes.Add(linkTypeSettings.DestinationEntityType, destinationTablePrefix);
                    }

                    // Check if the source item exists in this table.
                    var sourceEntityType = await GetEntityTypeFromIdAsync(sourceId, sourceTablePrefix, mySqlConnection);
                    if (!String.Equals(sourceEntityType, linkTypeSettings.SourceEntityType, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Check if the destination item exists in this table.
                    var destinationEntityType = await GetEntityTypeFromIdAsync(destinationId, destinationTablePrefix, mySqlConnection);
                    if (!String.Equals(destinationEntityType, linkTypeSettings.DestinationEntityType, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // If we reached this point, it means we found the correct link type and entity types.
                    return (linkTypeSettings.SourceEntityType, sourceTablePrefix, linkTypeSettings.DestinationEntityType, destinationTablePrefix);
                }

                return null;
            }

            // Local function to get the type number, source item ID and the destination item ID from a link.
            async Task<(int Type, ulong SourceItemId, ulong DestinationItemId)?> GetDataFromLinkAsync(ulong linkId, string tablePrefix, MySqlConnection connection)
            {
                var linkDataTable = new DataTable();
                await using var linkCommand = connection.CreateCommand();
                linkCommand.Parameters.AddWithValue("id", linkId);
                linkCommand.CommandText = $@"SELECT type, item_id, destination_item_id FROM `{tablePrefix}{WiserTableNames.WiserItemLink}` WHERE id = ?id
UNION ALL
SELECT type, item_id, destination_item_id FROM `{tablePrefix}{WiserTableNames.WiserItemLink}{WiserTableNames.ArchiveSuffix}` WHERE id = ?id
LIMIT 1";
                using var linkAdapter = new MySqlDataAdapter(linkCommand);
                linkAdapter.Fill(linkDataTable);

                if (linkDataTable.Rows.Count == 0)
                {
                    return null;
                }

                return (linkDataTable.Rows[0].Field<int>("type"), Convert.ToUInt64(linkDataTable.Rows[0]["item_id"]), Convert.ToUInt64(linkDataTable.Rows[0]["destination_item_id"]));
            }

            // Count all changed items and settings (if a single item has been changed multiple times, we count only one change).
            var times = new Dictionary<string, (int count, TimeSpan totalTime)>();
            var mainStopwatch = new Stopwatch();
            mainStopwatch.Start();
            var actionStopwatch = new Stopwatch();

            // First make a list of all objects that have been created and then deleted again, so that we can ignore them.
            actionStopwatch.Start();
            var objectsCreatedInBranch = new List<ObjectCreatedInBranchModel>();
            foreach (DataRow dataRow in dataTable.Rows)
            {
                var tableName = dataRow.Field<string>("tablename");
                if (String.IsNullOrWhiteSpace(tableName))
                {
                    continue;
                }

                var originalItemId = Convert.ToUInt64(dataRow["item_id"]);
                var action = dataRow.Field<string>("action").ToUpperInvariant();
                BranchesHelpers.TrackObjectAction(objectsCreatedInBranch, action, originalItemId.ToString(), tableName);
            }

            times.Add("trackObjects", (1, actionStopwatch.Elapsed));
            actionStopwatch.Reset();

            // Count the actual changes.
            foreach (DataRow dataRow in dataTable.Rows)
            {
                actionStopwatch.Start();
                var action = dataRow.Field<string>("action")?.ToUpperInvariant();
                var tableName = dataRow.Field<string>("tablename") ?? "";
                var itemId = dataRow.Field<ulong>("item_id");
                var field = dataRow.Field<string>("field") ?? "";
                var oldValue = dataRow.Field<string>("oldvalue");
                var newValue = dataRow.Field<string>("newvalue");
                var itemIdString = itemId.ToString();
                var objectCreatedInBranch = objectsCreatedInBranch.FirstOrDefault(i => i.ObjectId == itemIdString && String.Equals(i.TableName, tableName, StringComparison.OrdinalIgnoreCase));

                if (objectCreatedInBranch is not {AlsoDeleted: true} || objectCreatedInBranch.AlsoUndeleted)
                {
                    switch (action)
                    {
                        // Changes to settings.
                        case "INSERT_ENTITYPROPERTY":
                        {
                            AddSettingToMutationList(createdSettings, WiserSettingTypes.EntityProperty, itemId);
                            break;
                        }
                        case "UPDATE_ENTITYPROPERTY":
                        {
                            AddSettingToMutationList(updatedSettings, WiserSettingTypes.EntityProperty, itemId);
                            break;
                        }
                        case "DELETE_ENTITYPROPERTY":
                        {
                            AddSettingToMutationList(deletedSettings, WiserSettingTypes.EntityProperty, itemId);
                            break;
                        }
                        case "INSERT_MODULE":
                        {
                            AddSettingToMutationList(createdSettings, WiserSettingTypes.Module, itemId);
                            break;
                        }
                        case "UPDATE_MODULE":
                        {
                            AddSettingToMutationList(updatedSettings, WiserSettingTypes.Module, itemId);
                            break;
                        }
                        case "DELETE_MODULE":
                        {
                            AddSettingToMutationList(deletedSettings, WiserSettingTypes.Module, itemId);
                            break;
                        }
                        case "INSERT_QUERY":
                        {
                            AddSettingToMutationList(createdSettings, WiserSettingTypes.Query, itemId);
                            break;
                        }
                        case "UPDATE_QUERY":
                        {
                            AddSettingToMutationList(updatedSettings, WiserSettingTypes.Query, itemId);
                            break;
                        }
                        case "DELETE_QUERY":
                        {
                            AddSettingToMutationList(deletedSettings, WiserSettingTypes.Query, itemId);
                            break;
                        }
                        case "INSERT_ENTITY":
                        {
                            AddSettingToMutationList(createdSettings, WiserSettingTypes.Entity, itemId);
                            break;
                        }
                        case "UPDATE_ENTITY":
                        {
                            AddSettingToMutationList(updatedSettings, WiserSettingTypes.Entity, itemId);
                            break;
                        }
                        case "DELETE_ENTITY":
                        {
                            AddSettingToMutationList(deletedSettings, WiserSettingTypes.Entity, itemId);
                            break;
                        }
                        case "INSERT_FIELD_TEMPLATE":
                        {
                            AddSettingToMutationList(createdSettings, WiserSettingTypes.FieldTemplates, itemId);
                            break;
                        }
                        case "UPDATE_FIELD_TEMPLATE":
                        {
                            AddSettingToMutationList(updatedSettings, WiserSettingTypes.FieldTemplates, itemId);
                            break;
                        }
                        case "DELETE_FIELD_TEMPLATE":
                        {
                            AddSettingToMutationList(deletedSettings, WiserSettingTypes.FieldTemplates, itemId);
                            break;
                        }
                        case "INSERT_LINK_SETTING":
                        {
                            AddSettingToMutationList(createdSettings, WiserSettingTypes.Link, itemId);
                            break;
                        }
                        case "UPDATE_LINK_SETTING":
                        {
                            AddSettingToMutationList(updatedSettings, WiserSettingTypes.Link, itemId);
                            break;
                        }
                        case "DELETE_LINK_SETTING":
                        {
                            AddSettingToMutationList(deletedSettings, WiserSettingTypes.Link, itemId);
                            break;
                        }
                        case "INSERT_PERMISSION":
                        {
                            AddSettingToMutationList(createdSettings, WiserSettingTypes.Permission, itemId);
                            break;
                        }
                        case "UPDATE_PERMISSION":
                        {
                            AddSettingToMutationList(updatedSettings, WiserSettingTypes.Permission, itemId);
                            break;
                        }
                        case "DELETE_PERMISSION":
                        {
                            AddSettingToMutationList(deletedSettings, WiserSettingTypes.Permission, itemId);
                            break;
                        }
                        case "INSERT_USER_ROLE":
                        {
                            AddSettingToMutationList(createdSettings, WiserSettingTypes.UserRole, itemId);
                            break;
                        }
                        case "UPDATE_USER_ROLE":
                        {
                            AddSettingToMutationList(updatedSettings, WiserSettingTypes.UserRole, itemId);
                            break;
                        }
                        case "DELETE_USER_ROLE":
                        {
                            AddSettingToMutationList(deletedSettings, WiserSettingTypes.UserRole, itemId);
                            break;
                        }
                        case "INSERT_API_CONNECTION":
                        {
                            AddSettingToMutationList(createdSettings, WiserSettingTypes.ApiConnection, itemId);
                            break;
                        }
                        case "UPDATE_API_CONNECTION":
                        {
                            AddSettingToMutationList(updatedSettings, WiserSettingTypes.ApiConnection, itemId);
                            break;
                        }
                        case "DELETE_API_CONNECTION":
                        {
                            AddSettingToMutationList(deletedSettings, WiserSettingTypes.ApiConnection, itemId);
                            break;
                        }
                        case "INSERT_DATA_SELECTOR":
                        {
                            AddSettingToMutationList(createdSettings, WiserSettingTypes.DataSelector, itemId);
                            break;
                        }
                        case "UPDATE_DATA_SELECTOR":
                        {
                            AddSettingToMutationList(updatedSettings, WiserSettingTypes.DataSelector, itemId);
                            break;
                        }
                        case "DELETE_DATA_SELECTOR":
                        {
                            AddSettingToMutationList(deletedSettings, WiserSettingTypes.DataSelector, itemId);
                            break;
                        }
                        case "CREATE_STYLED_OUTPUT":
                        {
                            AddSettingToMutationList(createdSettings, WiserSettingTypes.StyledOutput, itemId);
                            break;
                        }
                        case "UPDATE_STYLED_OUTPUT":
                        {
                            AddSettingToMutationList(updatedSettings, WiserSettingTypes.StyledOutput, itemId);
                            break;
                        }
                        case "DELETE_STYLED_OUTPUT":
                        {
                            AddSettingToMutationList(deletedSettings, WiserSettingTypes.StyledOutput, itemId);
                            break;
                        }

                        // Changes to items.
                        case "CREATE_ITEM":
                        {
                            var tablePrefix = BranchesHelpers.GetTablePrefix(tableName, itemId);
                            AddItemToMutationList(createdItems, tablePrefix.TablePrefix, itemId);
                            break;
                        }
                        case "UPDATE_ITEM":
                        {
                            var tablePrefix = BranchesHelpers.GetTablePrefix(tableName, itemId);
                            AddItemToMutationList(updatedItems, tablePrefix.TablePrefix, itemId);
                            break;
                        }
                        case "DELETE_ITEM":
                        {
                            // When deleting an item, the entity type will be saved in the column "field" of wiser_history, so we don't have to look it up.
                            var tablePrefix = BranchesHelpers.GetTablePrefix(tableName, itemId);
                            AddItemToMutationList(deletedItems, tablePrefix.TablePrefix, itemId, field);
                            break;
                        }
                        case "ADD_LINK":
                        {
                            var destinationItemId = itemId;
                            var sourceItemId = Convert.ToUInt64(newValue);
                            var split = field.Split(',');
                            var type = Int32.Parse(split[0]);
                            var linkData = await GetEntityTypesOfLinkAsync(sourceItemId, destinationItemId, type, branchConnection, allLinkTypeSettings, tablePrefixes);
                            if (linkData == null)
                            {
                                break;
                            }

                            if (!tablePrefixes.TryGetValue(linkData.Value.SourceType, out var sourceTablePrefix))
                            {
                                sourceTablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(linkData.Value.SourceType);
                                tablePrefixes.Add(linkData.Value.SourceType, sourceTablePrefix);
                            }

                            if (!tablePrefixes.TryGetValue(linkData.Value.DestinationType, out var destinationTablePrefix))
                            {
                                destinationTablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(linkData.Value.DestinationType);
                                tablePrefixes.Add(linkData.Value.DestinationType, destinationTablePrefix);
                            }

                            AddItemToMutationList(updatedItems, sourceTablePrefix, sourceItemId, linkData.Value.SourceType);
                            AddItemToMutationList(updatedItems, destinationTablePrefix, destinationItemId, linkData.Value.DestinationType);

                            break;
                        }
                        case "UPDATE_ITEMLINKDETAIL":
                        case "CHANGE_LINK":
                        {
                            // First get the source item ID and destination item ID of the link.
                            var linkData = await GetDataFromLinkAsync(itemId, BranchesHelpers.GetTablePrefix(tableName, 0).TablePrefix, branchConnection);
                            if (!linkData.HasValue)
                            {
                                break;
                            }

                            // Then get the entity types of those IDs.
                            var entityData = await GetEntityTypesOfLinkAsync(linkData.Value.SourceItemId, linkData.Value.DestinationItemId, linkData.Value.Type, branchConnection, allLinkTypeSettings, tablePrefixes);
                            if (!entityData.HasValue)
                            {
                                break;
                            }

                            if (!tablePrefixes.TryGetValue(entityData.Value.SourceType, out var sourceTablePrefix))
                            {
                                sourceTablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(entityData.Value.SourceType);
                                tablePrefixes.Add(entityData.Value.SourceType, sourceTablePrefix);
                            }

                            if (!tablePrefixes.TryGetValue(entityData.Value.DestinationType, out var destinationTablePrefix))
                            {
                                destinationTablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(entityData.Value.DestinationType);
                                tablePrefixes.Add(entityData.Value.DestinationType, destinationTablePrefix);
                            }

                            // And finally mark these items as updated.
                            AddItemToMutationList(updatedItems, sourceTablePrefix, linkData.Value.SourceItemId, entityData.Value.SourceType);
                            AddItemToMutationList(updatedItems, destinationTablePrefix, linkData.Value.DestinationItemId, entityData.Value.DestinationType);

                            break;
                        }
                        case "REMOVE_LINK":
                        {
                            var sourceItemId = UInt64.Parse(oldValue!);
                            var linkData = await GetEntityTypesOfLinkAsync(sourceItemId, itemId, Int32.Parse(field), branchConnection, allLinkTypeSettings, tablePrefixes);
                            if (linkData == null)
                            {
                                break;
                            }

                            if (!tablePrefixes.TryGetValue(linkData.Value.SourceType, out var sourceTablePrefix))
                            {
                                sourceTablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(linkData.Value.SourceType);
                                tablePrefixes.Add(linkData.Value.SourceType, sourceTablePrefix);
                            }

                            if (!tablePrefixes.TryGetValue(linkData.Value.DestinationType, out var destinationTablePrefix))
                            {
                                destinationTablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(linkData.Value.DestinationType);
                                tablePrefixes.Add(linkData.Value.DestinationType, destinationTablePrefix);
                            }

                            AddItemToMutationList(updatedItems, sourceTablePrefix, sourceItemId, linkData.Value.SourceType);
                            AddItemToMutationList(updatedItems, destinationTablePrefix, itemId, linkData.Value.DestinationType);

                            break;
                        }
                        case "ADD_FILE" when oldValue == "item_id":
                        case "DELETE_FILE" when oldValue == "item_id":
                        {
                            var itemIdFromFile = UInt64.Parse(newValue!);
                            var tablePrefix = BranchesHelpers.GetTablePrefix(tableName, itemIdFromFile);
                            AddItemToMutationList(updatedItems, tablePrefix.TablePrefix, itemIdFromFile);

                            break;
                        }
                        case "ADD_FILE" when oldValue == "itemlink_id":
                        case "DELETE_FILE" when oldValue == "itemlink_id":
                        {
                            // First get the source item ID and destination item ID of the link.
                            var linkIdFromFile = UInt64.Parse(newValue!);
                            var linkData = await GetDataFromLinkAsync(linkIdFromFile, BranchesHelpers.GetTablePrefix(tableName, 0).TablePrefix, branchConnection);
                            if (!linkData.HasValue)
                            {
                                break;
                            }

                            // Then get the entity types of those IDs.
                            var entityData = await GetEntityTypesOfLinkAsync(linkData.Value.SourceItemId, linkData.Value.DestinationItemId, linkData.Value.Type, branchConnection, allLinkTypeSettings, tablePrefixes);
                            if (!entityData.HasValue)
                            {
                                break;
                            }

                            if (!tablePrefixes.TryGetValue(entityData.Value.SourceType, out var sourceTablePrefix))
                            {
                                sourceTablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(entityData.Value.SourceType);
                                tablePrefixes.Add(entityData.Value.SourceType, sourceTablePrefix);
                            }

                            if (!tablePrefixes.TryGetValue(entityData.Value.DestinationType, out var destinationTablePrefix))
                            {
                                destinationTablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(entityData.Value.DestinationType);
                                tablePrefixes.Add(entityData.Value.DestinationType, destinationTablePrefix);
                            }

                            // And finally mark these items as updated.
                            AddItemToMutationList(updatedItems, sourceTablePrefix, linkData.Value.SourceItemId, entityData.Value.SourceType);
                            AddItemToMutationList(updatedItems, destinationTablePrefix, linkData.Value.DestinationItemId, entityData.Value.DestinationType);

                            break;
                        }
                        case "UPDATE_FILE":
                        {
                            ulong itemIdFromFile = 0;
                            ulong linkIdFromFile = 0;
                            if (!filesData.TryGetValue(tableName, out var fileDictionary) || !fileDictionary.TryGetValue(itemId, out var fileData))
                            {
                                var fileDataTable = new DataTable();
                                await using var linkCommand = branchConnection.CreateCommand();
                                linkCommand.Parameters.AddWithValue("id", itemId);
                                linkCommand.CommandText = $@"SELECT item_id, itemlink_id FROM `{tableName}` WHERE id = ?id
UNION ALL
SELECT item_id, itemlink_id FROM `{tableName}{WiserTableNames.ArchiveSuffix}` WHERE id = ?id
LIMIT 1";
                                using var linkAdapter = new MySqlDataAdapter(linkCommand);
                                linkAdapter.Fill(fileDataTable);

                                if (fileDataTable.Rows.Count == 0)
                                {
                                    break;
                                }

                                itemIdFromFile = Convert.ToUInt64(fileDataTable.Rows[0]["item_id"]);
                                linkIdFromFile = Convert.ToUInt64(fileDataTable.Rows[0]["itemlink_id"]);
                            }
                            else
                            {
                                itemIdFromFile = fileData.itemId;
                                linkIdFromFile = fileData.itemLinkId;
                            }

                            if (itemIdFromFile > 0)
                            {
                                var tablePrefix = BranchesHelpers.GetTablePrefix(tableName, itemIdFromFile);
                                AddItemToMutationList(updatedItems, tablePrefix.TablePrefix, itemIdFromFile);
                                break;
                            }

                            // First get the source item ID and destination item ID of the link.
                            var linkData = await GetDataFromLinkAsync(linkIdFromFile, BranchesHelpers.GetTablePrefix(tableName, 0).TablePrefix, branchConnection);
                            if (!linkData.HasValue)
                            {
                                break;
                            }

                            // Then get the entity types of those IDs.
                            var entityData = await GetEntityTypesOfLinkAsync(linkData.Value.SourceItemId, linkData.Value.DestinationItemId, linkData.Value.Type, branchConnection, allLinkTypeSettings, tablePrefixes);
                            if (!entityData.HasValue)
                            {
                                break;
                            }

                            if (!tablePrefixes.TryGetValue(entityData.Value.SourceType, out var sourceTablePrefix))
                            {
                                sourceTablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(entityData.Value.SourceType);
                                tablePrefixes.Add(entityData.Value.SourceType, sourceTablePrefix);
                            }

                            if (!tablePrefixes.TryGetValue(entityData.Value.DestinationType, out var destinationTablePrefix))
                            {
                                destinationTablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(entityData.Value.DestinationType);
                                tablePrefixes.Add(entityData.Value.DestinationType, destinationTablePrefix);
                            }

                            // And finally mark these items as updated.
                            AddItemToMutationList(updatedItems, sourceTablePrefix, linkData.Value.SourceItemId, entityData.Value.SourceType);
                            AddItemToMutationList(updatedItems, destinationTablePrefix, linkData.Value.DestinationItemId, entityData.Value.DestinationType);

                            break;
                        }
                    }
                }

                if (times.ContainsKey(action))
                {
                    var (count, totalTime) = times[action];
                    times[action] = (count + 1, totalTime + actionStopwatch.Elapsed);
                }
                else
                {
                    times.Add(action, (1, actionStopwatch.Elapsed));
                }

                actionStopwatch.Reset();
            }

            // Add the counters to the results.
            actionStopwatch.Start();
            foreach (var item in createdItems)
            {
                var entityType = item.EntityType;
                if (String.IsNullOrWhiteSpace(entityType))
                {
                    entityType = await GetEntityTypeFromIdAsync(item.ItemId, item.TablePrefix, branchConnection);
                }

                (await GetOrAddEntityTypeCounterAsync(entityType)).Created++;
            }

            times.Add("createdItemsCounters", (1, actionStopwatch.Elapsed));

            actionStopwatch.Restart();
            foreach (var item in updatedItems)
            {
                var entityType = item.EntityType;
                if (String.IsNullOrWhiteSpace(entityType))
                {
                    entityType = await GetEntityTypeFromIdAsync(item.ItemId, item.TablePrefix, branchConnection);
                }

                (await GetOrAddEntityTypeCounterAsync(entityType)).Updated++;
            }

            times.Add("updatedItemsCounters", (1, actionStopwatch.Elapsed));

            actionStopwatch.Restart();
            foreach (var item in deletedItems)
            {
                var entityType = item.EntityType;
                if (String.IsNullOrWhiteSpace(entityType))
                {
                    entityType = await GetEntityTypeFromIdAsync(item.ItemId, item.TablePrefix, branchConnection);
                }

                (await GetOrAddEntityTypeCounterAsync(entityType)).Deleted++;
            }

            times.Add("deletedItemsCounters", (1, actionStopwatch.Elapsed));

            actionStopwatch.Restart();
            foreach (var setting in createdSettings)
            {
                GetOrAddWiserSettingCounter(setting.Key).Created = setting.Value.Count;
            }

            times.Add("createdSettingsCounters", (1, actionStopwatch.Elapsed));

            actionStopwatch.Restart();
            foreach (var setting in updatedSettings)
            {
                GetOrAddWiserSettingCounter(setting.Key).Updated = setting.Value.Count;
            }

            times.Add("updatedSettingsCounters", (1, actionStopwatch.Elapsed));

            actionStopwatch.Restart();
            foreach (var setting in deletedSettings)
            {
                GetOrAddWiserSettingCounter(setting.Key).Deleted = setting.Value.Count;
            }

            times.Add("deletedSettingsCounters", (1, actionStopwatch.Elapsed));
            actionStopwatch.Stop();
            mainStopwatch.Stop();

            logger.LogDebug($"Finished GetChangesAsync in {mainStopwatch.ElapsedMilliseconds}ms. Times: {JsonConvert.SerializeObject(times)}");

            return new ServiceResult<ChangesAvailableForMergingModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<MergeBranchResultModel>> MergeAsync(ClaimsIdentity identity, MergeBranchSettingsModel settings)
        {
            var result = new MergeBranchResultModel();
            var currentTenant = (await wiserTenantsService.GetSingleAsync(identity, true)).ModelObject;
            var productionTenant = (await wiserTenantsService.GetSingleAsync(currentTenant.TenantId, true)).ModelObject;

            // If the settings.Id is 0, it means the user wants to merge the current branch.
            if (settings.Id <= 0)
            {
                settings.Id = currentTenant.Id;
                settings.DatabaseName = currentTenant.Database.DatabaseName;
            }

            // Make sure the user is not trying to copy changes from main to main, that would be weird and also cause a lot of problems.
            if (currentTenant.TenantId == settings.Id)
            {
                return new ServiceResult<MergeBranchResultModel>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "U probeert wijzigingen van de hoofdbranch te synchroniseren, dat is niet mogelijk."
                };
            }

            var selectedBranchTenant = settings.Id == currentTenant.Id
                ? currentTenant
                : (await wiserTenantsService.GetSingleAsync(settings.Id, true)).ModelObject;

            // Check to make sure someone is not trying to copy changes from an environment that does not belong to them.
            if (selectedBranchTenant == null || currentTenant.TenantId != selectedBranchTenant.TenantId)
            {
                return new ServiceResult<MergeBranchResultModel>
                {
                    StatusCode = HttpStatusCode.Forbidden
                };
            }

            // Save the database settings for the WTS.
            settings.DatabaseName = selectedBranchTenant.Database.DatabaseName;
            settings.DatabaseHost = selectedBranchTenant.Database.Host.EncryptWithAesWithSalt(productionTenant.EncryptionKey, useSlowerButMoreSecureMethod: true);
            settings.DatabasePort = selectedBranchTenant.Database.PortNumber;
            settings.DatabaseUsername = selectedBranchTenant.Database.Username.EncryptWithAesWithSalt(productionTenant.EncryptionKey, useSlowerButMoreSecureMethod: true);
            settings.DatabasePassword = selectedBranchTenant.Database.Password.DecryptWithAesWithSalt(apiSettings.DatabasePasswordEncryptionKey).EncryptWithAesWithSalt(productionTenant.EncryptionKey, useSlowerButMoreSecureMethod: true);

            DateTime? lastMergeDate = null;

            await using var mainConnection = new MySqlConnection(wiserTenantsService.GenerateConnectionStringFromTenant(productionTenant));
            await mainConnection.OpenAsync();

            if (settings.CheckForConflicts)
            {
                // Get the date and time of the last merge of this branch, so we can find all changes made in production after this date, to check for merge conflicts.
                await using (var productionCommand = mainConnection.CreateCommand())
                {
                    productionCommand.Parameters.AddWithValue("branchId", selectedBranchTenant.Id);
                    productionCommand.CommandText = $"SELECT MAX(finished_on) AS lastMergeDate FROM {WiserTableNames.WiserBranchesQueue} WHERE branch_id = ?branchId AND success = 1 AND finished_on IS NOT NULL";

                    var dataTable = new DataTable();
                    using var sourceAdapter = new MySqlDataAdapter(productionCommand);
                    sourceAdapter.Fill(dataTable);
                    if (dataTable.Rows.Count > 0)
                    {
                        lastMergeDate = dataTable.Rows[0].Field<DateTime?>("lastMergeDate");
                    }
                }

                await using var branchConnection = new MySqlConnection(wiserTenantsService.GenerateConnectionStringFromTenant(selectedBranchTenant));
                await branchConnection.OpenAsync();

                // If we have no last merge date, it probably means someone removed a record from wiser_branch_queue, in that case get the date of the first change in wiser_history in the branch.
                if (!lastMergeDate.HasValue)
                {
                    await using var branchCommand = branchConnection.CreateCommand();
                    branchCommand.CommandText = $"SELECT MIN(changed_on) AS firstChangeDate FROM {WiserTableNames.WiserHistory}";
                    var dataTable = new DataTable();
                    using var branchAdapter = new MySqlDataAdapter(branchCommand);
                    branchAdapter.Fill(dataTable);
                    if (dataTable.Rows.Count > 0)
                    {
                        lastMergeDate = dataTable.Rows[0].Field<DateTime?>("firstChangeDate");
                    }
                }

                // If we somehow still don't have a last merge date, then we can't check for merge conflicts. This should never happen under normal circumstances.
                if (lastMergeDate.HasValue && (settings.ConflictSettings == null || !settings.ConflictSettings.Any()))
                {
                    var conflicts = new List<MergeConflictModel>();
                    await GetAllChangesFromBranchAsync(branchConnection, conflicts, settings);
                    await FindConflictsInMainBranchAsync(mainConnection, branchConnection, conflicts, lastMergeDate.Value, settings);
                    result.Conflicts = conflicts.Where(conflict => conflict.ChangeDateInMain.HasValue).ToList();
                    if (result.Conflicts.Any())
                    {
                        result.Success = false;
                        return new ServiceResult<MergeBranchResultModel>(result);
                    }
                }
            }

            // Add the merge to the queue so that the WTS will process it.
            await using (var productionCommand = mainConnection.CreateCommand())
            {
                productionCommand.Parameters.AddWithValue("branch_id", settings.Id);
                productionCommand.Parameters.AddWithValue("name", selectedBranchTenant.Name);
                productionCommand.Parameters.AddWithValue("action", "merge");
                productionCommand.Parameters.AddWithValue("data", JsonConvert.SerializeObject(settings));
                productionCommand.Parameters.AddWithValue("added_on", DateTime.Now);
                productionCommand.Parameters.AddWithValue("start_on", settings.StartOn ?? DateTime.Now);
                productionCommand.Parameters.AddWithValue("added_by", IdentityHelpers.GetUserName(identity, true));
                productionCommand.Parameters.AddWithValue("user_id", IdentityHelpers.GetWiserUserId(identity));
                productionCommand.CommandText = $@"INSERT INTO {WiserTableNames.WiserBranchesQueue} 
(branch_id, action, data, added_on, start_on, added_by, user_id)
VALUES (?branch_id, ?action, ?data, ?added_on, ?start_on, ?added_by, ?user_id)";

                await productionCommand.ExecuteNonQueryAsync();
            }

            result.Success = true;

            return new ServiceResult<MergeBranchResultModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> CanAccessBranchAsync(ClaimsIdentity identity, int branchId)
        {
            var currentBranch = (await wiserTenantsService.GetSingleAsync(identity, true)).ModelObject;
            var otherBranch = (await wiserTenantsService.GetSingleAsync(branchId, true)).ModelObject;

            return new ServiceResult<bool>(currentBranch.TenantId == otherBranch.TenantId);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> CanAccessBranchAsync(ClaimsIdentity identity, TenantModel branch)
        {
            var currentBranch = (await wiserTenantsService.GetSingleAsync(identity, true)).ModelObject;

            return new ServiceResult<bool>(currentBranch.TenantId == branch.TenantId);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> DeleteAsync(ClaimsIdentity identity, int id)
        {
            var currentTenant = (await wiserTenantsService.GetSingleAsync(identity, true)).ModelObject;
            var productionTenant = (await wiserTenantsService.GetSingleAsync(currentTenant.TenantId, true)).ModelObject;
            var branchData = await wiserTenantsService.GetSingleAsync(id, true);

            // Check if the branch exists or if there were any other errors retrieving the branch.
            if (branchData.StatusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<bool>(false)
                {
                    StatusCode = branchData.StatusCode,
                    ErrorMessage = branchData.ErrorMessage
                };
            }

            // Make sure the user is not trying to delete the main branch somehow, that is not allowed.
            if (productionTenant.TenantId == id)
            {
                return new ServiceResult<bool>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "U probeert de hoofdbranch te verwijderen, dat is niet mogelijk."
                };
            }

            // Check to make sure someone is not trying to delete an environment that does not belong to them.
            if (branchData.ModelObject.TenantId != productionTenant.TenantId)
            {
                return new ServiceResult<bool>(false)
                {
                    StatusCode = HttpStatusCode.Forbidden
                };
            }

            var settings = new BranchActionBaseModel
            {
                Id = id,
                DatabaseName = branchData.ModelObject.Database.DatabaseName
            };

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.AddParameter("id", id);
            clientDatabaseConnection.AddParameter("name", branchData.ModelObject.Name);
            clientDatabaseConnection.AddParameter("now", DateTime.Now);
            clientDatabaseConnection.AddParameter("username", IdentityHelpers.GetUserName(identity, true));
            clientDatabaseConnection.AddParameter("userid", IdentityHelpers.GetWiserUserId(identity));
            clientDatabaseConnection.AddParameter("data", JsonConvert.SerializeObject(settings));
            var query = $@"INSERT INTO {WiserTableNames.WiserBranchesQueue} (branch_id, name, action, added_on, added_by, user_id, start_on, data)
VALUES (?id, ?name, 'delete', ?now, ?username, ?userId, ?now, ?data)";
            await clientDatabaseConnection.ExecuteAsync(query);

            // Delete the row from easy_customers, so that the WTS doesn't need to access the main Wiser database.
            query = $@"DELETE FROM {ApiTableNames.WiserTenants} WHERE id = ?id";
            wiserDatabaseConnection.AddParameter("id", id);
            await wiserDatabaseConnection.ExecuteAsync(query);

            return new ServiceResult<bool>(true)
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }

        /// <summary>
        /// This method is for finding merge conflicts. This will get all changes from the branch database and add them to the conflicts list.
        /// This is meant to work together with FindConflictsInMainBranchAsync. which should be called right after this method..
        /// </summary>
        /// <param name="branchConnection">The connection to the branch database.</param>
        /// <param name="conflicts">The list of conflicts.</param>
        /// <param name="mergeBranchSettings">The settings that say what things to merge.</param>
        private static async Task GetAllChangesFromBranchAsync(MySqlConnection branchConnection, List<MergeConflictModel> conflicts, MergeBranchSettingsModel mergeBranchSettings)
        {
            // Get all changes from branch.
            var dataTable = new DataTable();
            await using (var branchCommand = branchConnection.CreateCommand())
            {
                branchCommand.CommandText = $@"SELECT
    id, 
    action,
    tablename,
    item_id,
    changed_on,
    changed_by,
    field,
    newvalue,
    language_code,
    groupname
FROM {WiserTableNames.WiserHistory}";
                using var branchAdapter = new MySqlDataAdapter(branchCommand);
                branchAdapter.Fill(dataTable);
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                var action = dataRow.Field<string>("action")?.ToUpperInvariant();
                var conflict = new MergeConflictModel
                {
                    Id = Convert.ToUInt64(dataRow["id"]),
                    ObjectId = dataRow.Field<ulong>("item_id"),
                    TableName = dataRow.Field<string>("tablename"),
                    FieldName = dataRow.Field<string>("field"),
                    ValueInBranch = dataRow.Field<string>("newvalue"),
                    ChangeDateInBranch = dataRow.Field<DateTime>("changed_on"),
                    ChangedByInBranch = dataRow.Field<string>("changed_by"),
                    LanguageCode = dataRow.Field<string>("language_code"),
                    GroupName = dataRow.Field<string>("groupname")
                };

                switch (action)
                {
                    case "UPDATE_ENTITYPROPERTY":
                    {
                        // No need to check for conflicts if the user doesn't want to synchronise changes of this type.
                        if (!mergeBranchSettings.Settings.Any(x => x.Type == WiserSettingTypes.EntityProperty && x.Update))
                        {
                            continue;
                        }

                        conflict.Type = "entityProperty";
                        conflict.TypeDisplayName = "Veld van entiteit";
                        conflict.Title = $"#{conflict.ObjectId}";
                        conflict.FieldDisplayName = conflict.FieldName;
                        break;
                    }
                    case "UPDATE_MODULE":
                    {
                        // No need to check for conflicts if the user doesn't want to synchronise changes of this type.
                        if (!mergeBranchSettings.Settings.Any(x => x.Type == WiserSettingTypes.Module && x.Update))
                        {
                            continue;
                        }

                        conflict.Type = "module";
                        conflict.TypeDisplayName = "Module";
                        conflict.Title = $"#{conflict.ObjectId}";
                        conflict.FieldDisplayName = conflict.FieldName;
                        break;
                    }
                    case "UPDATE_QUERY":
                    {
                        // No need to check for conflicts if the user doesn't want to synchronise changes of this type.
                        if (!mergeBranchSettings.Settings.Any(x => x.Type == WiserSettingTypes.Query && x.Update))
                        {
                            continue;
                        }

                        conflict.Type = "query";
                        conflict.TypeDisplayName = "Query";
                        conflict.Title = $"#{conflict.ObjectId}";
                        conflict.FieldDisplayName = conflict.FieldName;
                        break;
                    }
                    case "UPDATE_ENTITY":
                    {
                        // No need to check for conflicts if the user doesn't want to synchronise changes of this type.
                        if (!mergeBranchSettings.Settings.Any(x => x.Type == WiserSettingTypes.Entity && x.Update))
                        {
                            continue;
                        }

                        conflict.Type = "entity";
                        conflict.TypeDisplayName = "Entiteit";
                        conflict.Title = $"#{conflict.ObjectId}";
                        conflict.FieldDisplayName = conflict.FieldName;
                        break;
                    }
                    case "UPDATE_FIELD_TEMPLATE":
                    {
                        // No need to check for conflicts if the user doesn't want to synchronise changes of this type.
                        if (!mergeBranchSettings.Settings.Any(x => x.Type == WiserSettingTypes.FieldTemplates && x.Update))
                        {
                            continue;
                        }

                        conflict.Type = "fieldTemplate";
                        conflict.TypeDisplayName = "Veld-template";
                        conflict.Title = $"#{conflict.ObjectId}";
                        conflict.FieldDisplayName = conflict.FieldName;
                        break;
                    }
                    case "UPDATE_LINK_SETTING":
                    {
                        // No need to check for conflicts if the user doesn't want to synchronise changes of this type.
                        if (!mergeBranchSettings.Settings.Any(x => x.Type == WiserSettingTypes.Link && x.Update))
                        {
                            continue;
                        }

                        conflict.Type = "linkType";
                        conflict.TypeDisplayName = "Koppeltype";
                        conflict.Title = $"#{conflict.ObjectId}";
                        conflict.FieldDisplayName = conflict.FieldName;
                        break;
                    }
                    case "UPDATE_PERMISSION":
                    {
                        // No need to check for conflicts if the user doesn't want to synchronise changes of this type.
                        if (!mergeBranchSettings.Settings.Any(x => x.Type == WiserSettingTypes.Permission && x.Update))
                        {
                            continue;
                        }

                        conflict.Type = "permission";
                        conflict.TypeDisplayName = "Rechten";
                        conflict.Title = $"#{conflict.ObjectId}";
                        conflict.FieldDisplayName = conflict.FieldName;
                        break;
                    }
                    case "UPDATE_USER_ROLE":
                    {
                        // No need to check for conflicts if the user doesn't want to synchronise changes of this type.
                        if (!mergeBranchSettings.Settings.Any(x => x.Type == WiserSettingTypes.UserRole && x.Update))
                        {
                            continue;
                        }

                        conflict.Type = "userRole";
                        conflict.TypeDisplayName = "Koppeling van rol aan gebruiker";
                        conflict.Title = $"#{conflict.ObjectId}";
                        conflict.FieldDisplayName = conflict.FieldName;
                        break;
                    }
                    case "UPDATE_API_CONNECTION":
                    {
                        // No need to check for conflicts if the user doesn't want to synchronise changes of this type.
                        if (!mergeBranchSettings.Settings.Any(x => x.Type == WiserSettingTypes.ApiConnection && x.Update))
                        {
                            continue;
                        }

                        conflict.Type = "apiConnection";
                        conflict.TypeDisplayName = "Api-instellingen";
                        conflict.Title = $"#{conflict.ObjectId}";
                        conflict.FieldDisplayName = conflict.FieldName;
                        break;
                    }
                    case "UPDATE_DATA_SELECTOR":
                    {
                        // No need to check for conflicts if the user doesn't want to synchronise changes of this type.
                        if (!mergeBranchSettings.Settings.Any(x => x.Type == WiserSettingTypes.DataSelector && x.Update))
                        {
                            continue;
                        }

                        conflict.Type = "dataSelector";
                        conflict.TypeDisplayName = "Dataselector";
                        conflict.Title = $"#{conflict.ObjectId}";
                        conflict.FieldDisplayName = conflict.FieldName;
                        break;
                    }
                    case "UPDATE_STYLED_OUTPUT":
                    {
                        // No need to check for conflicts if the user doesn't want to synchronise changes of this type.
                        if (!mergeBranchSettings.Settings.Any(x => x.Type == WiserSettingTypes.StyledOutput && x.Update))
                        {
                            continue;
                        }

                        conflict.Type = "styledOutput";
                        conflict.TypeDisplayName = "Styled output";
                        conflict.Title = $"#{conflict.ObjectId}";
                        conflict.FieldDisplayName = conflict.FieldName;
                        break;
                    }

                    // Changes to items. We don't check the mergeBranchSettings here, because we don't know the entity types of items here yet.
                    // The mergeBranchSettings for items will be checked in FindConflictsInMainBranchAsync.
                    case "UPDATE_ITEM":
                    {
                        conflict.Type = "item";
                        conflict.TypeDisplayName = "Item";
                        conflict.Title = $"#{conflict.ObjectId}";
                        conflict.FieldDisplayName = conflict.FieldName;
                        break;
                    }
                    case "UPDATE_ITEMLINKDETAIL":
                    case "CHANGE_LINK":
                    {
                        conflict.Type = "link";
                        conflict.TypeDisplayName = "Koppeling";
                        conflict.Title = $"#{conflict.ObjectId}";
                        conflict.FieldDisplayName = conflict.FieldName;
                        break;
                    }
                    case "UPDATE_FILE":
                    {
                        conflict.Type = "file";
                        conflict.TypeDisplayName = "Bestand";
                        conflict.Title = $"#{conflict.ObjectId}";
                        conflict.FieldDisplayName = conflict.FieldName;
                        break;
                    }
                    default:
                    {
                        continue;
                    }
                }

                conflicts.Add(conflict);
            }
        }

        /// <summary>
        /// This method is for finding merge conflicts. This will get all changes from the main database. It will then check if the same items has been changed in the branch database and if so, add that as a conflict.
        /// This is meant to work together with GetAllChangesFromBranchAsync. which should be called right before this method.
        /// </summary>
        /// <param name="mainConnection">The connection to the main database.</param>
        /// <param name="branchConnection">The connection to the branch database.</param>
        /// <param name="conflicts">The list of conflicts.</param>
        /// <param name="lastMergeDate">The date and time of the last merge, so we know from when to start looking.</param>
        /// <param name="mergeBranchSettings">The settings that say what things to merge.</param>
        private async Task FindConflictsInMainBranchAsync(MySqlConnection mainConnection, MySqlConnection branchConnection, List<MergeConflictModel> conflicts, DateTime lastMergeDate, MergeBranchSettingsModel mergeBranchSettings)
        {
            var moduleNames = new Dictionary<ulong, string>();
            var entityTypes = new Dictionary<ulong, string>();
            var items = new Dictionary<ulong, (string Title, string EntityType, int ModuleId)>();
            var links = new Dictionary<ulong, int>();
            var entityTypeSettings = new Dictionary<string, EntitySettingsModel>();
            var linkTypeSettings = new Dictionary<int, LinkSettingsModel>();
            var entityProperties = new Dictionary<ulong, string>();
            var queryNames = new Dictionary<ulong, string>();
            var fieldTypes = new Dictionary<ulong, string>();
            var linkSettings = new Dictionary<ulong, string>();
            var apiConnections = new Dictionary<ulong, string>();
            var dataSelectors = new Dictionary<ulong, string>();
            var fieldDisplayNames = new Dictionary<string, string>();
            var dataTable = new DataTable();

            await using var productionCommand = mainConnection.CreateCommand();
            productionCommand.CommandTimeout = apiSettings.SqlCommandTimeoutForExportsAndLongQueries;
            productionCommand.Parameters.AddWithValue("lastChange", lastMergeDate);
            productionCommand.CommandText = $@"SELECT 
    action,
    tablename,
    item_id,
    changed_on,
    changed_by,
    field,
    newvalue,
    language_code,
    groupname
FROM {WiserTableNames.WiserHistory}
WHERE changed_on >= ?lastChange";
            using (var branchAdapter = new MySqlDataAdapter(productionCommand))
            {
                branchAdapter.Fill(dataTable);
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                var action = dataRow.Field<string>("action")?.ToUpperInvariant();
                var value = dataRow.Field<string>("newvalue");

                var conflict = conflicts.LastOrDefault(conflict => conflict.ObjectId == dataRow.Field<ulong>("item_id")
                                                                   && conflict.TableName == dataRow.Field<string>("tablename")
                                                                   && conflict.LanguageCode == dataRow.Field<string>("language_code")
                                                                   && conflict.GroupName == dataRow.Field<string>("groupname")
                                                                   && conflict.FieldName == dataRow.Field<string>("field")
                                                                   && conflict.ValueInBranch != value);

                // If we can't find a conflict in the list, it means that the chosen branch has no change for this item/object, so we can skip it.
                if (conflict == null)
                {
                    continue;
                }

                // Local function for getting the display name of an object, this uses a dictionary to cache the names in memory.
                async Task<string> GetDisplayNameAsync(IDictionary<ulong, string> cache, string nameColumn = "name")
                {
                    if (cache.ContainsKey(conflict!.ObjectId))
                    {
                        return cache[conflict.ObjectId];
                    }

                    await using var branchCommand = branchConnection.CreateCommand();
                    branchCommand.Parameters.AddWithValue("id", conflict.ObjectId);
                    branchCommand.CommandText = $"SELECT {nameColumn} FROM {conflict.TableName} WHERE id = ?id";
                    var moduleDataTable = new DataTable();
                    using var adapter = new MySqlDataAdapter(branchCommand);
                    adapter.Fill(moduleDataTable);
                    cache.Add(conflict.ObjectId, moduleDataTable.Rows.Count == 0 ? $"Onbekend, #{conflict.ObjectId}" : moduleDataTable.Rows[0].Field<string>(nameColumn));
                    return cache[conflict.ObjectId];
                }

                switch (action)
                {
                    // Changes to Wiser settings.
                    case "UPDATE_ENTITYPROPERTY":
                    {
                        if (!entityProperties.ContainsKey(conflict.ObjectId))
                        {
                            await using var branchCommand = branchConnection.CreateCommand();
                            branchCommand.Parameters.AddWithValue("id", conflict.ObjectId);
                            branchCommand.CommandText = $"SELECT entity_name, display_name, language_code FROM {WiserTableNames.WiserEntityProperty} WHERE id = ?id";
                            var entityPropertyDataTable = new DataTable();
                            using var adapter = new MySqlDataAdapter(branchCommand);
                            adapter.Fill(entityPropertyDataTable);

                            var name = new StringBuilder($"Onbekend, #{conflict.ObjectId}");
                            if (entityPropertyDataTable.Rows.Count > 0)
                            {
                                name = new StringBuilder(entityPropertyDataTable.Rows[0].Field<string>("display_name"));
                                var languageCode = entityPropertyDataTable.Rows[0].Field<string>("language_code");
                                if (!String.IsNullOrWhiteSpace(languageCode))
                                {
                                    name.Append($" ({languageCode})");
                                }

                                name.Append($" van {entityPropertyDataTable.Rows[0].Field<string>("entity_name")}");
                            }

                            entityProperties.Add(conflict.ObjectId, name.ToString());
                        }

                        conflict.Title = entityProperties[conflict.ObjectId];
                        break;
                    }
                    case "UPDATE_MODULE":
                    {
                        conflict.Title = await GetDisplayNameAsync(moduleNames);
                        break;
                    }
                    case "UPDATE_QUERY":
                    {
                        conflict.Title = await GetDisplayNameAsync(queryNames, "description");
                        break;
                    }
                    case "UPDATE_ENTITY":
                    {
                        if (!entityTypes.ContainsKey(conflict.ObjectId))
                        {
                            await using var branchCommand = branchConnection.CreateCommand();
                            branchCommand.Parameters.AddWithValue("id", conflict.ObjectId);
                            branchCommand.CommandText = $"SELECT name, friendly_name FROM {WiserTableNames.WiserEntity} WHERE id = ?id";
                            var entityDataTable = new DataTable();
                            using var adapter = new MySqlDataAdapter(branchCommand);
                            adapter.Fill(entityDataTable);

                            var name = $"Onbekend, #{conflict.ObjectId}";
                            if (entityDataTable.Rows.Count > 0)
                            {
                                name = entityDataTable.Rows[0].Field<string>("friendly_name");
                                if (String.IsNullOrWhiteSpace(name))
                                {
                                    name = entityDataTable.Rows[0].Field<string>("name");
                                }
                            }

                            entityTypes.Add(conflict.ObjectId, name);
                        }

                        conflict.Title = entityTypes[conflict.ObjectId];
                        break;
                    }
                    case "UPDATE_FIELD_TEMPLATE":
                    {
                        conflict.Title = await GetDisplayNameAsync(fieldTypes, "field_type");
                        break;
                    }
                    case "UPDATE_LINK_SETTING":
                    {
                        conflict.Title = await GetDisplayNameAsync(linkSettings);
                        break;
                    }
                    case "UPDATE_PERMISSION":
                    {
                        break;
                    }
                    case "UPDATE_USER_ROLE":
                    {
                        break;
                    }
                    case "UPDATE_API_CONNECTION":
                    {
                        conflict.Title = await GetDisplayNameAsync(apiConnections);
                        break;
                    }
                    case "UPDATE_DATA_SELECTOR":
                    {
                        conflict.Title = await GetDisplayNameAsync(dataSelectors);
                        break;
                    }

                    // Changes to items.
                    case "UPDATE_ITEM":
                    {
                        // Get the title and entity type of the item.
                        if (!items.ContainsKey(conflict.ObjectId))
                        {
                            await using var branchCommand = branchConnection.CreateCommand();
                            branchCommand.Parameters.AddWithValue("id", conflict.ObjectId);
                            branchCommand.CommandText = $"SELECT title, entity_type, moduleid FROM {conflict.TableName.Replace(WiserTableNames.WiserItemDetail, WiserTableNames.WiserItem)} WHERE id = ?id";
                            var entityDataTable = new DataTable();
                            using var adapter = new MySqlDataAdapter(branchCommand);
                            adapter.Fill(entityDataTable);

                            var entityType = "unknown";
                            var title = $"Onbekend, #{conflict.ObjectId}";
                            var moduleId = 0;
                            if (entityDataTable.Rows.Count > 0)
                            {
                                title = entityDataTable.Rows[0].Field<string>("title");
                                entityType = entityDataTable.Rows[0].Field<string>("entity_type");
                                moduleId = entityDataTable.Rows[0].Field<int>("moduleid");
                            }

                            items.Add(conflict.ObjectId, (title, entityType, moduleId));
                        }

                        conflict.Title = items[conflict.ObjectId].Title;
                        conflict.Type = items[conflict.ObjectId].EntityType;

                        // No need to check for conflicts if the user doesn't want to synchronise changes of this type.
                        if (!mergeBranchSettings.Entities.Any(x => String.Equals(x.Type, conflict.Type, StringComparison.OrdinalIgnoreCase) && x.Update))
                        {
                            continue;
                        }

                        // Get the display name for the entity type.
                        if (!entityTypeSettings.ContainsKey(items[conflict.ObjectId].EntityType))
                        {
                            entityTypeSettings.Add(items[conflict.ObjectId].EntityType, await wiserItemsService.GetEntityTypeSettingsAsync(items[conflict.ObjectId].EntityType));
                        }

                        conflict.TypeDisplayName = entityTypeSettings[items[conflict.ObjectId].EntityType].DisplayName;

                        // Get the display name for the field.
                        var languageCode = dataRow.Field<string>("language_code");
                        var fieldName = dataRow.Field<string>("field");
                        var fieldKey = $"{conflict.Type}_{fieldName}_{languageCode}";
                        if (!fieldDisplayNames.ContainsKey(fieldKey))
                        {
                            await using var branchCommand = branchConnection.CreateCommand();
                            branchCommand.Parameters.AddWithValue("fieldName", fieldName);
                            branchCommand.Parameters.AddWithValue("languageCode", languageCode);
                            branchCommand.Parameters.AddWithValue("entityType", conflict.Type);
                            branchCommand.CommandText = $"SELECT display_name FROM {WiserTableNames.WiserEntityProperty} WHERE entity_name = ?entityType AND property_name = ?fieldName AND language_code = ?languageCode";
                            var entityDataTable = new DataTable();
                            using var adapter = new MySqlDataAdapter(branchCommand);
                            adapter.Fill(entityDataTable);

                            var displayName = "";
                            if (entityDataTable.Rows.Count > 0)
                            {
                                displayName = entityDataTable.Rows[0].Field<string>("display_name");
                            }

                            if (String.IsNullOrWhiteSpace(displayName))
                            {
                                displayName = conflict.FieldName;
                            }

                            fieldDisplayNames.Add(fieldKey, displayName);
                        }

                        conflict.FieldDisplayName = fieldDisplayNames[fieldKey];

                        break;
                    }
                    case "UPDATE_ITEMLINKDETAIL":
                    case "CHANGE_LINK":
                    {
                        // Get the type number and name of the link.
                        if (!links.ContainsKey(conflict.ObjectId))
                        {
                            await using var branchCommand = branchConnection.CreateCommand();
                            branchCommand.Parameters.AddWithValue("id", conflict.ObjectId);
                            branchCommand.CommandText = $"SELECT type, item_id, destination_item_id FROM {conflict.TableName.Replace(WiserTableNames.WiserItemLinkDetail, WiserTableNames.WiserItemLink)} WHERE id = ?id";
                            var entityDataTable = new DataTable();
                            using var adapter = new MySqlDataAdapter(branchCommand);
                            adapter.Fill(entityDataTable);

                            var type = 0;
                            if (entityDataTable.Rows.Count > 0)
                            {
                                type = entityDataTable.Rows[0].Field<int>("type");
                            }

                            links.Add(conflict.ObjectId, type);
                        }

                        // Get the display name for the link type.
                        if (!linkTypeSettings.ContainsKey(links[conflict.ObjectId]))
                        {
                            var settings = await wiserItemsService.GetLinkTypeSettingsAsync(links[conflict.ObjectId]);
                            if (String.IsNullOrWhiteSpace(settings.Name))
                            {
                                settings.Name = links[conflict.ObjectId].ToString();
                            }

                            linkTypeSettings.Add(links[conflict.ObjectId], settings);
                        }

                        // No need to check for conflicts if the user doesn't want to synchronise changes of this type.
                        if (!mergeBranchSettings.Entities.Any(x => (String.Equals(x.Type, linkTypeSettings[links[conflict.ObjectId]].SourceEntityType, StringComparison.OrdinalIgnoreCase) || String.Equals(x.Type, linkTypeSettings[links[conflict.ObjectId]].DestinationEntityType, StringComparison.OrdinalIgnoreCase)) && x.Update))
                        {
                            continue;
                        }

                        conflict.Type = links[conflict.ObjectId].ToString();
                        conflict.TypeDisplayName = linkTypeSettings[links[conflict.ObjectId]].Name;

                        // Get the display name for the field.
                        var languageCode = dataRow.Field<string>("language_code");
                        var fieldName = dataRow.Field<string>("field");
                        var fieldKey = $"{conflict.Type}_{fieldName}_{languageCode}";
                        if (!fieldDisplayNames.ContainsKey(fieldKey))
                        {
                            await using var branchCommand = branchConnection.CreateCommand();
                            branchCommand.Parameters.AddWithValue("fieldName", fieldName);
                            branchCommand.Parameters.AddWithValue("languageCode", languageCode);
                            branchCommand.Parameters.AddWithValue("linkType", conflict.Type);
                            branchCommand.CommandText = $"SELECT display_name FROM {WiserTableNames.WiserEntityProperty} WHERE link_type = ?linkType AND property_name = ?fieldName AND language_code = ?languageCode";
                            var entityDataTable = new DataTable();
                            using var adapter = new MySqlDataAdapter(branchCommand);
                            adapter.Fill(entityDataTable);

                            var displayName = "";
                            if (entityDataTable.Rows.Count > 0)
                            {
                                displayName = entityDataTable.Rows[0].Field<string>("display_name");
                            }

                            if (String.IsNullOrWhiteSpace(displayName))
                            {
                                displayName = conflict.FieldName;
                            }

                            fieldDisplayNames.Add(fieldKey, displayName);
                        }

                        conflict.FieldDisplayName = fieldDisplayNames[fieldKey];
                        break;
                    }
                    case "UPDATE_FILE":
                    {
                        conflict.Title = await GetDisplayNameAsync(linkSettings, "file_name");
                        break;
                    }
                    default:
                    {
                        continue;
                    }
                }

                conflict.ValueInMain = value;
                conflict.ChangeDateInMain = dataRow.Field<DateTime>("changed_on");
                conflict.ChangedByInMain = dataRow.Field<string>("changed_by");
            }
        }

        /// <inheritdoc />
        public async Task<ulong?> GetMappedIdAsync(ulong id, bool idIsFromBranch = true)
        {
            await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> {WiserTableNames.WiserIdMappings});
            
            clientDatabaseConnection.AddParameter("id", id);
            var dataTable = await clientDatabaseConnection.GetAsync($"""
                SELECT {(idIsFromBranch ? "production_id" : "our_id")} AS mappedId
                FROM {WiserTableNames.WiserIdMappings}
                WHERE {(idIsFromBranch ? "our_id" : "production_id")} = ?id
""");
            
            return dataTable.Rows.Count > 0 ? dataTable.Rows[0].Field<ulong>("mappedId") : null;
        }
        
        /// <inheritdoc />
        public async Task<ulong> GenerateNewIdAsync(string tableName, IDatabaseConnection mainDatabaseConnection, IDatabaseConnection branchDatabase)
        {
            var query = $"SELECT MAX(id) AS id FROM {tableName}";
            
            var dataTable = await mainDatabaseConnection.GetAsync(query);
            var maxMainId = dataTable.Rows.Count > 0 ? Convert.ToUInt64(dataTable.Rows[0]["id"]) : 0UL;

            if (branchDatabase != null)
            {
                dataTable = await branchDatabase.GetAsync(query);
                var maxBranchId = dataTable.Rows.Count > 0 ? Convert.ToUInt64(dataTable.Rows[0]["id"]) : 0UL;
                return Math.Max(maxMainId, maxBranchId) + 1;
            }

            return maxMainId + 1;
        }
    }
}