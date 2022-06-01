using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Modules.VersionControl.Models
{
    public class CommitItemModel
    {
        public int CommitId { get; set; }

        public int TemplateId { get; set; }

        public int Version { get; set; }
    }
}
