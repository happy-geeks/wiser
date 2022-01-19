using System;
using System.Collections.Generic;
using Api.Modules.Templates.Models.Other;

namespace Api.Modules.Templates.Models.DynamicContent
{
    public class DynamicContentOverviewModel
    {
        public int id { get; set; }
        public string title { get; set; }
        public string component { get; set; }
        public string component_mode { get; set; }
        public List<string> usages { get; set; }
        public int renders { get; set; }
        public int avgRenderTime { get; set; }
        public DateTime changed_on { get; set; }
        public string changed_by { get; set; }

        public PublishedEnvironmentModel versions { get; set; }

    }
}
