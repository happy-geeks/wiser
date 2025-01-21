using System.Collections.Generic;
using System.Linq;
using Api.Modules.Templates.Attributes;

namespace FrontEnd.Modules.Templates.Models;

public class RenderPropertiesModel
{
    public string Tab { get; }
    public List<PropertyModel> Properties { get; }

    public RenderPropertiesModel(object model, string tab)
    {
        Tab = tab;
        Properties = new List<PropertyModel>();

        var type = model.GetType();
        foreach (var property in type.GetProperties())
        {
            if (property.GetCustomAttributes(typeof(WtsPropertyAttribute), false).FirstOrDefault() is WtsPropertyAttribute {IsVisible: true} attributes
                && (attributes.ConfigurationTab == null || attributes.ConfigurationTab == tab))
            {
                Properties.Add(new PropertyModel
                {
                    Name = property.Name,
                    Data = property.GetValue(model),
                    Attributes = attributes
                });
            }
        }
    }
}