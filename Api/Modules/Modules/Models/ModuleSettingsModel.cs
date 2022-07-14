using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Modules.Models
{
    /// <summary>
    /// A model for information about a Wiser 2.0 module.
    /// </summary>
    public class ModuleSettingsModel
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the module.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the custom query of the module.
        /// </summary>
        public string CustomQuery { get; set; }

        /// <summary>
        /// Gets or sets the count query of the module.
        /// </summary>
        public string CountQuery { get; set; }

        /// <summary>
        /// Gets or sets the options / settings.
        /// </summary>
        public JToken Options { get; set; }

        /// <summary>
        /// Gets or sets the icon of the module.
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets the group name of the module.
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Gets or sets the type of module.
        /// Most modules are of the type "DynamicItems", which is also the default value.
        /// Other modules could be "Admin" or "Scheduler" for example, which means they are very different than the standard dynamic modules.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets whether the user is allowed to read items in this module.
        /// </summary>
        public bool CanRead { get; set; }

        /// <summary>
        /// Gets or sets whether the user is allowed to change existing items in this module.
        /// </summary>
        public bool CanWrite { get; set; }

        /// <summary>
        /// Gets or sets whether the user is allowed to create new items in this module.
        /// </summary>
        public bool CanCreate { get; set; }

        /// <summary>
        /// Gets or sets whether the user is allowed to delete items in this module.
        /// </summary>
        public bool CanDelete { get; set; }

        /// <summary>
        /// Gets the description of the module.
        /// </summary>
        public string Description => (string.IsNullOrEmpty(Name)) ? $"n.a. ({Id})" : $"{Name} ({Id})";

    }
}