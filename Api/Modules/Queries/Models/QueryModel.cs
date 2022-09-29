using System;

namespace Api.Modules.Queries.Models
{
    //TODO Verify comments
    /// <summary>
    /// A model for a query within Wiser.
    /// </summary>
    public class QueryModel
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the encrypted id.
        /// </summary>
        public string EncryptedId { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the query.
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// Gets or sets if the query is shown in the export module.
        /// </summary>
        public bool ShowInExportModule { get; set; }

        /// <summary>
        /// Gets or sets if the query is only allowed to be executed by certain roles.
        /// </summary>
        public string AllowedRoles { get; set; }

        /// <summary>
        /// Gets or sets when the query was last changed on.
        /// </summary>
        public DateTime? ChangedOn { get; set; }
    }
}
