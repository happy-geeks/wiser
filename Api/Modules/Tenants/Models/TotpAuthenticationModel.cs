using Newtonsoft.Json;

namespace Api.Modules.Tenants.Models
{
    /// <summary>
    /// A mode for using time-based one-time password authentication in Wiser.
    /// </summary>
    public class TotpAuthenticationModel
    {
        /// <summary>
        /// Gets or sets whether Google Authentication still needs to be setup for the user.
        /// </summary>
        public bool RequiresSetup { get; set; }

        /// <summary>
        /// Gets or sets whether to use Google Authentication for the user.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the URL for the QR code image.
        /// </summary>
        public string QrImageUrl { get; set; }

        /// <summary>
        /// Gets or sets the secure ID.
        /// </summary>
        [JsonIgnore]
        public string SecretKey { get; set; }
    }
}