using GeeksCoreLibrary.Core.Enums;

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
        public Environments Enviornment { get; set; }

        /// <summary>
        /// Bool if the template is on the live environemnt.
        /// </summary>
        public bool IsLive { get; set; }

        /// <summary>
        /// Bool if the template is on the acceptance environemnt.
        /// </summary>
        public bool IsAcceptance { get; set; }

        /// <summary>
        /// Bool if the template is on the test environemnt.
        /// </summary>
        public bool IsTest { get; set; }
    }
}
