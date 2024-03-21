using System.Collections.Generic;
using System.Xml.Serialization;

namespace Api.Modules.Templates.Models.Template.WtsModels
{
    public class BodyPartModel
    {
        public string Text { get; set; }
        
        public bool? SingleItem { get; set; }
        [XmlIgnore]
        public bool SingleItemSpecified
        {
            get { return SingleItem.HasValue; }
        }
        
        public string UseResultSet { get; set; }
        
        public bool? ForceIndex { get; set; }
        [XmlIgnore]
        public bool ForceIndexSpecified
        {
            get { return ForceIndex.HasValue; }
        }
        
        public bool? EvaluateLogicSnippets { get; set; }
        [XmlIgnore]
        public bool EvaluateLogicSnippetsSpecified
        {
            get { return EvaluateLogicSnippets.HasValue; }
        }
    }
}