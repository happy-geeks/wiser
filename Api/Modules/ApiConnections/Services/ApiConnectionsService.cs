using System;
using System.Data;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Interfaces;
using Api.Core.Services;
using Api.Modules.ApiConnections.Interfaces;
using Api.Modules.ApiConnections.Models;
using Api.Modules.Tenants.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using Newtonsoft.Json.Linq;

namespace Api.Modules.ApiConnections.Services;

/// <inheritdoc cref="IApiConnectionsService" />
public class ApiConnectionsService : IApiConnectionsService, IScopedService
{
    private readonly IWiserTenantsService wiserTenantsService;
    private readonly IDatabaseConnection clientDatabaseConnection;
    private readonly IJsonService jsonService;

    /// <summary>
    /// Creates a new instance of <see cref="ApiConnectionsService"/>.
    /// </summary>
    public ApiConnectionsService(IWiserTenantsService wiserTenantsService, IDatabaseConnection databaseConnection, IJsonService jsonService)
    {
        this.wiserTenantsService = wiserTenantsService;
        this.clientDatabaseConnection = databaseConnection;
        this.jsonService = jsonService;
    }

    /// <inheritdoc />
    public async Task<ServiceResult<ApiConnectionModel>> GetSettingsAsync(ClaimsIdentity identity, int id)
    {
        await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
        clientDatabaseConnection.AddParameter("id", id);
        var query = $"SELECT id, options, authentication_data FROM {WiserTableNames.WiserApiConnection} WHERE id = ?id";

        var dataTable = await clientDatabaseConnection.GetAsync(query);
        if (dataTable.Rows.Count == 0)
        {
            return new ServiceResult<ApiConnectionModel>
            {
                StatusCode = HttpStatusCode.NotFound,
                ErrorMessage = $"API connection with id '{id}' does not exist"
            };
        }
        
        var firstRow = dataTable.Rows[0];
        var optionsString = firstRow.Field<string>("options");
        var authenticationDataString = firstRow.Field<string>("authentication_data");

        var result = new ApiConnectionModel
        {
            Id = id,
            Options = String.IsNullOrWhiteSpace(optionsString) ? new JObject() : JToken.Parse(optionsString),
            AuthenticationData = String.IsNullOrWhiteSpace(authenticationDataString) ? new JObject() : JToken.Parse(authenticationDataString)
        };
            
        var tenant = await wiserTenantsService.GetSingleAsync(identity);
        jsonService.EncryptValuesInJson(result.Options, tenant.ModelObject.EncryptionKey);
        jsonService.EncryptValuesInJson(result.AuthenticationData, tenant.ModelObject.EncryptionKey);

        return new ServiceResult<ApiConnectionModel>(result);
    }
}