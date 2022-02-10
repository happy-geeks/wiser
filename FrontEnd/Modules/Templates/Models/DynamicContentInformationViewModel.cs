using System.Collections.Generic;
using Api.Modules.Templates.Models.DynamicContent;
using GeeksCoreLibrary.Modules.Templates.ViewModels;

namespace FrontEnd.Modules.Templates.Models
{
    public class DynamicContentInformationViewModel : PageViewModel
    {
        public List<TabViewModel> Tabs { get; set; } = new();
    }
}
