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
        /// Gets or sets the options / settings.
        /// </summary>
        public JToken Options { get; set; }
        
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
    }
}