using Api.Core.Services;
using Api.Modules.LinkSettings.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace Api.Modules.LinkSettings.Services
{
    /// <summary>
    /// Service for all CRUD operations for item link settings.
    /// </summary>
    public class LinkSettingsService : ILinkSettingsService, IScopedService
    {
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IWiserItemsService wiserItemsService;

        /// <summary>
        /// Creates a new instance of <see cref="LinkSettingsService"/>.
        /// </summary>
        public LinkSettingsService(IDatabaseConnection clientDatabaseConnection, IWiserItemsService wiserItemsService)
        {
            this.clientDatabaseConnection = clientDatabaseConnection;
            this.wiserItemsService = wiserItemsService;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<LinkSettingsModel>>> GetAsync(ClaimsIdentity identity)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            return new ServiceResult<List<LinkSettingsModel>>(await wiserItemsService.GetAllLinkTypeSettingsAsync());
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
