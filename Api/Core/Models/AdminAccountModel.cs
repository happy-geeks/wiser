using System;
using System.ComponentModel.DataAnnotations;
using System.Data;
using Api.Modules.Tenants.Models;
using Newtonsoft.Json;

namespace Api.Core.Models
{
    /// <summary>
    /// A model for an admin account.
    /// </summary>
    public class AdminAccountModel
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        [Key]
        [JsonIgnore]
        public ulong Id { get; set; }

        /// <summary>
        /// Gets or sets the encrypted ID.
        /// </summary>
        public string EncryptedId { get; set; }

        /// <summary>
        /// Gets or sets the login name / e-mail address.
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets whether this admin account is active and should be allowed to login.
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets the TOTP settings (2FA).
        /// </summary>
        public TotpAuthenticationModel TotpAuthentication { get; set; }

        /// <summary>
        /// Convert a <see cref="DataRow"/> to an <see cref="AdminAccountModel"/>.
        /// </summary>
        /// <param name="dataRow">The <see cref="DataRow"/> to convert.</param>
        /// <returns></returns>
        public static AdminAccountModel FromDataRow(DataRow dataRow)
        {
            return new AdminAccountModel
            {
                Id = dataRow.Field<ulong>("id"),
                Login = dataRow.Field<string>("login"),
                Name = dataRow.Field<string>("name"),
                Active = Convert.ToBoolean(dataRow["active"]),
                TotpAuthentication = new TotpAuthenticationModel
                {
                    RequiresSetup = Convert.ToBoolean(dataRow["totpRequiresSetup"]),
                    Enabled = Convert.ToBoolean(dataRow["totpEnabled"]),
                    SecretKey = dataRow.Field<string>("totpSecret")
                }
            };
        }
    }
}