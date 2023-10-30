namespace Api.Core.Models
{
    /// <summary>
    /// Class for the constants of an Identity
    /// </summary>
    public class IdentityConstants
    {
        /// <summary>
        /// The key used to save the token identifier.
        /// </summary>
        public const string TokenIdentifierKey = "TokenIdentifier";

        /// <summary>
        /// The name of the administrator role.
        /// </summary>
        public const string AdministratorRole = "Admin";

        /// <summary>
        /// The name of the tenant role.
        /// </summary>
        public const string TenantRole = "Customer";

        /// <summary>
        /// The names of the tenant and admin roles.
        /// </summary>
        public const string TenantOrAdminRole = "Customer, Admin";

        /// <summary>
        /// The name for the admin accounts role.
        /// </summary>
        public const string AdminAccountRole = "WiserAdminAccount";

        /// <summary>
        /// The key for saving the admin account name.
        /// </summary>
        public const string AdminAccountName = " adminAccountName";
    }
}