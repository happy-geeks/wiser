using System.Threading.Tasks;
using Api.Modules.DigitalOcean.Models;

namespace Api.Modules.DigitalOcean.Interfaces
{
    public interface IDigitalOceanService
    {
        string AuthorizationRedirect();
        Task<string> ProcessCallbackAsync(string code);
        Task<GetDatabasesResponseModel> DatabaseListAsync(string accessToken);
        Task<CreateDatabaseApiResponseModel> CreateDatabaseAsync(string databaseCluster, string database, string user, string accessToken);
        Task<bool> RestrictMysqlUserToDbAsync(CreateDatabaseApiResponseModel databaseInfo, string accessToken);
    }
}
