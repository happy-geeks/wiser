using System.Collections.Generic;
using GeeksCoreLibrary.Modules.Templates.ViewModels;

namespace FrontEnd.Modules.Templates.Models;

public class DynamicContentInformationViewModel : PageViewModel
{
    public List<TabViewModel> Tabs { get; set; } = [];
}