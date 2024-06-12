using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Api.Core.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using IdentityServer4.Models;
using IdentityServer4.Stores;

namespace Api.Core.Services
{
    /// <inheritdoc cref="IDatabaseGrantsService"/>
    public class DatabaseGrantsService : IDatabaseGrantsService, IScopedService
    {
        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IDatabaseHelpersService databaseHelpersService;

        /// <summary>
        /// Creates a new instance of DatabaseGrantsService.
        /// </summary>
        public DatabaseGrantsService(IDatabaseConnection clientDatabaseConnection, IDatabaseHelpersService databaseHelpersService)
        {
            this.clientDatabaseConnection = clientDatabaseConnection;
            this.databaseHelpersService = databaseHelpersService;
        }

        /// <inheritdoc />
        public async Task<PersistedGrant> GetAsync(string key)
        {
            await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> { WiserTableNames.WiserGrantStore });
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("key", key);
            var query = $"SELECT `key`, `type`, `client_id`, `data`, `subject_id`, `description`, `creation_time`, `expiration`, `session_id` FROM {WiserTableNames.WiserGrantStore} WHERE `key` = ?key";
            var dataTable = await clientDatabaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return null;
            }

            var firstRow = dataTable.Rows[0];
            return new PersistedGrant
            {
                Key = key,
                SessionId = firstRow.Field<string>("session_id"),
                Type = firstRow.Field<string>("type"),
                ClientId = firstRow.Field<string>("client_id"),
                Data = firstRow.Field<string>("data"),
                CreationTime = firstRow.Field<DateTime>("creation_time"),
                Description = firstRow.Field<string>("description"),
                Expiration = firstRow.Field<DateTime>("expiration"),
                SubjectId = firstRow.Field<string>("subject_id")
            };
        }

        /// <inheritdoc />
        public async Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter)
        {
            await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> { WiserTableNames.WiserGrantStore });
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

            clientDatabaseConnection.ClearParameters();

            var query = $"SELECT `key`, `type`, `client_id`, `data`, `subject_id`, `description`, `creation_time`, `expiration`, `session_id` FROM {WiserTableNames.WiserGrantStore} {CreateFiltersForQuery(filter)}";
            var dataTable = await clientDatabaseConnection.GetAsync(query);

            return dataTable.Rows.Cast<DataRow>()
                .Select(dataRow => new PersistedGrant
                {
                    Key = dataRow.Field<string>("key"),
                    SessionId = dataRow.Field<string>("session_id"),
                    Type = dataRow.Field<string>("type"),
                    ClientId = dataRow.Field<string>("client_id"),
                    Data = dataRow.Field<string>("data"),
                    CreationTime = dataRow.Field<DateTime>("creation_time"),
                    Description = dataRow.Field<string>("description"),
                    Expiration = dataRow.Field<DateTime>("expiration"),
                    SubjectId = dataRow.Field<string>("subject_id")
                });
        }

        /// <inheritdoc />
        public async Task CreateAsync(PersistedGrant grant)
        {
            await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> { WiserTableNames.WiserGrantStore });
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("key", grant.Key);
            clientDatabaseConnection.AddParameter("type", grant.Type);
            clientDatabaseConnection.AddParameter("client_id", grant.ClientId);
            clientDatabaseConnection.AddParameter("data", grant.Data);
            clientDatabaseConnection.AddParameter("subject_id", grant.SubjectId);
            clientDatabaseConnection.AddParameter("description", grant.Description);
            clientDatabaseConnection.AddParameter("creation_time", grant.CreationTime);
            clientDatabaseConnection.AddParameter("expiration", grant.Expiration);
            clientDatabaseConnection.AddParameter("session_id", grant.SessionId);

            var query = @$"INSERT INTO {WiserTableNames.WiserGrantStore} (`key`, `type`, `client_id`, `data`, `subject_id`, `description`, `creation_time`, `expiration`, `session_id`)
                        VALUES (?key, ?type, ?client_id, ?data, ?subject_id, ?description, ?creation_time, ?expiration, ?session_id)";
            await clientDatabaseConnection.ExecuteAsync(query);
        }

        /// <inheritdoc />
        public async Task UpdateAsync(string key, PersistedGrant grant)
        {
            await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> { WiserTableNames.WiserGrantStore });
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("key", grant.Key);
            clientDatabaseConnection.AddParameter("type", grant.Type);
            clientDatabaseConnection.AddParameter("client_id", grant.ClientId);
            clientDatabaseConnection.AddParameter("data", grant.Data);
            clientDatabaseConnection.AddParameter("subject_id", grant.SubjectId);
            clientDatabaseConnection.AddParameter("description", grant.Description);
            clientDatabaseConnection.AddParameter("creation_time", grant.CreationTime);
            clientDatabaseConnection.AddParameter("expiration", grant.Expiration);
            clientDatabaseConnection.AddParameter("session_id", grant.SessionId);

            var query = @$"UPDATE {WiserTableNames.WiserGrantStore} 
                        SET `type` = ?type, `client_id` = ?client_id, `data` = ?data, `subject_id` = ?subject_id, `description` = ?description, `creation_time` = ?creation_time, `expiration` = ?expiration, `session_id` = ?session_id
                        WHERE `key` = ?key";
            await clientDatabaseConnection.ExecuteAsync(query);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string key)
        {
            await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> { WiserTableNames.WiserGrantStore });
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("key", key);
            var query = $"DELETE FROM {WiserTableNames.WiserGrantStore} WHERE `key` = ?key";
            await clientDatabaseConnection.ExecuteAsync(query);
        }

        /// <inheritdoc />
        public async Task DeleteAllAsync(PersistedGrantFilter filter)
        {
            await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> { WiserTableNames.WiserGrantStore });
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

            clientDatabaseConnection.ClearParameters();

            var query = $@"DELETE FROM {WiserTableNames.WiserGrantStore} {CreateFiltersForQuery(filter)}";
            await clientDatabaseConnection.ExecuteAsync(query);
        }

        private string CreateFiltersForQuery(PersistedGrantFilter filter)
        {
            var whereClause = new List<string>();
            if (!String.IsNullOrWhiteSpace(filter?.SessionId))
            {
                clientDatabaseConnection.AddParameter("session_id", filter.SessionId);
                whereClause.Add("`session_id` = ?session_id");
            }

            if (!String.IsNullOrWhiteSpace(filter?.ClientId))
            {
                clientDatabaseConnection.AddParameter("client_id", filter.ClientId);
                whereClause.Add("`client_id` = ?client_id");
            }

            if (!String.IsNullOrWhiteSpace(filter?.SubjectId))
            {
                clientDatabaseConnection.AddParameter("subject_id", filter.SubjectId);
                whereClause.Add("`subject_id` = ?subject_id");
            }

            if (!String.IsNullOrWhiteSpace(filter?.Type))
            {
                clientDatabaseConnection.AddParameter("type", filter.Type);
                whereClause.Add("`type` = ?type");
            }

            return !whereClause.Any() ? "" : $"WHERE {String.Join(" AND ", whereClause)}";
        }
    }
}