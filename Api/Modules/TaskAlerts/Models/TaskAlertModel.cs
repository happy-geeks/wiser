using System;
using Newtonsoft.Json;

namespace Api.Modules.TaskAlerts.Models
{
    //TODO Verify comments
    /// <summary>
    /// The model of a Wiser task alert.
    /// </summary>
    public class TaskAlertModel
    {
        // Note: All these JsonProperties are for backwards compatibility, to keep the original Wiser 2.0 names in the JSON.

        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// Gets or sets the encrypted id.
        /// </summary>
        [JsonProperty("idencrypted")]
        public string EncryptedId { get; set; }

        /// <summary>
        /// Gets or sets the content of the task alert.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the date and time the task alert has been created on.
        /// </summary>
        [JsonProperty("agendering_date")]
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Gets or sets the date and time the task alert was seen.
        /// </summary>
        [JsonProperty("checkedon")]
        public DateTime? CheckedOn { get; set; }

        /// <summary>
        /// Gets or sets the ID of the module that this task alert belongs to.
        /// </summary>
        [JsonProperty("moduleid")]
        public int ModuleId { get; set; }

        /// <summary>
        /// Gets or sets the name of the user that placed the task alert.
        /// </summary>
        [JsonProperty("placed_by")]
        public string PlacedBy { get; set; }


        /// <summary>
        /// Gets or sets the ID of the user that placed the task alert.
        /// </summary>
        [JsonProperty("placed_by_id")]
        public ulong PlacedById { get; set; }

        /// <summary>
        /// Gets or sets the state of th task alert.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the ID of the current user.
        /// </summary>
        [JsonProperty("userid")]
        public ulong UserId { get; set; }

        /// <summary>
        /// Gets or sets the linked item entity type.
        /// </summary>
        [JsonProperty("linked_item_entity_type")]
        public string LinkedItemEntityType { get; set; }

        /// <summary>
        /// Gets or sets the ID of the linked item.
        /// </summary>
        [JsonProperty("linked_item_id")]
        public string LinkedItemId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the module that the linked item belongs to.
        /// </summary>
        [JsonProperty("linked_item_module_id")]
        public int? LinkedItemModuleId { get; set; }
    }
}
