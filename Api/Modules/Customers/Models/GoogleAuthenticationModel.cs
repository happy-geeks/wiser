namespace Api.Modules.Customers.Models
{
    /// <summary>
    /// A mode for using Google Authentication in Wiser.
    /// </summary>
    public class GoogleAuthenticationModel
    {
        /// <summary>
        /// Gets or sets whether Google Authentication still needs to be setup for the user.
        /// </summary>
        public bool SetupGoogleAuthentication { get; set; }

        /// <summary>
        /// Gets or sets whether to use Google Authentication for the user.
        /// </summary>
        public bool UseGoogleAuthentication { get; set; }

        /// <summary>
        /// Gets or sets whether Google Authentication succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the URL for the QR code image.
        /// </summary>
        public string QrImageUrl { get; set; }

        /// <summary>
        /// Gets or sets the secure ID.
        /// </summary>
        public string SecureId { get; set; }

        /// <summary>
        /// Gets or sets the value that should be saved in a "Remember me" cookie.
        /// </summary>
        public string CookieValue { get; set; }
    }
}