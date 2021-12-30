using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Api.Modules.Modules.Models
{
    public class ModuleAccessRightsModel
    {
        /// <summary>
        /// Gets or sets the ID of the module.
        /// </summary>
        public int ModuleId { get; set; }

        /// <summary>
        /// Gets or sets the name of the module.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the icon of the module.
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets the main color of the module.
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// Gets or sets the type of module.
        /// Most modules are of the type "DynamicItems", which is also the default value.
        /// Other modules could be "Admin" or "Scheduler" for example, which means they are very different than the standard dynamic modules.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets whether the module should be visible.
        /// </summary>
        public bool Show { get; set; }

        /// <summary>
        /// Gets or sets whether the user is allowed to read items in the module.
        /// </summary>
        public bool CanRead { get; set; }

        /// <summary>
        /// Gets or sets whether the user is allowed to change existing items in the module.
        /// </summary>
        public bool CanWrite { get; set; }

        /// <summary>
        /// Gets or sets whether the user is allowed to create new items in the module.
        /// </summary>
        public bool CanCreate { get; set; }

        /// <summary>
        /// Gets or sets whether the user is allowed to delete items in the module.
        /// </summary>
        public bool CanDelete { get; set; }

        /// <summary>
        /// Gets or sets the order number of the module in the left/main menu.
        /// </summary>
        public int MenuOrder { get; set; }

        /// <summary>
        /// Gets or sets the order number of the module on the homepage.
        /// </summary>
        public int MetroOrder { get; set; }

        /// <summary>
        /// Gets or sets the IP restrictions of this module.
        /// If one or more IP addresses are entered, this module can only be accessed via those IPs for this user.
        /// </summary>
        public List<string> IpWhitelist { get; set; }

        /// <summary>
        /// Gets or sets the group name of the module.
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Convert a <see cref="DataRow"/> to an <see cref="ModuleAccessRightsModel"/>.
        /// </summary>
        /// <param name="dataRow">The <see cref="DataRow"/> to convert.</param>
        /// <returns></returns>
        public static ModuleAccessRightsModel FromDataRow(DataRow dataRow)
        {
            var result = new ModuleAccessRightsModel
            {
                CanCreate = Convert.ToBoolean(dataRow["create"]),
                CanDelete = Convert.ToBoolean(dataRow["delete"]),
                CanRead = Convert.ToBoolean(dataRow["read"]),
                CanWrite = Convert.ToBoolean(dataRow["write"]),
                MenuOrder = Convert.ToInt32(dataRow["volgorde"]),
                MetroOrder = Convert.ToInt32(dataRow["metroorder"]),
                ModuleId = Convert.ToInt32(dataRow["moduleid"]),
                Show = Convert.ToBoolean(dataRow["show"])
            };

            var whiteListString = dataRow.Field<string>("ip_whitelist");
            if (!String.IsNullOrWhiteSpace(whiteListString))
            {
                result.IpWhitelist = whiteListString.Split(',').ToList();
            }

            return result;
        }
    }
}
