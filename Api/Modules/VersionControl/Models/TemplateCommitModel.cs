using System;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Modules.Templates.Enums;

namespace Api.Modules.VersionControl.Models
{
    /// <summary>
    /// Model that represents a template linked to a commit.
    /// </summary>
    public class TemplateCommitModel
    {
        /// <summary>
        /// The id of the commit.
        /// </summary>
        public int CommitId { get; set; }

        /// <summary>
        /// The id of the template.
        /// </summary>
        public int TemplateId { get; set; }

        /// <summary>
        /// The latest version of the template.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// The environment of the template.
        /// </summary>
        public Environments Environment { get; set; }

        /// <summary>
        /// Bool if the template is on the live environment.
        /// </summary>
        public bool IsLive { get; set; }

        /// <summary>
        /// Bool if the template is on the acceptance environment.
        /// </summary>
        public bool IsAcceptance { get; set; }

        /// <summary>
        /// Bool if the template is on the test environment.
        /// </summary>
        public bool IsTest { get; set; }

        /// <summary>
        /// Gets or sets the template type.
        /// </summary>
        public TemplateTypes TemplateType { get; set; }

        /// <summary>
        /// Gets or sets the parent ID of the template.
        /// </summary>
        public int TemplateParentId { get; set; }

        /// <summary>
        /// Gets or sets the parent name of the template.
        /// </summary>
        public string TemplateParentName { get; set; }

        /// <summary>
        /// Gets or sets the name of the template.
        /// </summary>
        public string TemplateName { get; set; }

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
        /// Gets or sets the date and time that the commit was done.
        /// </summary>
        public DateTime ChangedOn { get; set; }

        /// <summary>
        /// Gets or sets the name of the user that made the commit.
        /// </summary>
        public string ChangedBy { get; set; }
    }
}
