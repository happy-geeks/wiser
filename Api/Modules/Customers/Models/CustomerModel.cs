using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace Api.Modules.Customers.Models
{
    /// <summary>
    /// A customer that uses Wiser.
    /// </summary>
    public class CustomerModel
    {
        /// <summary>
        /// Gets or sets the unique internal ID of this item.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the customer ID. In most cases this is the same as <see cref="Id"/>, but there are exception.
        /// This is a separate database column that can be used to enter the ID of another customer, if you copied the database of that customer,
        /// because that ID is saved in several tables, which you don't have to change then.
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the main e-mail address.
        /// </summary>
        public string EmailAddress { get; set; }

        /// <summary>
        /// </summary>
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the information required to connecto to the live database of the customer.
        /// </summary>
        public ConnectionInformationModel LiveDatabase { get; set; }

        /// <summary>
        /// Gets or sets the information required to connecto to the test database of the customer.
        /// </summary>
        public ConnectionInformationModel TestDatabase { get; set; }

        /// <summary>
        /// Gets or sets the information required to connecto to the FTP server of the customer.
        /// </summary>
        public ConnectionInformationModel Ftp { get; set; }

        /// <summary>
        /// Gets or sets the start date.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Gets or sets the end date.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Gets or sets the notes.
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Gets or sets whether Google Authentication has been enabled for this customer.
        /// </summary>
        public bool GoogleAuthenticationEnabled { get; set; }

        /// <summary>
        /// Gets or sets the backup order number.
        /// </summary>
        public int BackupOrderNumber { get; set; }

        /// <summary>
        /// Gets or sets the URL for the instructions manual of Wiser, if there is a custom manual for this customer.
        /// </summary>
        public string InstructionsUrl { get; set; }

        /// <summary>
        /// Gets or sets the users of this customer.
        /// </summary>
        public List<UserModel> Users { get; set; }
        
        /// <summary>
        /// Gets or sets the encryption key for Wiser 2 data.
        /// </summary>
        public string EncryptionKey { get; set; }

        /// <summary>
        /// Gets or sets the sub domain for Wiser 2+.
        /// </summary>
        public string SubDomain { get; set; }

        /// <summary>
        /// Gets or sets the host to use for sending mails.
        /// </summary>
        public string MailHost { get; set; }

        /// <summary>
        /// Gets or sets extra Wiser settings, for easy_objects.
        /// This is only used for creating a new customer via Wiser 2.1.
        /// </summary>
        public Dictionary<string, string> WiserSettings { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Convert a <see cref="DataRow"/> to an <see cref="CustomerModel"/>.
        /// </summary>
        /// <param name="dataRow">The <see cref="DataRow"/> to convert.</param>
        /// <returns></returns>
        public static CustomerModel FromDataRow(DataRow dataRow)
        {
            var result = new CustomerModel
            {
                Id = dataRow.Field<int>("id"),
                CustomerId = dataRow.Field<int>("customerid"),
                Name = dataRow.Field<string>("name"),
                EmailAddress = dataRow.Field<string>("emailadres"),
                BackupOrderNumber = dataRow.Field<int>("backup_ordernr"),
                EndDate = dataRow.Field<DateTime?>("enddate"),
                Notes = dataRow.Field<string>("notes"),
                StartDate = dataRow.Field<DateTime?>("startdatum"),
                GoogleAuthenticationEnabled = Convert.ToBoolean(dataRow["google_auth"]),
                InstructionsUrl = dataRow.Field<string>("webmanagerManualURL"),
                Ftp = new ConnectionInformationModel
                {
                    Password = dataRow.Field<string>("ftp_passencrypted"),
                    Username = dataRow.Field<string>("ftp_user"),
                    Host = dataRow.Field<string>("ftp_host"),
                    RootFolder = dataRow.Field<string>("ftp_root")
                },
                LiveDatabase = new ConnectionInformationModel
                {
                    Password = dataRow.Field<string>("db_passencrypted"),
                    Username = dataRow.Field<string>("db_login"),
                    PortNumber = String.IsNullOrWhiteSpace(dataRow.Field<string>("db_port")) ? 3306 : Convert.ToInt32(dataRow.Field<string>("db_port")),
                    Host = dataRow.Field<string>("db_host"),
                    DatabaseName = dataRow.Field<string>("db_dbname")
                },
                TestDatabase = new ConnectionInformationModel
                {
                    Password = dataRow.Field<string>("db_passencrypted_test"),
                    Username = dataRow.Field<string>("db_login_test"),
                    PortNumber = String.IsNullOrWhiteSpace(dataRow.Field<string>("db_port_test")) ? 3306 : Convert.ToInt32(dataRow.Field<string>("db_port_test")),
                    Host = dataRow.Field<string>("db_host_test"),
                    DatabaseName = dataRow.Field<string>("db_dbname_test")
                },
                EncryptionKey = dataRow.Field<string>("encryption_key"),
                MailHost = dataRow.Field<string>("mailhost"),
                SubDomain = dataRow.Field<string>("subdomain")
            };

            if (dataRow.Table.Columns.Contains("propertys") && !dataRow.IsNull("propertys"))
            {
                var array = dataRow.Field<string>("propertys").Replace("\r\n", "\n").Split('\r', '\n');
                foreach (var property in array)
                {
                    var separatorIndex = property.IndexOf('=');
                    if (separatorIndex < 0)
                    {
                        result.Properties.Add(property, String.Empty);
                        continue;
                    }

                    var key = property.Substring(0, separatorIndex);
                    var value = property.Substring(separatorIndex + 1);
                    result.Properties.Add(key, value);
                }
            }

            return result;
        }
    }
}