using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Modules.VersionControl.Enums;

namespace Api.Modules.VersionControl.Models
{
    public class TemplateCommitModel
    {
        public int CommitId { get; set; }
        public int TemplateId { get; set; }
        public int Version { get; set; }

        public string Enviornment { get; set; }
        public bool IsLive { get; set; }
        public bool IsAcceptance { get; set; }
        public bool IsTest { get; set; }
    }
}
