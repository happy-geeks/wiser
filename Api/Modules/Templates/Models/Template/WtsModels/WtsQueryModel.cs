using System.Xml.Serialization;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Api.Modules.Templates.Models.Template.WtsModels;

[XmlType("Query")]
public class WtsQueryModel : ActionModel
{
    public string Query { get; set; }

    public int? Timeout { get; set; }

    public CharacterEncodingModel CharacterEncoding { get; set; }

    public bool? UseTransaction { get; set; }
    [XmlIgnore]
    public bool UseTransactionSpecified => UseTransaction.HasValue;
}