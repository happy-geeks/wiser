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

        /// <summary>
        /// The key for the TOTP PIN that the user needs to enter when 2FA is enabled.
        /// </summary>
        public const string TotpPinKey = "totpPin";

        /// <summary>
        /// The key for the TOTP backup code that the user needs to enter when 2FA is enabled and they lost access to their authenticator app.
        /// </summary>
        public const string TotpBackupCodeKey = "totpBackupCode";

        /// <summary>
        /// The key that contains an encrypted true/false value that indicates whether or not the user is logging in from the front end.
        /// </summary>
        public const string IsWiserFrontEndLoginKey = "isWiserFrontEndLogin";
    }
}