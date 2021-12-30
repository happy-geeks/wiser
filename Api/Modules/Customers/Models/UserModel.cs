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
        /// Gets or sets the unique internal ID of this item.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the ID for Wiser 2.0.
        /// </summary>
        public ulong Wiser2Id { get; set; }

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
        /// Gets or sets the type of user.
        /// </summary>
        public string Type { get; set; }

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
        /// Gets or sets custom settings for this user.
        /// </summary>
        public string UserSettings { get; set; }

        /// <summary>
        /// Gets or sets custom settings for this user.
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, string> Settings 
        {
            get
            {
                if (String.IsNullOrWhiteSpace(UserSettings))
                {
                    return new Dictionary<string, string>();
                }

                var array = UserSettings.Replace("\r\n", "\n").Split('\r', '\n');
                var result = new Dictionary<string, string>();
                foreach (var property in array)
                {
                    var separatorIndex = property.IndexOf('=');
                    if (separatorIndex < 0)
                    {
                        result.Add(property, String.Empty);
                        continue;
                    }

                    var key = property.Substring(0, separatorIndex);
                    var value = property.Substring(separatorIndex + 1);
                    result.Add(key, value);
                }

                return result;
            }
        }

        /// <summary>
        /// Gets or sets the folder restriction.
        /// When this property contains a value, this user can only see root folders with this name.
        /// This works in (almost) all modules.
        /// </summary>
        public string FolderRestriction { get; set; }

        /// <summary>
        /// Gets or sets whether module statuses should be synchronized.
        /// </summary>
        public bool ModuleSyncStatusEnabled { get; set; }

        /// <summary>
        /// Gets or sets the XML with module settings.
        /// </summary>
        public string ModuleSettingsXml { get; set; }

        /// <summary>
        /// Gets or sets whether this user is active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Gets or sets data for Google Authentication.
        /// </summary>
        public GoogleAuthenticationModel GoogleAuthentication { get; set; }

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

        /// <summary>
        /// Convert a <see cref="DataRow"/> to an <see cref="UserModel"/>.
        /// </summary>
        /// <param name="dataRow">The <see cref="DataRow"/> to convert.</param>
        /// <returns></returns>
        public static UserModel FromDataRow(DataRow dataRow)
        {
            var result = new UserModel
            {
                Id = dataRow.Field<int>("id"),
                Name = dataRow.Field<string>("name"),
                CustomerId = dataRow.Field<int>("customerid"),
                Username = dataRow.Field<string>("login"),
                Password = dataRow.Field<string>("pass"),
                EmailAddress = !dataRow.Table.Columns.Contains("emailaddress") ? null : dataRow.Field<string>("emailaddress"),
                LastLoginDate = !dataRow.Table.Columns.Contains("lastlogin") ? null : dataRow.Field<DateTime?>("lastlogin"),
                LastLoginIpAddress = !dataRow.Table.Columns.Contains("lastloginip") ? null : dataRow.Field<string>("lastloginip"),
                FolderRestriction = !dataRow.Table.Columns.Contains("folderrestriction") ? null : dataRow.Field<string>("folderrestriction"),
                Type = !dataRow.Table.Columns.Contains("usertype") ? null : dataRow.Field<string>("usertype"),
                ModuleSyncStatusEnabled = dataRow.Table.Columns.Contains("modulesyncstatus") && Convert.ToBoolean(dataRow["modulesyncstatus"]),
                ModuleSettingsXml = !dataRow.Table.Columns.Contains("modulesettings") ? null : dataRow.Field<string>("modulesettings"),
                Active = !dataRow.Table.Columns.Contains("active") || Convert.ToBoolean(dataRow["active"]),
                UserSettings = !dataRow.Table.Columns.Contains("usersettings") ? null : dataRow.Field<string>("usersettings")
            };
            
            return result;
        }
    }
}