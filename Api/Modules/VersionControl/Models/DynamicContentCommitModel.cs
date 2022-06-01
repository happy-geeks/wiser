using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Modules.VersionControl.Models
{
    public class DynamicContentCommitModel
    {
        public int Id { get; set; }
        public int DynamicContentId { get; set; }
        public int Version { get; set; }
        public int CommitId { get; set; }
        public DateTime addedOn { get; set; }

    }
}
