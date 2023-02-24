namespace Api.Modules.DigitalOcean.Models
{
    /// <inheritdoc />
    public class ExtendedDatabaseApiModel : DatabaseApiModel
    {
        /// <summary>
        /// Connection to the database
        /// </summary>
        public DatabaseConnectionApiModel Connection { get; set; }
    }
}
