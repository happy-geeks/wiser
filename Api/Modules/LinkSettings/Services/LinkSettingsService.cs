using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Services;
using Api.Modules.Branches.Interfaces;
using Api.Modules.LinkSettings.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

namespace Api.Modules.LinkSettings.Services
{
    /// <summary>
    /// Service for all CRUD operations for item link settings.
    /// </summary>
    public class LinkSettingsService : ILinkSettingsService, IScopedService
    {
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IWiserItemsService wiserItemsService;
        private readonly IDatabaseHelpersService databaseHelpersService;
        private readonly IServiceProvider serviceProvider;
        private readonly IBranchesService branchesService;

        /// <summary>
        /// Creates a new instance of <see cref="LinkSettingsService"/>.
        /// </summary>
        public LinkSettingsService(IDatabaseConnection clientDatabaseConnection, IWiserItemsService wiserItemsService, IDatabaseHelpersService databaseHelpersService, IServiceProvider serviceProvider, IBranchesService branchesService)
        {
            this.clientDatabaseConnection = clientDatabaseConnection;
            this.wiserItemsService = wiserItemsService;
            this.databaseHelpersService = databaseHelpersService;
            this.serviceProvider = serviceProvider;
            this.branchesService = branchesService;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<LinkSettingsModel>>> GetAllAsync(ClaimsIdentity identity, int branchId = 0)
        {
            using var scope = serviceProvider.CreateScope();
            var databaseConnectionResult = await branchesService.GetBranchDatabaseConnectionAsync(scope, identity, branchId);
            if (databaseConnectionResult.StatusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<List<LinkSettingsModel>>
                {
                    ErrorMessage = databaseConnectionResult.ErrorMessage,
                    StatusCode = databaseConnectionResult.StatusCode
                };
            }

            var databaseConnectionToUse = databaseConnectionResult.ModelObject;
            var branchWiserItemsService = scope.ServiceProvider.GetService<IWiserItemsService>();

            await databaseConnectionToUse.EnsureOpenConnectionForReadingAsync();
            return new ServiceResult<List<LinkSettingsModel>>(await branchWiserItemsService.GetAllLinkTypeSettingsAsync());
        }

        /// <inheritdoc />
        public async Task<ServiceResult<LinkSettingsModel>> GetAsync(ClaimsIdentity identity, int id)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

            // These are cached, so it's okay to get all of them. All other requests will then get them from the cache.
            var allLinkTypeSettings = await wiserItemsService.GetAllLinkTypeSettingsAsync();
            var result = allLinkTypeSettings.SingleOrDefault(l => l.Id == id);
            if (result == null)
            {
                return new ServiceResult<LinkSettingsModel>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = $"Link setting with ID '{id}' does not exist."
                };
            }

            return new ServiceResult<LinkSettingsModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<LinkSettingsModel>> CreateAsync(ClaimsIdentity identity, LinkSettingsModel linkSettings)
        {
            if (linkSettings.Type <= 0 || String.IsNullOrWhiteSpace(linkSettings.DestinationEntityType) || String.IsNullOrWhiteSpace(linkSettings.SourceEntityType) || String.IsNullOrWhiteSpace(linkSettings.Name))
            {
                return new ServiceResult<LinkSettingsModel>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = $"The properties '{nameof(linkSettings.Type)}', '{nameof(linkSettings.DestinationEntityType)}', '{nameof(linkSettings.SourceEntityType)}' and '{nameof(linkSettings.Name)}' need to contain a value."
                };
            }

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("type", linkSettings.Type);
            clientDatabaseConnection.AddParameter("destination_entity_type", linkSettings.DestinationEntityType);
            clientDatabaseConnection.AddParameter("connected_entity_type", linkSettings.SourceEntityType);
            clientDatabaseConnection.AddParameter("name", linkSettings.Name);
            clientDatabaseConnection.AddParameter("show_in_tree_view", linkSettings.ShowInTreeView);
            clientDatabaseConnection.AddParameter("show_in_data_selector", linkSettings.ShowInDataSelector);
            clientDatabaseConnection.AddParameter("relationship", ToDatabaseValue(linkSettings.Relationship));
            clientDatabaseConnection.AddParameter("duplication", ToDatabaseValue(linkSettings.DuplicationMethod));
            clientDatabaseConnection.AddParameter("use_item_parent_id", linkSettings.UseItemParentId);
            clientDatabaseConnection.AddParameter("cascade_delete", linkSettings.CascadeDelete);
            clientDatabaseConnection.AddParameter("use_dedicated_table", linkSettings.UseDedicatedTable);
            var query = $@"INSERT INTO {WiserTableNames.WiserLink} 
                        (
                            type,
                            destination_entity_type,
                            connected_entity_type,
                            name,
                            show_in_tree_view,
                            show_in_data_selector,
                            relationship,
                            duplication,
                            use_item_parent_id,
                            use_dedicated_table,
                            cascade_delete
                        )
                        VALUES
                        (
                            ?type,
                            ?destination_entity_type,
                            ?connected_entity_type,
                            ?name,
                            ?show_in_tree_view,
                            ?show_in_data_selector,
                            ?relationship,
                            ?duplication,
                            ?use_item_parent_id,
                            ?use_dedicated_table,
                            ?cascade_delete
                        ); SELECT LAST_INSERT_ID();";

            try
            {
                var dataTable = await clientDatabaseConnection.GetAsync(query);
                linkSettings.Id = Convert.ToInt32(dataTable.Rows[0][0]);
            }
            catch (MySqlException mySqlException)
            {
                if (mySqlException.Number == (int)MySqlErrorCode.DuplicateKeyEntry)
                {
                    return new ServiceResult<LinkSettingsModel>
                    {
                        StatusCode = HttpStatusCode.Conflict,
                        ErrorMessage = $"An entry already exists with {nameof(linkSettings.Type)} = '{linkSettings.Type}', {nameof(linkSettings.DestinationEntityType)} = '{linkSettings.DestinationEntityType}' and {nameof(linkSettings.SourceEntityType)} = '{linkSettings.SourceEntityType}'"
                    };
                }

                throw;
            }

            return new ServiceResult<LinkSettingsModel>(linkSettings);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> UpdateAsync(ClaimsIdentity identity, int id, LinkSettingsModel linkSettings)
        {
            if (linkSettings.Type <= 0 || String.IsNullOrWhiteSpace(linkSettings.DestinationEntityType) || String.IsNullOrWhiteSpace(linkSettings.SourceEntityType) || String.IsNullOrWhiteSpace(linkSettings.Name))
            {
                return new ServiceResult<bool>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = $"The properties '{nameof(linkSettings.Type)}', '{nameof(linkSettings.DestinationEntityType)}', '{nameof(linkSettings.SourceEntityType)}' and '{nameof(linkSettings.Name)}' need to contain a value."
                };
            }

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

            // First try to create the tables, if we have a dedicated table prefix.
            if (linkSettings.UseDedicatedTable)
            {
                var tablePrefix = $"{linkSettings.Type}_";

                if (!await databaseHelpersService.TableExistsAsync($"{tablePrefix}{WiserTableNames.WiserItemLink}"))
                {
                    await clientDatabaseConnection.ExecuteAsync($@"CREATE TABLE `{tablePrefix}{WiserTableNames.WiserItemLink}` LIKE `{WiserTableNames.WiserItemLink}`;
CREATE TABLE `{tablePrefix}{WiserTableNames.WiserItemLink}{WiserTableNames.ArchiveSuffix}` LIKE `{WiserTableNames.WiserItem}{WiserTableNames.ArchiveSuffix}`;
CREATE TABLE `{tablePrefix}{WiserTableNames.WiserItemLinkDetail}` LIKE `{WiserTableNames.WiserItemLinkDetail}`;
CREATE TABLE `{tablePrefix}{WiserTableNames.WiserItemLinkDetail}{WiserTableNames.ArchiveSuffix}` LIKE `{WiserTableNames.WiserItemLinkDetail}{WiserTableNames.ArchiveSuffix}`;
CREATE TABLE `{tablePrefix}{WiserTableNames.WiserItemFile}` LIKE `{WiserTableNames.WiserItemFile}`;
CREATE TABLE `{tablePrefix}{WiserTableNames.WiserItemFile}{WiserTableNames.ArchiveSuffix}` LIKE `{WiserTableNames.WiserItemFile}{WiserTableNames.ArchiveSuffix}`;");

                    var createTriggersQuery = await ResourceHelpers.ReadTextResourceFromAssemblyAsync("Api.Core.Queries.WiserInstallation.CreateDedicatedLinkTableTriggers.sql");
                    createTriggersQuery = createTriggersQuery.Replace("{LinkType}", linkSettings.Type.ToString());
                    await clientDatabaseConnection.ExecuteAsync(createTriggersQuery);
                }
            }

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", id);
            clientDatabaseConnection.AddParameter("type", linkSettings.Type);
            clientDatabaseConnection.AddParameter("destination_entity_type", linkSettings.DestinationEntityType);
            clientDatabaseConnection.AddParameter("connected_entity_type", linkSettings.SourceEntityType);
            clientDatabaseConnection.AddParameter("name", linkSettings.Name);
            clientDatabaseConnection.AddParameter("show_in_tree_view", linkSettings.ShowInTreeView);
            clientDatabaseConnection.AddParameter("show_in_data_selector", linkSettings.ShowInDataSelector);
            clientDatabaseConnection.AddParameter("relationship", ToDatabaseValue(linkSettings.Relationship));
            clientDatabaseConnection.AddParameter("duplication", ToDatabaseValue(linkSettings.DuplicationMethod));
            clientDatabaseConnection.AddParameter("use_item_parent_id", linkSettings.UseItemParentId);
            clientDatabaseConnection.AddParameter("cascade_delete", linkSettings.CascadeDelete);
            clientDatabaseConnection.AddParameter("use_dedicated_table", linkSettings.UseDedicatedTable);

            var query = $@"UPDATE {WiserTableNames.WiserLink}
                        SET type = ?type,
                            destination_entity_type = ?destination_entity_type,
                            connected_entity_type = ?connected_entity_type,
                            name = ?name,
                            show_in_tree_view = ?show_in_tree_view,
                            show_in_data_selector = ?show_in_data_selector,
                            relationship = ?relationship,
                            duplication = ?duplication,
                            use_item_parent_id = ?use_item_parent_id,
                            use_dedicated_table = ?use_dedicated_table,
                            cascade_delete = ?cascade_delete
                        WHERE id = ?id";

            try
            {
                await clientDatabaseConnection.ExecuteAsync(query);
            }
            catch (MySqlException mySqlException)
            {
                if (mySqlException.Number == (int)MySqlErrorCode.DuplicateKeyEntry)
                {
                    return new ServiceResult<bool>
                    {
                        StatusCode = HttpStatusCode.Conflict,
                        ErrorMessage = $"An entry already exists with {nameof(linkSettings.Type)} = '{linkSettings.Type}', {nameof(linkSettings.DestinationEntityType)} = '{linkSettings.DestinationEntityType}' and {nameof(linkSettings.SourceEntityType)} = '{linkSettings.SourceEntityType}'"
                    };
                }

                throw;
            }

            return new ServiceResult<bool>(true)
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> DeleteAsync(ClaimsIdentity identity, int id)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", id);

            var query = $"DELETE FROM {WiserTableNames.WiserLink} WHERE id = ?id";
            await clientDatabaseConnection.ExecuteAsync(query);
            return new ServiceResult<bool>(true)
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }

        private static string ToDatabaseValue(LinkDuplicationMethods value)
        {
            switch (value)
            {
                case LinkDuplicationMethods.None:
                    return "none";
                case LinkDuplicationMethods.CopyLink:
                    return "copy-link";
                case LinkDuplicationMethods.CopyItem:
                    return "copy-item";
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        private static string ToDatabaseValue(LinkRelationships value)
        {
            switch (value)
            {
                case LinkRelationships.OneToOne:
                    return "one-to-one";
                case LinkRelationships.OneToMany:
                    return "one-to-many";
                case LinkRelationships.ManyToMany:
                    return "many-to-many";
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
    }
}