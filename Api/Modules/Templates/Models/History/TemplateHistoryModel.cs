using System;
using System.Collections.Generic;

namespace Api.Modules.Templates.Models.History
{
    public class TemplateHistoryModel
    {
        public int Id;
        public int Version;
        public DateTime ChangedOn;
        public string ChangedBy;

        public Dictionary<string, KeyValuePair<object, object>> TemplateChanges;
        public Dictionary<string, KeyValuePair<object, object>> LinkedTemplateChanges;
        public List<HistoryVersionModel> DynamicContentChanges;

        public TemplateHistoryModel (int id, int version, DateTime changedOn, string changedBy)
        {
            this.Id = id;
            this.Version = version;
            this.ChangedOn = changedOn;
            this.ChangedBy = changedBy;
            this.TemplateChanges = new Dictionary<string, KeyValuePair<object, object>>();
            this.LinkedTemplateChanges = new Dictionary<string, KeyValuePair<object, object>>();
            this.DynamicContentChanges = new List<HistoryVersionModel>();
        }
    }
}
