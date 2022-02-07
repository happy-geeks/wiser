using System;
using System.Collections.Generic;

namespace Api.Modules.Templates.Models.History
{
    public class HistoryVersionModel
    {
        /// <summary>
        /// Get or sets the version number.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Get or sets the Change date.
        /// </summary>
        public DateTime ChangedOn { get; set; }

        /// <summary>
        /// Get or sets the Name of the user that made the change.
        /// </summary>
        public string ChangedBy { get; set; }

        /// <summary>
        /// Get the list of changes compared to the previous version. This does not generated the changelist, but retrieves it from the model if set.
        /// </summary>
        public List<DynamicContentChangeModel> Changes { get; set; }

        public string RawVersionString { get; set; }

        public string Component { get; set; }

        public string ComponentMode { get; set; }

        public HistoryVersionModel(int version, DateTime changedOn, string changedBy, string component, string componentMode, string rawVersionString)
        {
            this.Version = version;
            this.ChangedOn = changedOn;
            this.ChangedBy = changedBy;
            this.Component = component;
            this.ComponentMode = componentMode;
            this.RawVersionString = rawVersionString;
            Changes = null;
        }
        
        /// <summary>
        /// Get the change date in a displayable format(DD-MM-YYYY om HH:MM:SS).
        /// </summary>
        /// <returns>A string containing a displayable date.</returns>
        public string GetDisplayChangedOn()
        {
            return ChangedOn.ToShortDateString() + " om " + ChangedOn.ToLongTimeString();
        }
    }
}
