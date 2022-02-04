using System.Collections.Generic;
using Api.Modules.Templates.Models.Other;

namespace Api.Modules.Templates.Models.History
{
    public class TemplateHistoryOverviewModel
    {
        public int TemplateId { get; set; }
        public PublishedEnvironmentModel PublishedEnvironment { get; set; }
        public List<TemplateHistoryModel> TemplateHistory { get; set; }
        public List<PublishHistoryModel> PublishHistory { get; set; }
    }
}
