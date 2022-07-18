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
        /// The version of the template.
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
    }
}
