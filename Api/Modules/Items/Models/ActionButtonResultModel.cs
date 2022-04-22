using Newtonsoft.Json.Linq;

namespace Api.Modules.Items.Models
{
    /// <summary>
    /// A model for returning results from an action-button action.
    /// </summary>
    public class ActionButtonResultModel
    {
        /// <summary>
        /// Gets or sets whether the action was executed successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message, if the action was not successful.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the encrypted item ID that resulted from this action.
        /// </summary>
        public string ItemId { get; set; }

        /// <summary>
        /// Gets or sets the link ID that resulted from this action.
        /// </summary>
        public int LinkId { get; set; }

        /// <summary>
        /// Gets or sets any other data to use for this action.
        /// </summary>
        public JArray OtherData { get; set; }
    }
}