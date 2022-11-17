namespace Api.Core.Models
{
    /// <summary>
    /// Class for Http Context Constants
    /// </summary>
    public class HttpContextConstants
    {
        /// <summary>
        /// The key for saving sub domain in the http context.
        /// </summary>
        public const string SubDomainKey = "subDomain";

        /// <summary>
        /// The key for saving the selected Wiser user, for when an admin account is logging in as someone else.
        /// </summary>
        public const string SelectedUserKey = "selectedUser";

        /// <summary>
        /// The key for indicating whether or not someone is logging in via a test environment.
        /// </summary>
        public const string IsTestEnvironmentKey = "isTestEnvironment";
    }
}
