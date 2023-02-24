namespace Api.Modules.DigitalOcean.Models
{
    /// <inheritdoc />
    public class CreateDatabaseApiResponseModel : PostDatabaseUserResponseModel
    {
        /// <summary>
        /// Name of the database
        /// </summary>
        public string Database { get; set; }
        
        /// <summary>
        /// The cluster of the database
        /// </summary>
        public GetDatabaseResponseModel Cluster { get; set; }
    }
}
