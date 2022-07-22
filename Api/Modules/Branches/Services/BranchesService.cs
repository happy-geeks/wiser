﻿using System;
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
                var customerModel = results.FirstOrDefault(customer => customer.Id == id);
                if (customerModel == null)
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

                customerModel.Name += $" (Status: {statusMessage})";
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

                return (linkDataTable.Rows[0].Field<int>("type"), Convert.ToUInt64(linkDataTable.Rows[0]["item_id"]), Convert.ToUInt64(linkDataTable.Rows[0]["destination_item_id"]));
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

            settings.DatabaseName = selectedBranchCustomer.Database.DatabaseName;

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
                await branchAdapter.FillAsync(dataTable);
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
                await branchAdapter.FillAsync(dataTable);
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
                    await adapter.FillAsync(moduleDataTable);
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
                            await adapter.FillAsync(entityPropertyDataTable);

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
                        conflict.Title = await GetDisplayNameAsync(queryNames);
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
                            await adapter.FillAsync(entityDataTable);

                            var name = $"Onbekend, #{conflict.ObjectId}";
                            if (entityDataTable.Rows.Count > 0)
                            {
                                name = entityDataTable.Rows[0].Field<string>("friendly_name");
                                if (String.IsNullOrWhiteSpace(name))
                                {
                                    name = entityDataTable.Rows[0].Field<string>("name");
                                }
                            }

                            entityTypes.Add(conflict.ObjectId,  name);
                        }

                        conflict.Title = entityTypes[conflict.ObjectId];
                        break;
                    }
                    case "UPDATE_FIELD_TEMPLATE":
                    {
                        conflict.Title = await GetDisplayNameAsync(fieldTypes);
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
                            await adapter.FillAsync(entityDataTable);

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
                            await adapter.FillAsync(entityDataTable);

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
                            await adapter.FillAsync(entityDataTable);

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
                            await adapter.FillAsync(entityDataTable);

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
            var allLinkTypeSettings = (await wiserItemsService.GetAllLinkTypeSettingsAsync()).Where(l => l.Type == linkType).ToList();
            await using var command = mySqlConnection.CreateCommand();
            command.Parameters.AddWithValue("sourceId", sourceId);
            command.Parameters.AddWithValue("destinationId", destinationId);

            // If there are no settings for this link, we assume that the links are from items in the normal wiser_item table and not a table with a prefix.
            if (!allLinkTypeSettings.Any())
            {
                // Check if the source item exists in this table.
                command.CommandText = $@"SELECT entity_type FROM {WiserTableNames.WiserItem} WHERE id = ?sourceId
UNION ALL
SELECT entity_type FROM {WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix} WHERE id = ?sourceId
LIMIT 1";
                var sourceDataTable = new DataTable();
                using var sourceAdapter = new MySqlDataAdapter(command);
                await sourceAdapter.FillAsync(sourceDataTable);
                if (sourceDataTable.Rows.Count == 0)
                {
                    return null;
                }

                var sourceEntityType = sourceDataTable.Rows[0].Field<string>("entity_type");
                
                // Check if the destination item exists in this table.
                command.CommandText = $@"SELECT entity_type FROM {WiserTableNames.WiserItem} WHERE id = ?destinationId
UNION ALL
SELECT entity_type FROM {WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix} WHERE id = ?destinationId
LIMIT 1";
                var destinationDataTable = new DataTable();
                using var destinationAdapter = new MySqlDataAdapter(command);
                await destinationAdapter.FillAsync(destinationDataTable);
                if (destinationDataTable.Rows.Count == 0)
                {
                    return null;
                }
                
                var destinationEntityType = destinationDataTable.Rows[0].Field<string>("entity_type");

                return (sourceEntityType, "", destinationEntityType, "");
            }

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
                command.CommandText = $@"SELECT entity_type FROM {destinationTablePrefix}{WiserTableNames.WiserItem} WHERE id = ?destinationId
UNION ALL
SELECT entity_type FROM {destinationTablePrefix}{WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix} WHERE id = ?destinationId
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