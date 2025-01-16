using System.Collections.Generic;
using FrontEnd.Modules.Base.Models;

namespace FrontEnd.Modules.DataSelector.Models;

public class DataSelectorViewModel : BaseModuleViewModel
{
    public List<string> EmbedOptions { get; set; } = [];

    public List<Dictionary<string, string>> PropertyFormattingOptions { get; set; } = [];

    public bool ExportMode { get; set; }
}