using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Modules.DigitalOcean.Interfaces;
using Api.Modules.DigitalOcean.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using RestSharp;

namespace Api.Modules.DigitalOcean.Services
{
    /// <inheritdoc cref="IDigitalOceanService" />
    public class DigitalOceanService : IDigitalOceanService, ITransientService
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly DigitalOceanSettings digitalOceanSettings;

        /// <summary>
        /// Creates a new instance of <see cref="DigitalOceanService"/>.
        /// </summary>
        public DigitalOceanService(IHttpContextAccessor httpContextAccessor, IOptions<DigitalOceanSettings> digitalOceanSettings)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.digitalOceanSettings = digitalOceanSettings.Value;
        }

        /// <inheritdoc />
        public string AuthorizationRedirect()
        {
            var callBackUrl = $"{httpContextAccessor.HttpContext.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host.Value}/api/v3/digital-ocean/callback";
            return $"https://cloud.digitalocean.com/v1/oauth/authorize?client_id={Uri.EscapeDataString(digitalOceanSettings.ClientId)}&redirect_uri={Uri.EscapeUriString(callBackUrl)}&response_type=code";
        }

        /// <inheritdoc />
        public async Task<string> ProcessCallbackAsync(string code)
        {
            var callBackUrl = $"{httpContextAccessor.HttpContext.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host.Value}/api/v3/digital-ocean/callback";
            var tokenUri = $"https://cloud.digitalocean.com/v1/oauth/token?client_id={Uri.EscapeDataString(digitalOceanSettings.ClientId)}&grant_type=authorization_code&code={Uri.EscapeDataString(code)}&client_secret={Uri.EscapeDataString(digitalOceanSettings.ClientSecret)}&redirect_uri={Uri.EscapeUriString(callBackUrl)}";
            
            var restClient = new RestClient(tokenUri);
            var restRequest = new RestRequest(Method.Post);
            var response = await restClient.ExecuteAsync(restRequest);

            return response.Content;
        }

        /// <inheritdoc />
        public async Task<GetDatabasesResponseModel> DatabaseListAsync(string accessToken)
        {
            var apiResponse = await ProcessRequestAsync(Method.Get, "databases", accessToken);
            return JsonConvert.DeserializeObject<GetDatabasesResponseModel>(apiResponse);
        }

        /// <inheritdoc />
        public async Task<CreateDatabaseApiResponseModel> CreateDatabaseAsync(string databaseCluster, string database, string user, string accessToken)
        {
            if (user.Length > 10)
            {
                user = user[..10];
            }

            var databaseInfo = new CreateDatabaseApiResponseModel
            {
                Users = new List<UserApiModel>
                {
                    new() { Name = $"{user.ToMySqlSafeValue(false)}_web", Password = SecurityHelpers.GenerateRandomPassword(), Role = "normal" },
                    new() { Name = $"{user.ToMySqlSafeValue(false)}_ais", Password = SecurityHelpers.GenerateRandomPassword(), Role = "normal" },
                    new() { Name = $"{user.ToMySqlSafeValue(false)}_wiser", Password = SecurityHelpers.GenerateRandomPassword(), Role = "normal" }
                },
                Database = database
            };

            //Retrieve the cluster
            var apiResponse = await ProcessRequestAsync(Method.Get, $"databases/{databaseCluster}", accessToken);
            var clusterInfo = JsonConvert.DeserializeObject<GetDatabaseResponseModel>(apiResponse);
            databaseInfo.Cluster = clusterInfo;

            var connInfo = clusterInfo.Database.Connection;
            await using var mysqlConnection = new MySqlConnection($"server={connInfo.Host};port={connInfo.Port};uid={connInfo.User};pwd={connInfo.Password};sslmode=REQUIRED");
            await mysqlConnection.OpenAsync();
            await using var query = new MySqlCommand($"CREATE DATABASE {databaseInfo.Database}", mysqlConnection);
            var response = await query.ExecuteScalarAsync();
            Console.WriteLine(JsonConvert.SerializeObject(response));

            foreach (var databaseUser in databaseInfo.Users)
            {
                await using var query2 = new MySqlCommand($"CREATE USER `{databaseUser.Name}`@`%` IDENTIFIED WITH mysql_native_password BY '{databaseUser.Password}'", mysqlConnection);
                var response2 = await query2.ExecuteScalarAsync();
                Console.WriteLine(JsonConvert.SerializeObject(response2));
            }

            await mysqlConnection.CloseAsync();

            return databaseInfo;
        }

        /// <inheritdoc />
        public async Task<bool> RestrictMysqlUserToDbAsync(CreateDatabaseApiResponseModel databaseInfo, string accessToken)
        {
            var connInfo = databaseInfo.Cluster.Database.Connection;
            //Do MYSQL query here
            await using var mysqlConnection = new MySqlConnection($"server={connInfo.Host};port={connInfo.Port};uid={connInfo.User};pwd={connInfo.Password};database={databaseInfo.Database};sslmode=REQUIRED");
            mysqlConnection.Open();
            foreach (var user in databaseInfo.Users)
            {
                await using var query = new MySqlCommand($"GRANT ALL PRIVILEGES ON {databaseInfo.Database}.* TO '{user.Name}'@'%'", mysqlConnection);
                var response = await query.ExecuteScalarAsync();
                Console.WriteLine(JsonConvert.SerializeObject(response));
            }

            await mysqlConnection.CloseAsync();
            return true;
        }

        private async Task<string> ProcessRequestAsync(Method method, string endpoint, string accessToken, object body = null)
        {
            var restClient = new RestClient("https://api.digitalocean.com/v2/");
            var restRequest = new RestRequest(endpoint, method);
            restRequest.AddHeader("Authorization", $"Bearer {accessToken}");

            if (body != null)
            {
                restRequest.AddJsonBody(body);
            }
            
            var response = await restClient.ExecuteAsync(restRequest);

            return response.Content;
        }
    }
}