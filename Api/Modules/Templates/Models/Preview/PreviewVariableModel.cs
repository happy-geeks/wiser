using System;

namespace Api.Modules.Templates.Models.Preview
{
    public class PreviewVariableModel
    {
        public string Type { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public Boolean Encrypt { get; set; }
    }
}
