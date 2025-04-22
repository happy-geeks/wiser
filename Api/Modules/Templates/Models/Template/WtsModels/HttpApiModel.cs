using System.Xml.Serialization;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Api.Modules.Templates.Models.Template.WtsModels;

public class HttpApiModel : ActionModel
{
    public string Url { get; set; }
    
    public string Method { get; set; }
    
    public string OAuth { get; set; }
    
    public bool? SingleRequest { get; set; }
    
    [XmlIgnore]
    public bool SingleRequestSpecified => SingleRequest.HasValue;
    
    public bool? IgnoreSslValidationErrors { get; set; }
    
    [XmlIgnore]
    public bool IgnoreSslValidationErrorsSpecified => IgnoreSslValidationErrors.HasValue;
    
    public string NextUrlProperty { get; set; }
    
    public HeaderModel[] Headers { get; set; }
    
    public BodyModel Body { get; set; }
    
    public string ResultContentType { get; set; }
}