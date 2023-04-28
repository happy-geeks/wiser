using System;
using System.Collections.Generic;

namespace Api.Modules.VersionControl.Models
{
    /// <summary>
    /// A model that represents a connection between a dynamic content and a commit, or a dynamic content that still needs to be committed.
    /// </summary>
    public class DynamicContentCommitModel
    {
        /// <summary>
        /// The id of the commit.
        /// </summary>
        public int CommitId { get; set; }

        /// <summary>
        /// The id of the dynamic content.
        /// </summary>
        public int DynamicContentId { get; set; }

        /// <summary>
        /// The version of the dynamic content.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the component type.
        /// </summary>
        public string Component { get; set; }

        /// <summary>
        /// Gets or sets the component mode.
        /// </summary>
        public string ComponentMode { get; set; }

        /// <summary>
        /// The date The commit was added on.
        /// </summary>
        public DateTime ChangedOn { get; set; }

        /// <summary>
        /// Gets or sets the name of the user that made the last change.
        /// </summary>
        public string ChangedBy { get; set; }

        /// <summary>
        /// Bool if the content is on the live environment.
        /// </summary>
        public bool IsLive { get; set; }

        /// <summary>
        /// Bool if the content is on the acceptance environment.
        /// </summary>
        public bool IsAcceptance { get; set; }

        /// <summary>
        /// Bool if the content is on the test environment.
        /// </summary>
        public bool IsTest { get; set; }

        /// <summary>
        /// Gets or sets the version that is currently published to test.
        /// </summary>
        public int VersionTest { get; set; }

        /// <summary>
        /// Gets or sets the version that is currently published to acceptance.
        /// </summary>
        public int VersionAcceptance { get; set; }

        /// <summary>
        /// Gets or sets the version that is currently published to live/production.
        /// </summary>
        public int VersionLive { get; set; }

        /// <summary>
        /// Gets or sets the IDs of the templates that this content is linked to.
        /// </summary>
        public List<int> TemplateIds { get; set; }

        /// <summary>
        /// Gets or sets the names of the templates that this content is linked to.
        /// </summary>
        public List<string> TemplateNames { get; set; }
    }
}