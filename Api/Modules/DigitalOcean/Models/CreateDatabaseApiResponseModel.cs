namespace Api.Modules.DigitalOcean.Models
{
    public class CreateDatabaseApiResponseModel : PostDatabaseUserResponseModel
    {
        public string Database { get; set; }
        public GetDatabaseResponseModel Cluster { get; set; }
    }
}
