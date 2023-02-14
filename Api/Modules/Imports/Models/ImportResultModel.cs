using System.Collections.Generic;

namespace Api.Modules.Imports.Models
{
    /// <summary>
    /// A model for the result of the Wiser import module.
    /// </summary>
    public class ImportResultModel
    {
        /// <summary>
        /// Gets or sets the total amount of items imported
        /// </summary>
        public uint ItemsTotal { get; set; }

        /// <summary>
        /// Gets or sets the amount of created items that are imported
        /// </summary>
        public uint ItemsCreated { get; set; }

        /// <summary>
        /// Gets or sets the amount of updated items that are imported
        /// </summary>
        public uint ItemsUpdated { get; set; }

        /// <summary>
        /// Gets or sets the amount of successfully imported items
        /// </summary>
        public uint Successful { get; set; }

        /// <summary>
        /// Gets or sets the amount of items that have to failed to be imported
        /// </summary>
        public uint Failed { get; set; }

        /// <summary>
        /// Gets or sets the given errors that have occured during the import
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the given errors that have occured during the import in a user friendly wording
        /// </summary>
        public List<string> UserFriendlyErrors { get; set; } = new List<string>();
    }
}
