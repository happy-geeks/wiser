namespace Api.Modules.Items.Models
{
    /// <summary>
    /// A model for a request to move an item to a different location.
    /// </summary>
    public class MoveItemRequestModel
    {
        /// <summary>
        /// Gets or sets the new position of the item.
        /// </summary>
        public string Position { get; set; }

        /// <summary>
        /// Gets or sets the encrypted ID of the original parent.
        /// </summary>
        public string EncryptedSourceParentId { get; set; }

        /// <summary>
        /// Gets or sets the encrypted ID of the parent of the destination item (the new location of the item).
        /// </summary>
        public string EncryptedDestinationParentId { get; set; }

        /// <summary>
        /// Gets or sets the entity type of the item that is being moved.
        /// </summary>
        public string SourceEntityType { get; set; }

        /// <summary>
        /// Gets or sets the entity type of the new parent item.
        /// </summary>
        public string DestinationEntityType { get; set; }

        /// <summary>
        /// Gets or sets the ID of the module.
        /// </summary>
        public int ModuleId { get; set; }
    }
}
