using System.Collections.Generic;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.History;
using Api.Modules.Templates.Models.Preview;
using GeeksCoreLibrary.Modules.Templates.ViewModels;

namespace Api.Modules.Templates.Models.Template
{
    public class TemplateOverviewModel : PageViewModel
    {
        public DevelopmentTemplateModel Development { get; set; }
        public TemplateHistoryOverviewModel History { get; set; }
        public List<DynamicContentOverviewModel> LinkedDynamicContent { get; set; }
        public List<PreviewProfileModel> PreviewProfiles { get; set; }
    }
}
