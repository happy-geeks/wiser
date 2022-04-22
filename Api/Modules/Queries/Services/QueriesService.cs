using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Customers.Interfaces;
using Api.Modules.Queries.Interfaces;
using Api.Modules.Queries.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using MySql.Data.MySqlClient;

namespace Api.Modules.Queries.Services
{
    //TODO Verify comments
    /// <summary>
    /// Service for getting queries for the Wiser modules.
    /// </summary>
    public class QueriesService : IQueriesService, IScopedService
    {
        private readonly IWiserCustomersService wiserCustomersService;
        private readonly IDatabaseConnection clientDatabaseConnection;

        /// <summary>
        /// Creates a new instance of <see cref="QueriesService"/>.
        /// </summary>
        public QueriesService(IWiserCustomersService wiserCustomersService, IDatabaseConnection clientDatabaseConnection)
        {
            this.wiserCustomersService = wiserCustomersService;
            this.clientDatabaseConnection = clientDatabaseConnection;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<QueryModel>>> GetForExportModuleAsync(ClaimsIdentity identity)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT id, description
                                                            FROM {WiserTableNames.WiserQuery}
                                                            WHERE show_in_export_module = 1
                                                            ORDER BY description ASC");

            var results = new List<QueryModel>();
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<List<QueryModel>>(results);
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                results.Add(new QueryModel
                {
                    Id = dataRow.Field<int>("id"),
                    EncryptedId = await wiserCustomersService.EncryptValue(dataRow.Field<int>("id").ToString(), identity),
                    Description = dataRow.Field<string>("description")
                });
            }

            return new ServiceResult<List<QueryModel>>(results);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<QueryModel>>> GetAsync(ClaimsIdentity identity)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT id, description, query, show_in_export_module
                                                            FROM {WiserTableNames.WiserQuery}
                                                            ORDER BY id ASC");

            var results = new List<QueryModel>();
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<List<QueryModel>>(results);
            }

            foreach (DataRow dataRow in dataTable.Rows)
            {
                results.Add(new QueryModel
                {
                    Id = dataRow.Field<int>("id"),
                    EncryptedId = await wiserCustomersService.EncryptValue(dataRow.Field<int>("id").ToString(), identity),
                    Description = dataRow.Field<string>("description"),
                    Query = dataRow.Field<string>("query"),
                    ShowInExportModule = dataRow.Field<bool>("show_in_export_module"),
                });
            }

            return new ServiceResult<List<QueryModel>>(results);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<QueryModel>> GetAsync(ClaimsIdentity identity, int id)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", id);
            var query = $"SELECT id, description, query, show_in_export_module FROM {WiserTableNames.WiserQuery} WHERE id = ?id";
            var dataTable = await clientDatabaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<QueryModel>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = $"Wiser query with ID '{id}' does not exist.",
                    ReasonPhrase = $"Wiser query with ID '{id}' does not exist."
                };
            }
                
            var dataRow = dataTable.Rows[0];
            var showInExportModule = dataRow.Field<bool>("show_in_export_module");
            var result = new QueryModel()
            {
                Id = dataRow.Field<int>("id"),
                EncryptedId = await wiserCustomersService.EncryptValue(dataRow.Field<int>("id").ToString(), identity),
                Description = dataRow.Field<string>("description"),
                Query = dataRow.Field<string>("query"),
                ShowInExportModule = dataRow.Field<bool>("show_in_export_module")
            };

            return new ServiceResult<QueryModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<QueryModel>> CreateAsync(ClaimsIdentity identity, string description)
        {
            if (String.IsNullOrEmpty(description))
            {
                return new ServiceResult<QueryModel>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "'Description' must contain a value.",
                    ReasonPhrase = "'Description' must contain a value."
                };
            }

            var queryModel = new QueryModel
            {
                Description = description,
                Query = "",
                ShowInExportModule = false
            };

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("description", queryModel.Description);
            clientDatabaseConnection.AddParameter("query", queryModel.Query);
            clientDatabaseConnection.AddParameter("show_in_export_module", queryModel.ShowInExportModule);
            
            var query = $@"INSERT INTO {WiserTableNames.WiserQuery}
                        (
                            description,
                            query,
                            show_in_export_module
                        )
                        VALUES
                        (
                            ?description,
                            ?query,
                            ?show_in_export_module
                        ); SELECT LAST_INSERT_ID();";

            try
            {
                var dataTable = await clientDatabaseConnection.GetAsync(query);
                queryModel.Id = Convert.ToInt32(dataTable.Rows[0][0]);
            }
            catch (MySqlException mySqlException)
            {
                if (mySqlException.Number == (int)MySqlErrorCode.DuplicateKeyEntry)
                {
                    return new ServiceResult<QueryModel>
                    {
                        StatusCode = HttpStatusCode.Conflict,
                        ErrorMessage = $"An entry already exists with {nameof(queryModel.Query)} = '{queryModel.Query}', {nameof(queryModel.Description)} = '{queryModel.Description}' and {nameof(queryModel.ShowInExportModule)} = '{queryModel.ShowInExportModule}'",
                        ReasonPhrase = "And entry already exists with this data."
                    };
                }

                throw;
            }

            return new ServiceResult<QueryModel>(queryModel);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> UpdateAsync(ClaimsIdentity identity, QueryModel queryModel)
        {
            if (queryModel?.Description == null)
            {
                return new ServiceResult<bool>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "Either 'Query' or 'Description' must contain a value.",
                    ReasonPhrase = "Either 'Query' or 'Description' must contain a value."
                };
            }
            
            // Check if query exists.
            var queryResult = await GetAsync(identity, queryModel.Id);
            if (queryResult.StatusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<bool>
                {
                    ErrorMessage = queryResult.ErrorMessage,
                    ReasonPhrase = queryResult.ReasonPhrase,
                    StatusCode = queryResult.StatusCode
                };
            }

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", queryModel.Id);
            clientDatabaseConnection.AddParameter("description", queryModel.Description);
            clientDatabaseConnection.AddParameter("query", queryModel.Query);
            clientDatabaseConnection.AddParameter("show_in_export_module", queryModel.ShowInExportModule ? 1:0);

            var query = $@"UPDATE {WiserTableNames.WiserQuery}
                        SET description = ?description,
                            query = ?query,
                            show_in_export_module = ?show_in_export_module
                        WHERE id = ?id";

            await clientDatabaseConnection.ExecuteAsync(query);
            
            return new ServiceResult<bool>
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> DeleteAsync(ClaimsIdentity identity, int id)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            
            // Check if query exists.
            var queryResult = await GetAsync(identity, id);
            if (queryResult.StatusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<bool>
                {
                    ErrorMessage = queryResult.ErrorMessage,
                    ReasonPhrase = queryResult.ReasonPhrase,
                    StatusCode = queryResult.StatusCode
                };
            }

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", id);

            var query = $"DELETE FROM {WiserTableNames.WiserQuery} WHERE id = ?id";
            await clientDatabaseConnection.ExecuteAsync(query);

            return new ServiceResult<bool>
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }
    }
}
