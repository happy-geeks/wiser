using System.Collections.Generic;

namespace Api.Modules.Templates.Models.Template.WtsModels
{
    public class BodyModel
    {
        public string ContentType { get; set; }
        
        public List<BodyPartModel> BodyParts { get; set; }
    }
}