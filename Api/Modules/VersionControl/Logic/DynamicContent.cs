using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Modules.VersionControl.Enums;

namespace Api.Modules.VersionControl.Logic
{
    public class DynamicContent
    {
        public int Id { get; set; }
        public string Settings { get; set; }
        public string Component { get; set; }
        public int Version { get; set; }
        public string ChangedBy { get; set; }
        public PublishEnvironments Environments { get; set; }
        public Template template { get; set; }

        public DynamicContent(int id, string settings, string component, int version, string changedBy, PublishEnvironments environments, Template template)
        {
            this.Id = id;
            this.Settings = settings;
            this.Component = component;
            this.Version = version;
            this.ChangedBy = changedBy;
            this.Environments = environments;
            this.template = template;

        }




        
    }
}
