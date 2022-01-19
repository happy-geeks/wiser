using System;

namespace Api.Modules.Templates.Models.Preview
{
    public class PreviewVariableModel
    {
        public string type { get; set; }
        public string key { get; set; }
        public string value { get; set; }
        public Boolean encrypt { get; set; }
    }
}
