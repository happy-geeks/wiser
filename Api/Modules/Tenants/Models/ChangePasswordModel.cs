using System.ComponentModel.DataAnnotations;

namespace Api.Modules.Tenants.Models
{
    /// <summary>
    /// A model with properties to change a password.
    /// </summary>
    public class ChangePasswordModel
    {
        /// <summary>
        /// The old password.
        /// </summary>
        [Required(ErrorMessage = "The field '{0}' is required.")]
        public string OldPassword { get; set; }

        /// <summary>
        /// The new password.
        /// </summary>
        public string NewPassword { get; set; }

        /// <summary>
        /// The new password for a second time, has to be the same as <see cref="NewPassword">NewPassword</see>.
        /// </summary>
        [Compare("NewPassword", ErrorMessage = "The passwords do not match.")]
        public string NewPasswordRepeat { get; set; }
    }
}