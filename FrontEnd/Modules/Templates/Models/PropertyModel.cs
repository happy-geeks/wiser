using Api.Modules.Templates.Attributes;

namespace FrontEnd.Modules.Templates.Models;

public class PropertyModel
{
    public string Name { get; set; }
    public object Data { get; set; }
    public WtsPropertyAttribute Attributes { get; set; }
}