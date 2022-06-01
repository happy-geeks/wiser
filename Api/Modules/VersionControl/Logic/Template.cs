using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Core.Services;
using Api.Modules.VersionControl.Enums;
using Api.Modules.VersionControl.Models;

namespace Api.Modules.VersionControl.Logic
{
    public class Template
    {
        public string TemplateName { get; set; }
        public int Version { get; set; }
        public int TemplateId { get; set; }
        public PublishEnvironments Environment { get; set; }
        public List<DynamicContent> DynamicContents { get; set; }
        public Commit Commits { get; set; }


        public Template(string templateName, int version, int templateId, PublishEnvironments environment)
        {
            this.TemplateName = templateName;
            this.Version = version;
            this.TemplateId = templateId;
            this.Environment = environment;
        }
        


    }
}
