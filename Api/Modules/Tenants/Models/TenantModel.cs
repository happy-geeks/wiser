using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace Api.Modules.Tenants.Models
{
    /// <summary>
    /// A tenant that uses Wiser.
    /// </summary>
    public class TenantModel
    {
        /// <summary>
        /// Gets or sets the unique internal ID of this item.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the tenant ID. In most cases this is the same as <see cref="Id"/>, but there are exception.
        /// This is a separate database column that can be used to enter the ID of another tenant, if you copied the database of that tenant,
        /// because that ID is saved in several tables, which you don't have to change then.
        /// </summary>
        public int TenantId { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the information required to connect to the database of the tenant.
        /// </summary>
        public ConnectionInformationModel Database { get; set; }
        
        /// <summary>
        /// Gets or sets the encryption key for Wiser data.
        /// </summary>
        public string EncryptionKey { get; set; }

        /// <summary>
        /// Gets or sets the sub domain for Wiser.
        /// </summary>
        public string SubDomain { get; set; }

        /// <summary>
        /// Gets or sets the title to show in the browser tab when the user opens Wiser for this tenant/tenant.
        /// </summary>
        public string WiserTitle { get; set; }

        /// <summary>
        /// Gets or sets extra Wiser settings, for easy_objects.
        /// This is only used for creating a new tenant via Wiser 3.
        /// </summary>
        public Dictionary<string, string> WiserSettings { get; set; } = new();

        /// <summary>
        /// Convert a <see cref="DataRow"/> to an <see cref="TenantModel"/>.
        /// </summary>
        /// <param name="dataRow">The <see cref="DataRow"/> to convert.</param>
        /// <returns></returns>
        public static TenantModel FromDataRow(DataRow dataRow)
        {
            var result = new TenantModel
            {
                Id = dataRow.Field<int>("id"),
                TenantId = dataRow.Field<int>("customerid"),
                Name = dataRow.Field<string>("name"),
                EncryptionKey = dataRow.Field<string>("encryption_key"),
                SubDomain = dataRow.Field<string>("subdomain"),
                WiserTitle = dataRow.Field<string>("wiser_title")
            };

            if (dataRow.Table.Columns.Contains("db_host"))
            {
                result.Database = new ConnectionInformationModel
                {
                    Password = dataRow.Field<string>("db_passencrypted"),
                    Username = dataRow.Field<string>("db_login"),
                    PortNumber = String.IsNullOrWhiteSpace(dataRow.Field<string>("db_port")) ? 3306 : Convert.ToInt32(dataRow.Field<string>("db_port")),
                    Host = dataRow.Field<string>("db_host"),
                    DatabaseName = dataRow.Field<string>("db_dbname")
                };
            }

            return result;
        }
    }
}