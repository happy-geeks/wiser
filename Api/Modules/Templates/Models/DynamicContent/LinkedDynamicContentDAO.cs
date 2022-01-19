using System;

namespace Api.Modules.Templates.Models.DynamicContent
{
    public class LinkedDynamicContentDAO
    {
        public int id { get; set; }
        public string component { get; set; }
        public string component_mode { get; set; }
        public string usages { get; set; }
        public DateTime changed_on { get; set; }
        public string changed_by { get; set; }
        public string title { get; set; }
    }
}
