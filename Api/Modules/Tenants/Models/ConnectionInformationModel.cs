namespace Api.Modules.Tenants.Models
{
    /// <summary>
    /// A model with connection information. Can be used to store credentials to a database connection or an FTP connection or something similar.
    /// </summary>
    public class ConnectionInformationModel
    {
        /// <summary>
        /// Gets or sets the hostname / IP address.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the (encrypted) password.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the port number.
        /// </summary>
        public int PortNumber { get; set; }

        /// <summary>
        /// Gets or sets the name of the database. Only used for connections with a databases (obviously).
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the root folder. Only used for FTP connections.
        /// </summary>
        public string RootFolder { get; set; }
    }
}