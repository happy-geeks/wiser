using System.Collections.Generic;
using Api.Modules.Templates.Models.DynamicContent;
using Api.Modules.Templates.Models.History;
using Api.Modules.Templates.Models.Preview;
using GeeksCoreLibrary.Modules.Templates.ViewModels;

namespace Api.Modules.Templates.Models.Template
{
    public class TemplateOverviewModel : PageViewModel
    {
        public DevelopmentTemplateModel development { get; set; }
        public TemplateHistoryOverviewModel history { get; set; }
        public List<DynamicContentOverviewModel> linkedDynamicContent { get; set; }
        public List<PreviewProfileModel> previewProfiles { get; set; }
    }
}
