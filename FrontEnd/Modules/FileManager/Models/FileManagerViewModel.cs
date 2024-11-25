using FrontEnd.Modules.Base.Models;
using FrontEnd.Modules.FileManager.Enums;

namespace FrontEnd.Modules.FileManager.Models;

public class FileManagerViewModel : BaseModuleViewModel
{
    public FileManagerModes? Mode { get; set; }
    
    public bool Iframe { get; set; }

    public string SelectedText { get; set; } = "";

    public bool HideFields { get; set; }
}