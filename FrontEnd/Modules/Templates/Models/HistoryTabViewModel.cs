using System.Collections.Generic;
using Api.Modules.Templates.Models.History;

namespace FrontEnd.Modules.Templates.Models
{
    public class HistoryTabViewModel : TemplateHistoryOverviewModel
    {
        public new List<TemplateHistoryViewModel> TemplateHistory { get; set; }
    }
}
