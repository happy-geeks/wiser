namespace Api.Modules.DataSelectors.Models
{
    //TODO Verify comments
    /// <summary>
    /// A model for a Wiser data selector.
    /// </summary>
    public class DataSelectorModel
    {
        /// <summary>
        /// Gets or sets the ID of the data selector.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the encrypted ID of the data selector.
        /// </summary>
        public string EncryptedId { get; set; }

        /// <summary>
        /// Gets or sets the name of the data selector.
        /// </summary>
        public string Name { get; set; }
    }
}
