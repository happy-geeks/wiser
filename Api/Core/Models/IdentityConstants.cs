namespace Api.Core.Models
{
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
        /// The name of the customer role.
        /// </summary>
        public const string CustomerRole = "Customer";

        /// <summary>
        /// The names of the customer and admin roles.
        /// </summary>
        public const string CustomerOrAdminRole = "Customer, Admin";

        /// <summary>
        /// The name for the admin accounts role.
        /// </summary>
        public const string AdminAccountRole = "WiserAdminAccount";
    }
}