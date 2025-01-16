using System.Collections.Generic;
using Api.Modules.Templates.Models.History;

namespace FrontEnd.Modules.Templates.Models;

public class TemplateHistoryViewModel : TemplateHistoryModel
{
    public new List<HistoryVersionViewModel> DynamicContentChanges { get; set; }
}