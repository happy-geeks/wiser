namespace Api.Modules.Tenants.Models
{
    /// <summary>
    /// A model for validating authentication cookies / tokens.
    /// </summary>
    public class ValidateCookieModel
    {
        /// <summary>
        /// Gets or sets whether the authentication was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the value or (error) message.
        /// </summary>
        public string MessageOrValue { get; set; } 

        /// <summary>
        /// Gets or sets the data of the authenticated user.
        /// </summary>
        public UserModel UserData { get; set; }
    }
}