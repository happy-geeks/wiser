using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Modules.Templates.Models.Other;

namespace Api.Modules.VersionControl.Models
{
    public class DynamicContentModel
    {
        public int Id { get; set; }
        public int Version { get; set; }
        public string Title { get; set; }
        public string Component { get; set; }
        public string ComponentMode { get; set; }
        public int? ComponentModeId { get; set; }
        public List<string> Usages { get; set; }
        public int? Renders { get; set; }
        public int? AverageRenderTime { get; set; }
        public DateTime? ChangedOn { get; set; }
        public string ChangedBy { get; set; }
        public int? LatestVersion { get; set; }

    }
}
