using System.ComponentModel.DataAnnotations;
using System.Data;
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
        [Key, JsonIgnore]
        public int Id { get; set; }

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
        /// Convert a <see cref="DataRow"/> to an <see cref="AdminAccountModel"/>.
        /// </summary>
        /// <param name="dataRow">The <see cref="DataRow"/> to convert.</param>
        /// <returns></returns>
        public static AdminAccountModel FromDataRow(DataRow dataRow)
        {
            return new AdminAccountModel
            {
                Id = dataRow.Field<int>("id"),
                Login = dataRow.Field<string>("login"),
                Name = dataRow.Field<string>("employee"),
                Active = dataRow.Field<bool>("active")
            };
        }
    }
}