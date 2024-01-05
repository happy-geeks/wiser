namespace Api.Modules.Tenants.Models
{
    /// <summary>
    /// Information about the authenticated user.
    /// This is used for some controller methods where it's not possible to authenticate via OAUTH2, such as an URL to a file.
    /// </summary>
    public class TenantInformationModel
    {
        /// <summary>
        /// Gets or sets the encrypted ID of the tenant the user belongs to.
        /// </summary>
        public string encryptedTenantId { get; set; }

        /// <summary>
        /// Gets or sets the encrypted ID the authenticated user.
        /// </summary>
        public string encryptedUserId { get; set; }

        /// <summary>
        /// Gets or sets the name of the user.
        /// </summary>
        public string username { get; set; }

        /// <summary>
        /// Gets or sets the e-mail address.
        /// </summary>
        public string userEmailAddress { get; set; }

        /// <summary>
        /// Gets or sets the type of user. 
        /// </summary>
        public string userType { get; set; }

        /// <summary>
        /// Gets or sets whether this is a test environment.
        /// </summary>
        public bool isTest { get; set; }

        /// <summary>
        /// Gets or sets the sub domain that is being used for Wiser.
        /// </summary>
        public string subDomain { get; set; }
    }
}