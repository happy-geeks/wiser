using FrontEnd.Modules.Base.Models;
using Microsoft.AspNetCore.Mvc;

namespace FrontEnd.Modules.DynamicItems.Models;

public class DynamicItemsViewModel : BaseModuleViewModel
{
    [FromQuery(Name = "moduleId")]
    public int ModuleId { get; set; }

    [FromQuery(Name = "moduleName")]
    public string ModuleName { get; set; }

    [FromQuery(Name = "itemId")]
    public string InitialItemId { get; set; }

    [FromQuery(Name = "parentId")]
    public string ParentItemId { get; set; }

    [FromQuery(Name = "entityType")]
    public string EntityType { get; set; }

    [FromQuery(Name = "newItemData")]
    public string NewItemData { get; set; }

    [FromQuery(Name = "iframe")]
    public bool IframeMode { get; set; }

    [FromQuery(Name = "readonly")]
    public bool ReadOnly { get; set; }

    [FromQuery(Name = "hideHeader")]
    public bool HideHeader { get; set; }

    [FromQuery(Name = "hideFooter")]
    public bool HideFooter { get; set; }

    [FromQuery(Name = "createNewItem")]
    public bool CreateNewItem { get; set; }

    [FromQuery(Name = "saveButtonText")]
    public string SaveButtonText { get; set; } = "Opslaan";

    [FromQuery(Name = "isAllowedToChangePropertyWidth")]
    public bool IsAllowedToChangePropertyWidth { get; set; }
}