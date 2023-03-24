namespace Api.Modules.DataSelectors.Models
{
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

        /// <summary>
        /// Gets or sets whether the data selector has been removed.
        /// </summary>
        public bool Removed { get; set; }

        /// <summary>
        /// Gets or sets the request JSON.
        /// </summary>
        public string RequestJson { get; set; }

        /// <summary>
        /// Gets or sets the saved JSON.
        /// </summary>
        public string SavedJson { get; set; }

        /// <summary>
        /// Gets or sets whether this should be shown in the export module.
        /// </summary>
        public bool ShowInExportModule { get; set; }

        /// <summary>
        /// Gets or sets whether this should be shown in the communication module.
        /// </summary>
        public bool ShowInCommunicationModule { get; set; }

        /// <summary>
        /// Gets or sets whether this should be available for rendering as dynamic data in an HTML editor in Wiser.
        /// </summary>
        public bool AvailableForRendering { get; set; }

        /// <summary>
        /// Gets or sets whether the result of this data selector should be shown in the "Dataselector" tile of the dashboard.
        /// </summary>
        public bool ShowInDashboard { get; set; }
        
        /// <summary>
        /// Gets or sets whether this data selector should be available when making branches.
        /// </summary>
        public bool AvailableForBranches { get; set; }

        /// <summary>
        /// Gets or sets the ID of the default HTML template for rendering. Only applicable if <see cref="AvailableForRendering"/> is set to <see langword="true"/>.
        /// </summary>
        public ulong DefaultTemplate { get; set; }
        
        /// <summary>
        /// Gets or sets the roles that are allowed to execute the data selector from the API.
        /// </summary>
        public string AllowedRoles { get; set; }
    }
}
