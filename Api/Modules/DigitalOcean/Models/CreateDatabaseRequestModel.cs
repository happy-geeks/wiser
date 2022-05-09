namespace Api.Modules.DigitalOcean.Models
{
    public class CreateDatabaseRequestModel
    {
        public string DatabaseCluster { get; set; }
        public string Database { get; set; }
        public string User { get; set; }
    }
}
