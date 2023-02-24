namespace Api.Modules.DigitalOcean.Models
{
    /// <summary>
    /// Class that describes an object which has a response from a database
    /// </summary>
    public class GetDatabaseResponseModel
    {
        /// <summary>
        /// An object of the database that should be returned
        /// </summary>
        public ExtendedDatabaseApiModel Database { get; set; }
    }
}
