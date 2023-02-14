using System;
using System.Globalization;
using Api.Modules.Templates.Models.Template;

namespace Api.Modules.Templates.Models.History
{
    /// <summary>
    /// A model to store information about the history of publishing a template to a specific environment.
    /// </summary>
    public class PublishHistoryModel
    {
        /// <summary>
        /// Gets or sets the ID of the template
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public int Templateid { get; set; }
        
        /// <summary>
        /// Gets or sets the date and time when a template has changed 
        /// </summary>
        public DateTime ChangedOn { get; set; }
        
        /// <summary>
        /// Gets or sets who changed the template
        /// </summary>
        public string ChangedBy { get; set; }

        /// <summary>
        /// Gets or sets a log of what is published
        /// </summary>
        public PublishLogModel PublishLog { get; set; }
        
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
