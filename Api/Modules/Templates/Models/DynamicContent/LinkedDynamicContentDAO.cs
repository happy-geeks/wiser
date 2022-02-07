using System;

namespace Api.Modules.Templates.Models.DynamicContent
{
    public class LinkedDynamicContentDao
    {
        public int Id { get; set; }
        public string Component { get; set; }
        public string ComponentMode { get; set; }
        public string Usages { get; set; }
        public DateTime ChangedOn { get; set; }
        public string ChangedBy { get; set; }
        public string Title { get; set; }
    }
}
