using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Api.Modules.Imports.Models
{
    /// <summary>
    /// A model for the confirmation of the links to delete by import.
    /// </summary>
    public class DeleteLinksConfirmModel
    {
        /// <summary>
        /// Gets or sets the IDs of the links to delete or the IDs of the items to remove the parent ID from.
        /// </summary>
        [Required]
        public List<ulong> Ids { get; set; }

        /// <summary>
        /// Gets or sets if the link is made by a parent ID.
        /// </summary>
        [Required]
        public bool UseParentId { get; set; }

        /// <summary>
        /// Gets or sets the entity type of the source item of the link.
        /// </summary>
        [Required]
        public string SourceEntityType { get; set; }

        /// <summary>
        /// Gets or sets the IDs of the source items of the link.
        /// </summary>
        [Required]
        public List<ulong> SourceIds { get; set; }

        /// <summary>
        /// Gets or sets the entity type of the destination item of the link.
        /// </summary>
        [Required]
        public string DestinationEntityType { get; set; }

        /// <summary>
        /// Gets or sets the IDs of the destination items of the link.
        /// </summary>
        [Required]
        public List<ulong> DestinationIds { get; set; }
    }
}
