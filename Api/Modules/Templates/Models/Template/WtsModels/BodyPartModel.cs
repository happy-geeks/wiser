using System.Xml.Serialization;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Api.Modules.Templates.Models.Template.WtsModels;

public class BodyPartModel
{
    public string Text { get; set; }

    public bool? SingleItem { get; set; }
    [XmlIgnore]
    public bool SingleItemSpecified => SingleItem.HasValue;

    public string UseResultSet { get; set; }

    public bool? ForceIndex { get; set; }
    [XmlIgnore]
    public bool ForceIndexSpecified => ForceIndex.HasValue;

    public bool? EvaluateLogicSnippets { get; set; }
    [XmlIgnore]
    public bool EvaluateLogicSnippetsSpecified => EvaluateLogicSnippets.HasValue;
}