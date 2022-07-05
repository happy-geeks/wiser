using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Services;
using Api.Modules.Branches.Interfaces;
using Api.Modules.Branches.Models;
using Api.Modules.Customers.Enums;
using Api.Modules.Customers.Interfaces;
using Api.Modules.Customers.Models;
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
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace Api.Modules.Branches.Services
{
    /// <inheritdoc cref="IBranchesService" />
    public class BranchesService : IBranchesService, IScopedService
    {
        private readonly IWiserCustomersService wiserCustomersService;
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IDatabaseHelpersService databaseHelpersService;
        private readonly ILogger<BranchesService> logger;
        private readonly IWiserItemsService wiserItemsService;
        private readonly IDatabaseConnection wiserDatabaseConnection;

        /// <summary>
        /// Creates a new instance of <see cref="BranchesService"/>.
        /// </summary>
        public BranchesService(IWiserCustomersService wiserCustomersService, IDatabaseConnection connection, IDatabaseHelpersService databaseHelpersService, ILogger<BranchesService> logger, IWiserItemsService wiserItemsService)
        {
            this.wiserCustomersService = wiserCustomersService;
            this.clientDatabaseConnection = connection;
            this.databaseHelpersService = databaseHelpersService;
            this.logger = logger;
            this.wiserItemsService = wiserItemsService;

            if (clientDatabaseConnection is ClientDatabaseConnection databaseConnection)
            {
                wiserDatabaseConnection = databaseConnection.WiserDatabaseConnection;
            }
        }

        /// <inheritdoc />
        public async Task<ServiceResult<CustomerModel>> CreateAsync(ClaimsIdentity identity, CreateBranchSettingsModel settings)
        {
            if (String.IsNullOrWhiteSpace(settings?.Name))
            {
                return new ServiceResult<CustomerModel>
                {
                    ErrorMessage = "Name is empty",
                    StatusCode = HttpStatusCode.BadRequest
                };
            }

            // Make sure the queue table exists and is up-to-date.
            await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> {WiserTableNames.WiserBranchesQueue});
            
            var currentCustomer = (await wiserCustomersService.GetSingleAsync(identity, true)).ModelObject;
            var subDomain = currentCustomer.SubDomain;
            var newCustomerName = $"{currentCustomer.Name} - {settings.Name}";
            var newCustomerTitle = $"{currentCustomer.WiserTitle} - {settings.Name}";

            // If the ID is not the same as the customer ID, it means this is not the main/production environment of this customer.
            // Then we want to get the sub domain of the main/production environment of the customer, to use as base for the new sub domain for the new environment.
            if (currentCustomer.Id != currentCustomer.CustomerId)
            {
                wiserDatabaseConnection.AddParameter("customerId", currentCustomer.CustomerId);
                var dataTable = await wiserDatabaseConnection.GetAsync($"SELECT subdomain, name, wiser_title FROM {ApiTableNames.WiserCustomers} WHERE id = ?customerId");
                if (dataTable.Rows.Count == 0)
                {
                    // This should never happen, hence the exception.
                    throw new Exception("Customer not found");
                }

                subDomain = dataTable.Rows[0].Field<string>("subdomain");
                newCustomerName = $"{dataTable.Rows[0].Field<string>("name")} - {settings.Name}";
                newCustomerTitle = $"{dataTable.Rows[0].Field<string>("wiser_title")} - {settings.Name}";
            }

            // Create a valid database and sub domain name for the new environment.
            var databaseNameBuilder = new StringBuilder(settings.Name.Trim().ToLowerInvariant());
            databaseNameBuilder = Path.GetInvalidFileNameChars().Aggregate(databaseNameBuilder, (current, invalidChar) => current.Replace(invalidChar.ToString(), ""));
            databaseNameBuilder = databaseNameBuilder.Replace(@"\", "_").Replace(@"/", "_").Replace(".", "_").Replace(" ", "_");

            var databaseName = $"{currentCustomer.Database.DatabaseName}_{databaseNameBuilder}".ToMySqlSafeValue(false);
            if (databaseName.Length > 54)
            {
                databaseName = $"{databaseName[..54]}{DateTime.Now:yyMMddHHmm}";
            }

            subDomain += $"_{databaseNameBuilder}";

            // Make sure no customer exists yet with this name and/or sub domain.
            var customerExists = await wiserCustomersService.CustomerExistsAsync(newCustomerName, subDomain);
            if (customerExists.StatusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<CustomerModel>
                {
                    ErrorMessage = customerExists.ErrorMessage,
                    StatusCode = customerExists.StatusCode
                };
            }

            if (customerExists.ModelObject != CustomerExistsResults.Available)
            {
                return new ServiceResult<CustomerModel>
                {
                    StatusCode = HttpStatusCode.Conflict,
                    ErrorMessage = $"Een branch met de naam '{settings.Name}' bestaat al."
                };
            }

            // Make sure the database doesn't exist yet.
            if (await databaseHelpersService.DatabaseExistsAsync(databaseName))
            {
                return new ServiceResult<CustomerModel>
                {
                    StatusCode = HttpStatusCode.Conflict,
                    ErrorMessage = $"We hebben geprobeerd een database aan te maken met de naam '{databaseName}', echter bestaat deze al. Kies a.u.b. een andere naam, of neem contact op met ons."
                };
            }

            settings.NewCustomerName = newCustomerName;
            settings.SubDomain = subDomain;
            settings.WiserTitle = newCustomerTitle;
            settings.DatabaseName = databaseName;

            // Add the new customer environment to easy_customers. We do this here already so that the AIS doesn't need access to the main wiser database.
            var newCustomer = new CustomerModel
            {
                CustomerId = currentCustomer.CustomerId,
                Name = newCustomerName,
                EncryptionKey = SecurityHelpers.GenerateRandomPassword(20),
                SubDomain = subDomain,
                WiserTitle = newCustomerTitle,
                Database = new ConnectionInformationModel
                {
                    Host = currentCustomer.Database.Host,
                    Password = currentCustomer.Database.Password,
                    Username = currentCustomer.Database.Username,
                    PortNumber = currentCustomer.Database.PortNumber,
                    DatabaseName = databaseName
                }
            };
            
            await wiserCustomersService.CreateOrUpdateCustomerAsync(newCustomer);
            
            // Clear some data that we don't want to return to client.
            newCustomer.Database.Host = null;
            newCustomer.Database.Password = null;
            newCustomer.Database.Username = null;
            newCustomer.Database.PortNumber = 0;
            
            // Add the creation of the branch to the queue, so that the AIS can process it.
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("name", settings.Name);
            clientDatabaseConnection.AddParameter("action", "create");
            clientDatabaseConnection.AddParameter("branch_id", newCustomer.Id);
            clientDatabaseConnection.AddParameter("data", JsonConvert.SerializeObject(settings));
            clientDatabaseConnection.AddParameter("added_on", DateTime.Now);
            clientDatabaseConnection.AddParameter("start_on", settings.StartOn ?? DateTime.Now);
            clientDatabaseConnection.AddParameter("added_by", IdentityHelpers.GetUserName(identity, true));
            clientDatabaseConnection.AddParameter("user_id", IdentityHelpers.GetWiserUserId(identity));
            await clientDatabaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(WiserTableNames.WiserBranchesQueue, 0);
            
            return new ServiceResult<CustomerModel>(newCustomer);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<CustomerModel>>> GetAsync(ClaimsIdentity identity)
        {
            var currentCustomer = (await wiserCustomersService.GetSingleAsync(identity, true)).ModelObject;

            var query = $@"SELECT id, name, subdomain, db_dbname
FROM {ApiTableNames.WiserCustomers}
WHERE customerid = ?id
AND id <> ?id
ORDER BY id DESC";
            
            wiserDatabaseConnection.AddParameter("id", currentCustomer.CustomerId);
            var dataTable = await wiserDatabaseConnection.GetAsync(query);
            var results = new List<CustomerModel>();
            foreach (DataRow dataRow in dataTable.Rows)
            {
                results.Add(new CustomerModel
                {
                    Id = dataRow.Field<int>("id"),
                    CustomerId = currentCustomer.CustomerId,
                    Name = dataRow.Field<string>("name"),
                    SubDomain = dataRow.Field<string>("subdomain"),
                    Database = new ConnectionInformationModel
                    {
                        DatabaseName = dataRow.Field<string>("db_dbname")
                    }
                });
            }

            return new ServiceResult<List<CustomerModel>>(results);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> IsMainBranchAsync(ClaimsIdentity identity)
        {
            var currentCustomer = (await wiserCustomersService.GetSingleAsync(identity, true)).ModelObject;

            return new ServiceResult<bool>(currentCustomer.Id == currentCustomer.CustomerId);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<ChangesAvailableForMergingModel>> GetChangesAsync(ClaimsIdentity identity, int id)
        {
            var currentCustomer = (await wiserCustomersService.GetSingleAsync(identity, true)).ModelObject;
            
            var result = new ChangesAvailableForMergingModel();
            // If the id is 0, then get the current branch where the user is authenticated, otherwise get the branch of the given ID.
            var selectedEnvironmentCustomer = id <= 0
                ? currentCustomer
                : (await wiserCustomersService.GetSingleAsync(id, true)).ModelObject;

            // Only allow users to get the changes of their own branches.
            if (currentCustomer.CustomerId != selectedEnvironmentCustomer.CustomerId)
            {
                return new ServiceResult<ChangesAvailableForMergingModel>
                {
                    StatusCode = HttpStatusCode.Forbidden
                };
            }

            await using var branchConnection = new MySqlConnection(wiserCustomersService.GenerateConnectionStringFromCustomer(selectedEnvironmentCustomer));
            await branchConnection.OpenAsync();
            
            // Get all history since last synchronisation.
            var dataTable = new DataTable();
            await using (var environmentCommand = branchConnection.CreateCommand())
            {
                environmentCommand.CommandText = $"SELECT action, tablename, item_id, field, oldvalue, newvalue FROM `{WiserTableNames.WiserHistory}` ORDER BY id ASC";
                using var environmentAdapter = new MySqlDataAdapter(environmentCommand);
                await environmentAdapter.FillAsync(dataTable);
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
                
                var entityChangesModel = result.Entities.FirstOrDefault(setting => setting.EntityType == entityType);
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
                if (idToEntityTypeMappings.ContainsKey(itemId))
                {
                    return idToEntityTypeMappings[itemId];
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
                    await environmentAdapter.FillAsync(getEntityTypeDataTable);
                }

                var entityType = getEntityTypeDataTable.Rows.Count == 0 ? null : getEntityTypeDataTable.Rows[0].Field<string>("entity_type");
                idToEntityTypeMappings.Add(itemId, entityType);
                return entityType;
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
                await linkAdapter.FillAsync(linkDataTable);

                if (linkDataTable.Rows.Count == 0)
                {
                    return null;
                }

                return (linkDataTable.Rows[0].Field<int>("type"), linkDataTable.Rows[0].Field<ulong>("item_id"), linkDataTable.Rows[0].Field<ulong>("destination_item_id"));
            }

            // Count all changed items and settings (if a single item has been changed multiple times, we count only one change).
            foreach (DataRow dataRow in dataTable.Rows)
            {
                var action = dataRow.Field<string>("action")?.ToUpperInvariant();
                var tableName = dataRow.Field<string>("tablename") ?? "";
                var itemId = dataRow.Field<ulong>("item_id");
                var field = dataRow.Field<string>("field") ?? "";
                var oldValue = dataRow.Field<string>("oldvalue");
                var newValue = dataRow.Field<string>("newvalue");

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
                        var linkData = await GetEntityTypesOfLinkAsync(sourceItemId, destinationItemId, type, branchConnection);
                        if (linkData == null)
                        {
                            break;
                        }
                        
                        AddItemToMutationList(updatedItems, await wiserItemsService.GetTablePrefixForEntityAsync(linkData.Value.SourceType), sourceItemId, linkData.Value.SourceType);
                        AddItemToMutationList(updatedItems, await wiserItemsService.GetTablePrefixForEntityAsync(linkData.Value.DestinationType), destinationItemId, linkData.Value.DestinationType);

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
                        var entityData = await GetEntityTypesOfLinkAsync(linkData.Value.SourceItemId, linkData.Value.DestinationItemId, linkData.Value.Type, branchConnection);
                        if (!entityData.HasValue)
                        {
                            break;
                        }
                        
                        // And finally mark these items as updated.
                        AddItemToMutationList(updatedItems, await wiserItemsService.GetTablePrefixForEntityAsync(entityData.Value.SourceType), linkData.Value.SourceItemId, entityData.Value.SourceType);
                        AddItemToMutationList(updatedItems, await wiserItemsService.GetTablePrefixForEntityAsync(entityData.Value.DestinationType), linkData.Value.DestinationItemId, entityData.Value.DestinationType);

                        break;
                    }
                    case "REMOVE_LINK":
                    {
                        var sourceItemId = UInt64.Parse(oldValue!);
                        var linkData = await GetEntityTypesOfLinkAsync(sourceItemId, itemId, Int32.Parse(field), branchConnection);
                        if (linkData == null)
                        {
                            break;
                        }
                        
                        AddItemToMutationList(updatedItems, await wiserItemsService.GetTablePrefixForEntityAsync(linkData.Value.SourceType), sourceItemId, linkData.Value.SourceType);
                        AddItemToMutationList(updatedItems, await wiserItemsService.GetTablePrefixForEntityAsync(linkData.Value.DestinationType), itemId, linkData.Value.DestinationType);

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
                        var entityData = await GetEntityTypesOfLinkAsync(linkData.Value.SourceItemId, linkData.Value.DestinationItemId, linkData.Value.Type, branchConnection);
                        if (!entityData.HasValue)
                        {
                            break;
                        }
                        
                        // And finally mark these items as updated.
                        AddItemToMutationList(updatedItems, await wiserItemsService.GetTablePrefixForEntityAsync(entityData.Value.SourceType), linkData.Value.SourceItemId, entityData.Value.SourceType);
                        AddItemToMutationList(updatedItems, await wiserItemsService.GetTablePrefixForEntityAsync(entityData.Value.DestinationType), linkData.Value.DestinationItemId, entityData.Value.DestinationType);

                        break;
                    }
                    case "UPDATE_FILE":
                    {
                        var fileDataTable = new DataTable();
                        await using var linkCommand = branchConnection.CreateCommand();
                        linkCommand.Parameters.AddWithValue("id", itemId);
                        linkCommand.CommandText = $@"SELECT item_id, itemlink_id FROM `{tableName}` WHERE id = ?id
UNION ALL
SELECT item_id, itemlink_id FROM `{tableName}{WiserTableNames.ArchiveSuffix}` WHERE id = ?id
LIMIT 1";
                        using var linkAdapter = new MySqlDataAdapter(linkCommand);
                        await linkAdapter.FillAsync(fileDataTable);

                        if (fileDataTable.Rows.Count == 0)
                        {
                            break;
                        }

                        var itemIdFromFile = fileDataTable.Rows[0].Field<ulong>("item_id");
                        if (itemIdFromFile > 0)
                        {
                            var tablePrefix = BranchesHelpers.GetTablePrefix(tableName, itemIdFromFile);
                            AddItemToMutationList(updatedItems, tablePrefix.TablePrefix, itemIdFromFile);
                            break;
                        }

                        // First get the source item ID and destination item ID of the link.
                        var linkIdFromFile = fileDataTable.Rows[0].Field<ulong>("itemlink_id");
                        var linkData = await GetDataFromLinkAsync(linkIdFromFile, BranchesHelpers.GetTablePrefix(tableName, 0).TablePrefix, branchConnection);
                        if (!linkData.HasValue)
                        {
                            break;
                        }
                        
                        // Then get the entity types of those IDs.
                        var entityData = await GetEntityTypesOfLinkAsync(linkData.Value.SourceItemId, linkData.Value.DestinationItemId, linkData.Value.Type, branchConnection);
                        if (!entityData.HasValue)
                        {
                            break;
                        }
                        
                        // And finally mark these items as updated.
                        AddItemToMutationList(updatedItems, await wiserItemsService.GetTablePrefixForEntityAsync(entityData.Value.SourceType), linkData.Value.SourceItemId, entityData.Value.SourceType);
                        AddItemToMutationList(updatedItems, await wiserItemsService.GetTablePrefixForEntityAsync(entityData.Value.DestinationType), linkData.Value.DestinationItemId, entityData.Value.DestinationType);
                        
                        break;
                    }
                }
            }

            // Add the counters to the results.
            foreach (var item in createdItems)
            {
                var entityType = item.EntityType;
                if (String.IsNullOrWhiteSpace(entityType))
                {
                    entityType = await GetEntityTypeFromIdAsync(item.ItemId, item.TablePrefix, branchConnection);
                }

                (await GetOrAddEntityTypeCounterAsync(entityType)).Created++;
            }
            
            foreach (var item in updatedItems)
            {
                var entityType = item.EntityType;
                if (String.IsNullOrWhiteSpace(entityType))
                {
                    entityType = await GetEntityTypeFromIdAsync(item.ItemId, item.TablePrefix, branchConnection);
                }

                (await GetOrAddEntityTypeCounterAsync(entityType)).Updated++;
            }
            
            foreach (var item in deletedItems)
            {
                var entityType = item.EntityType;
                if (String.IsNullOrWhiteSpace(entityType))
                {
                    entityType = await GetEntityTypeFromIdAsync(item.ItemId, item.TablePrefix, branchConnection);
                }

                (await GetOrAddEntityTypeCounterAsync(entityType)).Deleted++;
            }

            foreach (var setting in createdSettings)
            {
                GetOrAddWiserSettingCounter(setting.Key).Created = setting.Value.Count;
            }

            foreach (var setting in updatedSettings)
            {
                GetOrAddWiserSettingCounter(setting.Key).Updated = setting.Value.Count;
            }

            foreach (var setting in deletedSettings)
            {
                GetOrAddWiserSettingCounter(setting.Key).Deleted = setting.Value.Count;
            }

            return new ServiceResult<ChangesAvailableForMergingModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<MergeBranchResultModel>> MergeAsync(ClaimsIdentity identity, MergeBranchSettingsModel settings)
        {
            var result = new MergeBranchResultModel();
            var currentCustomer = (await wiserCustomersService.GetSingleAsync(identity, true)).ModelObject;
            var productionCustomer = (await wiserCustomersService.GetSingleAsync(currentCustomer.CustomerId, true)).ModelObject;

            // If the settings.Id is 0, it means the user wants to merge the current branch.
            if (settings.Id <= 0)
            {
                settings.Id = currentCustomer.Id;
                settings.DatabaseName = currentCustomer.Database.DatabaseName;
            }

            // Make sure the user is not trying to copy changes from main to main, that would be weird and also cause a lot of problems.
            if (currentCustomer.CustomerId == settings.Id)
            {
                return new ServiceResult<MergeBranchResultModel>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "U probeert wijzigingen van de hoofdbranch te synchroniseren, dat is niet mogelijk."
                };
            }

            var selectedBranchCustomer = settings.Id == currentCustomer.Id
                ? currentCustomer
                : (await wiserCustomersService.GetSingleAsync(settings.Id, true)).ModelObject;

            // Check to make sure someone is not trying to copy changes from an environment that does not belong to them.
            if (selectedBranchCustomer == null || currentCustomer.CustomerId != selectedBranchCustomer.CustomerId)
            {
                return new ServiceResult<MergeBranchResultModel>
                {
                    StatusCode = HttpStatusCode.Forbidden
                };
            }

            DateTime? lastMergeDate = null;

            // Get the date and time of the last merge of this branch, so we can find all changes made in production after this date, to check for merge conflicts.
            await using var mainConnection = new MySqlConnection(wiserCustomersService.GenerateConnectionStringFromCustomer(productionCustomer));
            await mainConnection.OpenAsync();
            await using (var productionCommand = mainConnection.CreateCommand())
            {
                productionCommand.Parameters.AddWithValue("branchId", selectedBranchCustomer.Id);
                productionCommand.CommandText = $"SELECT MAX(finished_on) AS lastMergeDate FROM {WiserTableNames.WiserBranchesQueue} WHERE branch_id = ?branchId AND success = 1 AND finished_on IS NOT NULL";

                var dataTable = new DataTable();
                using var sourceAdapter = new MySqlDataAdapter(productionCommand);
                await sourceAdapter.FillAsync(dataTable);
                if (dataTable.Rows.Count > 0)
                {
                    lastMergeDate = dataTable.Rows[0].Field<DateTime?>("lastMergeDate");
                }
            }

            await using var branchConnection = new MySqlConnection(wiserCustomersService.GenerateConnectionStringFromCustomer(selectedBranchCustomer));
            await branchConnection.OpenAsync();

            // If we have no last merge date, it probably means someone removed a record from wiser_branch_queue, in that case get the date of the first change in wiser_history in the branch. 
            if (!lastMergeDate.HasValue)
            {
                await using var branchCommand = branchConnection.CreateCommand();
                branchCommand.CommandText = $@"SELECT MIN(changed_on) AS firstChangeDate FROM {WiserTableNames.WiserHistory}";
                var dataTable = new DataTable();
                using var branchAdapter = new MySqlDataAdapter(branchCommand);
                await branchAdapter.FillAsync(dataTable);
                if (dataTable.Rows.Count > 0)
                {
                    lastMergeDate = dataTable.Rows[0].Field<DateTime?>("firstChangeDate");
                }
            }

            // If we somehow still don't have a last merge date, then we can't check for merge conflicts. This should never happen under normal circumstances.
            if (lastMergeDate.HasValue)
            {
                var moduleNames = new Dictionary<ulong, string>();
                var entityTypes = new Dictionary<ulong, string>();
                var entityTypeSettings = new Dictionary<string, EntitySettingsModel>();

                var conflicts = new List<MergeConflictModel>();

                // Get all changes from branch.
                var dataTable = new DataTable();
                await using (var branchCommand = branchConnection.CreateCommand())
                {
                    branchCommand.CommandText = $@"SELECT 
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
                    await branchAdapter.FillAsync(dataTable);
                }

                foreach (DataRow dataRow in dataTable.Rows)
                {
                    var action = dataRow.Field<string>("action")?.ToUpperInvariant();
                    var conflict = new MergeConflictModel
                    {
                        Id = dataRow.Field<ulong>("item_id"),
                        TableName = dataRow.Field<string>("table_name"),
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
                            conflict.Type = "entityProperty";
                            conflict.Title = "Veld van entiteit";
                            conflict.FieldDisplayName = conflict.FieldName;
                            break;
                        }
                        case "UPDATE_MODULE":
                        {
                            conflict.Type = "module";

                            if (!moduleNames.ContainsKey(conflict.Id))
                            {
                                await using var branchCommand = branchConnection.CreateCommand();
                                branchCommand.Parameters.AddWithValue("id", conflict.Id);
                                branchCommand.CommandText = $"SELECT name FROM {WiserTableNames.WiserModule} WHERE id = ?id";
                                var moduleDataTable = new DataTable();
                                using var branchAdapter = new MySqlDataAdapter(branchCommand);
                                await branchAdapter.FillAsync(moduleDataTable);
                                moduleNames.Add(conflict.Id, moduleDataTable.Rows.Count == 0 ? "Onbekend" : moduleDataTable.Rows[0].Field<string>("name"));
                            }

                            conflict.Title = $"Module ({moduleNames[conflict.Id]})";

                            conflict.FieldDisplayName = conflict.FieldName;
                            break;
                        }
                        case "UPDATE_QUERY":
                        {
                            break;
                        }
                        case "INSERT_ENTITY":
                        {
                            break;
                        }
                        case "UPDATE_ENTITY":
                        {
                            break;
                        }
                        case "DELETE_ENTITY":
                        {
                            break;
                        }
                        case "INSERT_FIELD_TEMPLATE":
                        {
                            break;
                        }
                        case "UPDATE_FIELD_TEMPLATE":
                        {
                            break;
                        }
                        case "DELETE_FIELD_TEMPLATE":
                        {
                            break;
                        }
                        case "INSERT_LINK_SETTING":
                        {
                            break;
                        }
                        case "UPDATE_LINK_SETTING":
                        {
                            break;
                        }
                        case "DELETE_LINK_SETTING":
                        {
                            break;
                        }
                        case "INSERT_PERMISSION":
                        {
                            break;
                        }
                        case "UPDATE_PERMISSION":
                        {
                            break;
                        }
                        case "DELETE_PERMISSION":
                        {
                            break;
                        }
                        case "INSERT_USER_ROLE":
                        {
                            break;
                        }
                        case "UPDATE_USER_ROLE":
                        {
                            break;
                        }
                        case "DELETE_USER_ROLE":
                        {
                            break;
                        }
                        case "INSERT_API_CONNECTION":
                        {
                            break;
                        }
                        case "UPDATE_API_CONNECTION":
                        {
                            break;
                        }
                        case "DELETE_API_CONNECTION":
                        {
                            break;
                        }
                        case "INSERT_DATA_SELECTOR":
                        {
                            break;
                        }
                        case "UPDATE_DATA_SELECTOR":
                        {
                            break;
                        }
                        case "DELETE_DATA_SELECTOR":
                        {
                            break;
                        }

                        // Changes to items.
                        case "CREATE_ITEM":
                        {
                            break;
                        }
                        case "UPDATE_ITEM":
                        {
                            break;
                        }
                        case "DELETE_ITEM":
                        {
                            break;
                        }
                        case "ADD_LINK":
                        {

                            break;
                        }
                        case "UPDATE_ITEMLINKDETAIL":
                        case "CHANGE_LINK":
                        {

                            break;
                        }
                        case "REMOVE_LINK":
                        {

                            break;
                        }
                        case "ADD_FILE" when oldValue == "item_id":
                        case "DELETE_FILE" when oldValue == "item_id":
                        {

                            break;
                        }
                        case "ADD_FILE" when oldValue == "itemlink_id":
                        case "DELETE_FILE" when oldValue == "itemlink_id":
                        {

                            break;
                        }
                        case "UPDATE_FILE":
                        {

                            break;
                        }
                        default:
                            continue;
                    }

                    conflicts.Add(conflict);
                }

                await using (var productionCommand = mainConnection.CreateCommand())
                {
                    productionCommand.Parameters.AddWithValue("lastChange", lastMergeDate.Value);
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
                        dataTable.Clear();
                        await branchAdapter.FillAsync(dataTable);
                    }

                    foreach (DataRow dataRow in dataTable.Rows)
                    {
                        var action = dataRow.Field<string>("action")?.ToUpperInvariant();

                        var conflict = conflicts.SingleOrDefault(conflict => conflict.Id == dataRow.Field<ulong>("item_id")
                                                                             && conflict.TableName == dataRow.Field<string>("table_name")
                                                                             && conflict.LanguageCode == dataRow.Field<string>("language_code")
                                                                             && conflict.GroupName == dataRow.Field<string>("groupname")
                                                                             && conflict.FieldName == dataRow.Field<string>("field"));

                        if (conflict == null)
                        {
                            continue;
                        }

                        switch (action)
                        {
                            case "UPDATE_ENTITYPROPERTY":
                            {
                                break;
                            }
                            case "UPDATE_MODULE":
                            {
                                if (!moduleNames.ContainsKey(conflict.Id))
                                {
                                    await using var branchCommand = branchConnection.CreateCommand();
                                    branchCommand.Parameters.AddWithValue("id", conflict.Id);
                                    branchCommand.CommandText = $"SELECT name FROM {WiserTableNames.WiserModule} WHERE id = ?id";
                                    var moduleDataTable = new DataTable();
                                    using var branchAdapter = new MySqlDataAdapter(branchCommand);
                                    await branchAdapter.FillAsync(moduleDataTable);
                                    moduleNames.Add(conflict.Id, moduleDataTable.Rows.Count == 0 ? "Onbekend" : moduleDataTable.Rows[0].Field<string>("name"));
                                }

                                conflict.Title = $"Module ({moduleNames[conflict.Id]})";

                                conflict.FieldDisplayName = conflict.FieldName;
                                break;
                            }
                            case "UPDATE_QUERY":
                            {
                                break;
                            }
                            case "INSERT_ENTITY":
                            {
                                break;
                            }
                            case "UPDATE_ENTITY":
                            {
                                break;
                            }
                            case "DELETE_ENTITY":
                            {
                                break;
                            }
                            case "INSERT_FIELD_TEMPLATE":
                            {
                                break;
                            }
                            case "UPDATE_FIELD_TEMPLATE":
                            {
                                break;
                            }
                            case "DELETE_FIELD_TEMPLATE":
                            {
                                break;
                            }
                            case "INSERT_LINK_SETTING":
                            {
                                break;
                            }
                            case "UPDATE_LINK_SETTING":
                            {
                                break;
                            }
                            case "DELETE_LINK_SETTING":
                            {
                                break;
                            }
                            case "INSERT_PERMISSION":
                            {
                                break;
                            }
                            case "UPDATE_PERMISSION":
                            {
                                break;
                            }
                            case "DELETE_PERMISSION":
                            {
                                break;
                            }
                            case "INSERT_USER_ROLE":
                            {
                                break;
                            }
                            case "UPDATE_USER_ROLE":
                            {
                                break;
                            }
                            case "DELETE_USER_ROLE":
                            {
                                break;
                            }
                            case "INSERT_API_CONNECTION":
                            {
                                break;
                            }
                            case "UPDATE_API_CONNECTION":
                            {
                                break;
                            }
                            case "DELETE_API_CONNECTION":
                            {
                                break;
                            }
                            case "INSERT_DATA_SELECTOR":
                            {
                                break;
                            }
                            case "UPDATE_DATA_SELECTOR":
                            {
                                break;
                            }
                            case "DELETE_DATA_SELECTOR":
                            {
                                break;
                            }

                            // Changes to items.
                            case "CREATE_ITEM":
                            {
                                break;
                            }
                            case "UPDATE_ITEM":
                            {
                                break;
                            }
                            case "DELETE_ITEM":
                            {
                                break;
                            }
                            case "ADD_LINK":
                            {

                                break;
                            }
                            case "UPDATE_ITEMLINKDETAIL":
                            case "CHANGE_LINK":
                            {

                                break;
                            }
                            case "REMOVE_LINK":
                            {

                                break;
                            }
                            case "ADD_FILE" when oldValue == "item_id":
                            case "DELETE_FILE" when oldValue == "item_id":
                            {

                                break;
                            }
                            case "ADD_FILE" when oldValue == "itemlink_id":
                            case "DELETE_FILE" when oldValue == "itemlink_id":
                            {

                                break;
                            }
                            case "UPDATE_FILE":
                            {

                                break;
                            }
                            default:
                                continue;
                        }
                    }
                }
            }

            // Add the merge to the queue so that the AIS will process it.
            await using (var productionCommand = mainConnection.CreateCommand())
            {
                productionCommand.Parameters.AddWithValue("branch_id", settings.Id);
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

        /// <summary>
        /// Get the entity types and table prefixes for both items in a link.
        /// </summary>
        /// <param name="sourceId">The ID of the source item.</param>
        /// <param name="destinationId">The ID of the destination item.</param>
        /// <param name="linkType">The link type number.</param>
        /// <param name="mySqlConnection">The connection to the database.</param>
        /// <returns>A named tuple with the entity types and table prefixes for both the source and the destination.</returns>
        private async Task<(string SourceType, string SourceTablePrefix, string DestinationType, string DestinationTablePrefix)?> GetEntityTypesOfLinkAsync(ulong sourceId, ulong destinationId, int linkType, MySqlConnection mySqlConnection)
        {
            var allLinkTypeSettings = (await wiserItemsService.GetAllLinkTypeSettingsAsync()).Where(l => l.Type == linkType);
            await using var command = mySqlConnection.CreateCommand();
            command.Parameters.AddWithValue("sourceId", sourceId);
            command.Parameters.AddWithValue("destinationId", destinationId);
            
            // It's possible that there are multiple link types that use the same number, so we have to check all of them.
            foreach (var linkTypeSettings in allLinkTypeSettings)
            {
                var sourceTablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(linkTypeSettings.SourceEntityType);
                var destinationTablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(linkTypeSettings.DestinationEntityType);

                // Check if the source item exists in this table.
                command.CommandText = $@"SELECT entity_type FROM {sourceTablePrefix}{WiserTableNames.WiserItem} WHERE id = ?sourceId
UNION ALL
SELECT entity_type FROM {sourceTablePrefix}{WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix} WHERE id = ?sourceId
LIMIT 1";
                var sourceDataTable = new DataTable();
                using var sourceAdapter = new MySqlDataAdapter(command);
                await sourceAdapter.FillAsync(sourceDataTable);
                if (sourceDataTable.Rows.Count == 0 || !String.Equals(sourceDataTable.Rows[0].Field<string>("entity_type"), linkTypeSettings.SourceEntityType))
                {
                    continue;
                }

                // Check if the destination item exists in this table.
                command.CommandText = $@"SELECT entity_type FROM {destinationTablePrefix}{WiserTableNames.WiserItem} WHERE id = ?sourceId
UNION ALL
SELECT entity_type FROM {destinationTablePrefix}{WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix} WHERE id = ?sourceId
LIMIT 1";
                var destinationDataTable = new DataTable();
                using var destinationAdapter = new MySqlDataAdapter(command);
                await destinationAdapter.FillAsync(destinationDataTable);
                if (destinationDataTable.Rows.Count == 0 || !String.Equals(destinationDataTable.Rows[0].Field<string>("entity_type"), linkTypeSettings.DestinationEntityType))
                {
                    continue;
                }
                
                // If we reached this point, it means we found the correct link type and entity types.
                return (linkTypeSettings.SourceEntityType, sourceTablePrefix, linkTypeSettings.DestinationEntityType, destinationTablePrefix);
            }

            return null;
        }
    }
}