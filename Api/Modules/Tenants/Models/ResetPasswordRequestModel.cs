namespace Api.Modules.Tenants.Models
{
    /// <summary>
    /// A model to request a password reset.
    /// </summary>
    public class ResetPasswordRequestModel
    {
        /// <summary>
        /// Gets or sets the username of the user that forgot their password.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the e-mail address of the user that forgot their password.
        /// </summary>
        public string EmailAddress { get; set; }

        /// <summary>
        /// Optional: Gets or sets the language code of the language used in Wiser. Default is "nl".
        /// </summary>
        public string LanguageCode { get; set; } = "nl";

        /// <summary>
        /// Gets or sets the Wiser sub domain used to access the site.
        /// </summary>
        public string SubDomain { get; set; }
    }
}
