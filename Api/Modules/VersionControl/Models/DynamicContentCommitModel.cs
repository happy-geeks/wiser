using System;

namespace Api.Modules.VersionControl.Models
{
    /// <summary>
    /// A model that represents a connection between a dynamic content item and a commit
    /// </summary>
    public class DynamicContentCommitModel
    {
        /// <summary>
        /// The id of the commit.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The id of the dynamic content.
        /// </summary>
        public int DynamicContentId { get; set; }

        /// <summary>
        /// The version of the dynamic content.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// The id of the commit
        /// </summary>
        public int CommitId { get; set; }

        /// <summary>
        /// The data The commit was added on.
        /// </summary>
        public DateTime AddedOn { get; set; }

    }
}
