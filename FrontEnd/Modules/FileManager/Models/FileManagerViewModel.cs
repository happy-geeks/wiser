using FrontEnd.Modules.Base.Models;
using FrontEnd.Modules.FileManager.Enums;

namespace FrontEnd.Modules.FileManager.Models;

public class FileManagerViewModel : BaseModuleViewModel
{
    public FileManagerModes? Mode { get; set; }
}