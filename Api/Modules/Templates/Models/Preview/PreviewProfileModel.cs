using System.Collections.Generic;

namespace Api.Modules.Templates.Models.Preview
{
    public class PreviewProfileModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public List<PreviewVariableModel> Variables { get; set; }
    }
}
