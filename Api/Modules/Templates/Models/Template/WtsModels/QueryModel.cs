using System.Xml.Serialization;

namespace Api.Modules.Templates.Models.Template.WtsModels
{
    [XmlType("Query")]
    public class QueryModel : ActionModel
    {
        public string Query { get; set; }
        
        public int? Timeout { get; set; }
        
        public CharacterEncodingModel CharacterEncoding { get; set; }
        
        public bool? UseTransaction { get; set; }
        [XmlIgnore]
        public bool UseTransactionSpecified
        {
            get { return UseTransaction.HasValue; }
        }
    }
}