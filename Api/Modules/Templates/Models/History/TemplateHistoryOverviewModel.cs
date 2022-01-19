using System.Collections.Generic;
using Api.Modules.Templates.Models.Other;

namespace Api.Modules.Templates.Models.History
{
    public class TemplateHistoryOverviewModel
    {
        public int templateId;

        public PublishedEnvironmentModel publishedEnvironment;
        public List<TemplateHistoryModel> templateHistory;
        public List<PublishHistoryModel> publishHistory;

        public TemplateHistoryOverviewModel (int templateId, List<TemplateHistoryModel> templateHistory, List<PublishHistoryModel> publishHistory)
        {
            this.templateId = templateId;
            this.templateHistory = templateHistory;
            this.publishHistory = publishHistory;
        }

    }
}
