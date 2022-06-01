using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Modules.VersionControl.Models
{
    public class ModuleGridSettings
    {
        public int ModuleId { get; set; }
        public string CustomQuery { get; set; }
        public string CountQuery { get; set; }
        public string GridOptions { get; set; }
        public string GridDivId { get; set; }
        public string Name { get; set; }
    }
}
