using System;
using System.Collections.Generic;
using Api.Modules.Templates.Models.Other;

namespace Api.Modules.Templates.Models.DynamicContent
{
    public class DynamicContentOverviewModel
    {
        public int Id { get; set; }
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
        public Dictionary<string, object> Data { get; set; }

        public PublishedEnvironmentModel Versions { get; set; }
    }
}
