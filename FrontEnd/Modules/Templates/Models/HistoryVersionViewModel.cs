using System.Collections.Generic;
using Api.Modules.Templates.Models.History;

namespace FrontEnd.Modules.Templates.Models;

public class HistoryVersionViewModel : HistoryVersionModel
{
    public List<(FieldViewModel OldVersion, FieldViewModel NewVersion)> ChangedFields { get; set; }
}