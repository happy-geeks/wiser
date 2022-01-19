using System;
using Api.Modules.Templates.Models.Template;

namespace Api.Modules.Templates.Models.History
{
    public class PublishHistoryModel
    {
        public int templateid { get; set; }
        public DateTime changed_on { get; set; }
        public string changed_by { get; set; }

        public PublishLogModel publishLog { get; set; }
    }
}
