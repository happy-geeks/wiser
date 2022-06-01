using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Modules.VersionControl.Logic
{
    public class Commit
    {
        public string Description { get; set; }
        public DateTime AddedOn { get; set; }
        public string ChangedBy { get; set; }
        public List<DynamicContent> DynamicContent { get; set; }
        public List<Template> Template { get; set; }

        public Commit(string description, DateTime addedOn, string changedBy)
        {
            this.Description = description;
            this.AddedOn = addedOn;
            this.ChangedBy = changedBy;
        }
    }
}
