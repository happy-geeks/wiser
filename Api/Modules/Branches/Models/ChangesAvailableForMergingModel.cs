using System.Collections.Generic;

namespace Api.Modules.Branches.Models
{
    /// <summary>
    /// A model for returning all changes that can be merged back into the main/original branch.
    /// </summary>
    public class ChangesAvailableForMergingModel
    {
        /// <summary>
        /// Gets or sets the entities that can be merged.
        /// </summary>
        public List<EntityChangesModel> Entities { get; set; } = new();

        /// <summary>
        /// Gets or sets the Wiser settings that can be merged.
        /// </summary>
        public List<SettingsChangesModel> Settings { get; set; } = new();

        /// <summary>
        /// Gets or sets the link types that can be merged.
        /// </summary>
        public List<LinkTypeChangesModel> LinkTypes { get; set; } = new();
    }
}