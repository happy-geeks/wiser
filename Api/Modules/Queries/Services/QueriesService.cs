using System.Collections.Generic;
using System.Data;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.Customers.Interfaces;
using Api.Modules.Queries.Interfaces;
using Api.Modules.Queries.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;

namespace Api.Modules.Queries.Services
{
    //TODO Verify comments
    /// <summary>
    /// Service for getting queries for the Wiser export module.
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
    }
}
