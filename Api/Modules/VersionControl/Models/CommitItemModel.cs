using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Modules.VersionControl.Models
{

    /// <summary>
    /// A model That represents a commit with a specific template.
    /// </summary>
    public class CommitItemModel
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
    }
}
