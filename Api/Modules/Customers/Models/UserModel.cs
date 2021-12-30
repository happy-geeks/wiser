using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using Newtonsoft.Json;

namespace Api.Modules.Customers.Models
{
    /// <summary>
    /// A Wiser user.
    /// </summary>
    public class UserModel
    {
        /// <summary>
        /// Gets or sets the unique ID of this user.
        /// </summary>
        [Key]
        public ulong Id { get; set; }
        
        /// <summary>
        /// Gets or sets the encrypted ID for Wiser 2.0.
        /// </summary>
        public string EncryptedId { get; set; }

        /// <summary>
        /// Gets or sets the full name.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the customer where this user belongs to.
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        ///  Gets or sets the encrypted customer if for Wiser 2.0.
        /// </summary>
        public string EncryptedCustomerId { get; set; }
        
        /// <summary>
        /// Gets or sets the password. 
        /// Will not be serialized to the output, only for internal usage.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the username (for logging in).
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the e-mail address.
        /// </summary>
        public string EmailAddress { get; set; }
        
        /// <summary>
        /// Gets or sets the date and time of the user's last successful login attempt.
        /// </summary>
        public DateTime? LastLoginDate { get; set; }

        /// <summary>
        /// Gets or sets the IP address the used used during it's last successful login attempt.
        /// </summary>
        public string LastLoginIpAddress { get; set; }

        /// <summary>
        /// Gets or sets the 
        /// </summary>
        public CustomerModel Customer { get; set; }
        
        /// <summary>
        /// Gets or sets the value that should be saved in a "Remember me" cookie. This will only contain a value if returned by the login method.
        /// </summary>
        public string CookieValue { get; set; }

        /// <summary>
        /// Gets or sets whether the user has to change their password the next time they login.
        /// </summary>
        public bool RequirePasswordChange { get; set; }

        /// <summary>
        /// Gets or sets the role of the user.
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Gets or sets the number 0, encrypted with the user's encryption ID.
        /// This is needed for some Wiser API functions.
        /// </summary>
        public string ZeroEncrypted { get; set; }

        /// <summary>
        /// Gets or sets the (encrypted) ID of the root directory for the general file uploader.
        /// </summary>
        public string FilesRootId { get; set; }

        /// <summary>
        /// Gets or sets the (encrypted) ID of the root directory for the general image uploader.
        /// </summary>
        public string ImagesRootId { get; set; }

        /// <summary>
        /// Gets or sets the (encrypted) ID of the root directory for the general template uploader.
        /// </summary>
        public string TemplatesRootId { get; set; }

        /// <summary>
        /// Gets or sets the main domain. This is used for generating URLs for images, files etc in HTML editors.
        /// </summary>
        public string MainDomain { get; set; }
    }
}