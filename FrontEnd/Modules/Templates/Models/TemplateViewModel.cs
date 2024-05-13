using FrontEnd.Modules.Base.Models;

namespace FrontEnd.Modules.Templates.Models;

public class TemplateViewModel : BaseModuleViewModel
{
    public int TemplateId { get; set; }

    public string InitialTab { get; set; }
}