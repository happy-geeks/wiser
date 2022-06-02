using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;

namespace Api.Modules.VersionControl.Models
{
    public class CreateCommitModel
    {
        public int id { get; set; }
        public string Description { get; set; }
        public int AsanaId { get; set; }
        public DateTime AddedOn { get; set; }
        public string ChangedBy { get; set; }
    }
}
