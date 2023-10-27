using System.Xml.Serialization;

namespace Api.Modules.Templates.Models.Template.WtsModels
{
    public class HttpApiModel : ActionModel
    {
        public string Url { get; set; }
        
        public string Method { get; set; }
        
        public string OAuth { get; set; }
        
        public bool? SingleRequest { get; set; }
        [XmlIgnore]
        public bool SingleRequestSpecified
        {
            get { return SingleRequest.HasValue; }
        }
        
        public bool? IgnoreSSLValidationErrors { get; set; }
        [XmlIgnore]
        public bool IgnoreSSLValidationErrorsSpecified
        {
            get { return IgnoreSSLValidationErrors.HasValue; }
        }
        
        public string NextUrlProperty { get; set; }
        
        public HeaderModel[] Headers { get; set; }
        
        public BodyModel Body { get; set; }
        
        public string ResultContentType { get; set; }
    }
}