using System;
using System.Collections.Generic;
using System.Globalization;

namespace Api.Modules.Templates.Models.History
{
    /// <summary>
    /// A model representing a single change in the settings of a dynamic component.
    /// </summary>
    public class HistoryVersionModel
    {
        /// <summary>
        /// Get or sets the name.
        /// </summary>
        public string Name { get; set; }
        
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

        /// <summary>
        /// Gets or sets the raw version string.
        /// </summary>
        public string RawVersionString { get; set; }

        /// <summary>
        /// Gets or sets the name of the component.
        /// </summary>
        public string Component { get; set; }

        /// <summary>
        /// Gets or sets the name of the component mode.
        /// </summary>
        public string ComponentMode { get; set; }
        
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
