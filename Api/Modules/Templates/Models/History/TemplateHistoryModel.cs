using System;
using System.Collections.Generic;

namespace Api.Modules.Templates.Models.History
{
    public class TemplateHistoryModel
    {
        public int id;
        public int version;
        public DateTime changed_on;
        public string changed_by;

        public Dictionary<string, KeyValuePair<object, object>> templateChanges;
        public Dictionary<string, KeyValuePair<object, object>> linkedTemplateChanges;
        public List<HistoryVersionModel> dynamicContentChanges;

        public TemplateHistoryModel (int id, int version, DateTime changed_on, string changed_by)
        {
            this.id = id;
            this.version = version;
            this.changed_on = changed_on;
            this.changed_by = changed_by;
            this.templateChanges = new Dictionary<string, KeyValuePair<object, object>>();
            this.linkedTemplateChanges = new Dictionary<string, KeyValuePair<object, object>>();
            this.dynamicContentChanges = new List<HistoryVersionModel>();
        }
    }
}
