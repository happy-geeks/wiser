using System;
using System.Collections.Generic;

namespace Api.Modules.Templates.Models.Preview
{
    public class PreviewProfileModel
    {
        public Int64 id { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public List<PreviewVariableModel> variables { get; set; }
    }
}
