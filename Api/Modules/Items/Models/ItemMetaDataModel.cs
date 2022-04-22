using System;

namespace Api.Modules.Items.Models
{
    /// <summary>
    /// A model for the meta data of a Wiser item.
    /// </summary>
    public class ItemMetaDataModel
    {
        /// <summary>
        /// Gets or sets the ID of the item.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Gets or sets the original ID of the item. This is used when an item has multiple versions, then this property will contain the ID of the item that was created first.
        /// </summary>
        public string OriginalItemId { get; set; }

        /// <summary>
        /// Gets or sets the encrypted ID of this item.
        /// </summary>
        public string EncryptedId { get; set; }

        /// <summary>
        /// Gets or sets the entity type of this item.
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// Gets or sets the unique UUID of this item. This is normally used to save IDs from external systems, when synching items between Wiser and an external system.
        /// </summary>
        public string UniqueUuid { get; set; }

        /// <summary>
        /// Gets or sets the environment(s) that this item should be visible on.
        /// </summary>
        public int PublishedEnvironment { get; set; }

        /// <summary>
        /// Gets or sets whether this item is read only and cannot be changed.
        /// </summary>
        public string ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets the title/name of the item.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the date and time that this item was created.
        /// </summary>
        public DateTime AddedOn { get; set; }

        /// <summary>
        /// Gets or sets the name of the user that created this item.
        /// </summary>
        public string AddedBy { get; set; }

        /// <summary>
        /// Gets or sets the date and time that this item was last changed.
        /// </summary>
        public DateTime? ChangedOn { get; set; }

        /// <summary>
        /// Gets or sets the user that last changed this item.
        /// </summary>
        public string ChangedBy { get; set; }

        /// <summary>
        /// Gets or sets whether this item has been marked as removed.
        /// </summary>
        public bool Removed { get; set; }

        /// <summary>
        /// Gets or sets whether this item can have different values/versions for different environments.
        /// </summary>
        public bool EnableMultipleEnvironments { get; set; }
        
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