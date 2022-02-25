using System;
using System.Collections.Generic;
using System.Globalization;

namespace Api.Modules.Templates.Models.History
{
    public class TemplateHistoryModel
    {
        public int Id { get; set; }
        public int Version { get; set; }
        public DateTime ChangedOn { get; set; }
        public string ChangedBy { get; set; }

        public Dictionary<string, KeyValuePair<object, object>> TemplateChanges { get; set; }
        public Dictionary<string, KeyValuePair<object, object>> LinkedTemplateChanges { get; set; }
        public List<HistoryVersionModel> DynamicContentChanges { get; set; }

        public TemplateHistoryModel()
        {
        }

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
        
        /// <summary>
        /// Get the change date in a displayable format(DD-MM-YYYY om HH:MM:SS).
        /// </summary>
        /// <returns>A string containing a displayable date.</returns>
        public string GetDisplayChangedOn()
        {
            return ChangedOn.ToString("dd-MM-yyyy 'om' HH:mm:ss", new CultureInfo("nl-NL"));
        }
    }
}
