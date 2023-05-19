using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GeeksCoreLibrary.Core.Enums;
using Newtonsoft.Json;

namespace Api.Modules.Items.Models
{
    /// <summary>
    /// A model of a Wiser item that contains all fields/details as direct properties in the JSON, instead of a list of item details that needs to be iterated through.
    /// This is made for Zapier, but can also be used for other things in the future.
    /// </summary>
    public class FlatItemModel
    {
        /// <summary>
        /// Gets or sets the ID of the item.
        /// </summary>
        [Key]
        public ulong Id { get; set; }

        /// <summary>
        /// Gets or sets the encrypted ID of an item.
        /// This should be encrypted via the method JCLEUtils.AESEncode(), with the encryption key unique to the customer and the parameter "withdate" set to true.
        /// </summary>
        public string EncryptedId { get; set; }

        /// <summary>
        /// Gets or sets the unique uuid, this is for saving IDs of external systems.
        /// If items are synchronized with other systems, the external ID should be saved here.
        /// </summary>
        public string UniqueUuid { get; set; }

        /// <summary>
        /// Gets or sets the ID of the module this item belongs to.
        /// </summary>
        public int ModuleId { get; set; }

        /// <summary>
        /// Gets or sets the published environment of the item.
        /// This decides in which environment(s) this item should be visible (none, dev, test, acceptance and live).
        /// </summary>
        public Environments? PublishedEnvironment { get; set; }

        /// <summary>
        /// Gets or sets whether this item is removed/deleted.
        /// </summary>
        public bool? Removed { get; set; }

        /// <summary>
        /// Gets or sets the title of the item.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the date and time this item was created.
        /// </summary>
        public DateTime AddedOn { get; set; }

        /// <summary>
        /// Gets or sets the name of the user that created this item.
        /// </summary>
        public string AddedBy { get; set; }

        /// <summary>
        /// Gets or sets the date and time of when this item has last been changed.
        /// </summary>
        public DateTime ChangedOn { get; set; }

        /// <summary>
        /// Gets or sets the name of the user that last changed this item.
        /// </summary>
        public string ChangedBy { get; set; }

        /// <summary>
        /// Gets or sets the entity type of the item.
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// Object with all fields and their values. The <see cref="JsonExtensionDataAttribute"/> will cause the serializer to flatten this object.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> Fields { get; set; } = new();
    }
}