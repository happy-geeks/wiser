using System;
using Api.Modules.Templates.Models.Template;

namespace Api.Modules.Templates.Models.History
{
    public class PublishHistoryModel
    {
        public int Templateid { get; set; }
        public DateTime ChangedOn { get; set; }
        public string ChangedBy { get; set; }

        public PublishLogModel PublishLog { get; set; }
    }
}
