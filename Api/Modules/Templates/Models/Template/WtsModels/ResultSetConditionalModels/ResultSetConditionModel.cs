using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Serialization;
using Api.Modules.Templates.Attributes;
using Api.Modules.Templates.Enums;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Api.Modules.Templates.Models.Template.WtsModels;

public class ResultSetConditionModel
{
    //the selector and valueForComparision are private to avoid them interfering with saving
    //each has a set and get funtion to compensate for this
    //each implementation of the model has a diffrent name for the selector and value for comparison to avoid confusing the front end by having 2 or more fields with the same name.
    private string Selector { get; set; } = "";
    private string ValueForComparison { get; set; } = "";
    
    
    public virtual OnlyWithTypes type { get; set; }= OnlyWithTypes.None;

    public void SetSelector(string value)
    {
        Selector = value;
    }

    public string GetSelector()
    {
        return Selector;
    }
    public void SetValue(string value)
    {
        ValueForComparison = value;
    }

    public string GetValue()
    {
        return ValueForComparison;
    }

    //returns true of success full
    public OnlyWithTypes? SetModel(XmlNode value)
    {
        if (string.IsNullOrEmpty(value.Value))
        {
            return null;
        }
        string[] splitString = value.Value.Split(',');
        /*if (splitString.Length != 2)
        {
            //data does not follow expected pattern.
            return null;
        }*/
        Selector = splitString[0];
        ValueForComparison = splitString.Length > 1 ? splitString[1] : ""; 
        
        return type;
    }

    public XmlNode GetModel(OnlyWithTypes currentType)
    {
        if (string.IsNullOrEmpty(Selector)&&string.IsNullOrEmpty(ValueForComparison))return null;
        if (currentType != type) return null;
        return new XmlDocument().CreateCDataSection($"{Selector},{ValueForComparison}");
    }

    public bool isEmpty()
    {
        if (string.IsNullOrEmpty(Selector)&&string.IsNullOrEmpty(ValueForComparison))
        {
            return true;
        }
        return false;
    }
}