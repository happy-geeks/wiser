using System;
using System.Collections.Generic;
using System.Globalization;
using GeeksCoreLibrary.Modules.Templates.Enums;

namespace Api.Modules.Templates.Models.History
{
    /// <summary>
    /// A model class that contains the history of a template
    /// </summary>
    public class TemplateHistoryModel
    {
        /// <summary>
        /// Gets or sets the ID of the Template History object
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Gets or sets the version number of the Template History object
        /// </summary>
        public int Version { get; set; }
        
        /// <summary>
        /// Gets or sets the date and time when a template has been changed
        /// </summary>
        public DateTime ChangedOn { get; set; }
        
        /// <summary>
        /// Gets or sets the user which has changed a template
        /// </summary>
        public string ChangedBy { get; set; }

        /// <summary>
        /// Gets or sets a dictionary of the changes done in a template 
        /// </summary>
        public Dictionary<string, Tuple<object, object, TemplateTypes>> TemplateChanges { get; set; }
        
        /// <summary>
        /// Gets or sets a dictionary of the changes done in a template linked to the current template
        /// </summary>
        public Dictionary<string, Tuple<object, object, TemplateTypes>> LinkedTemplateChanges { get; set; }
        
        /// <summary>
        /// Gets or sets a list of changes done in the Dynamic Content of a template
        /// </summary>
        public List<HistoryVersionModel> DynamicContentChanges { get; set; }

#pragma warning disable CS1591
        public TemplateHistoryModel()
#pragma warning restore CS1591
        {
        }

        /// <summary>
        /// Constructor for the Template History with all needed info
        /// </summary>
        /// <param name="id">The ID of the Template History</param>
        /// <param name="version">The version number of the Template History</param>
        /// <param name="changedOn">The time and date of when the template is changed</param>
        /// <param name="changedBy">The name of the user who has changed the template</param>
        public TemplateHistoryModel (int id, int version, DateTime changedOn, string changedBy)
        {
            this.Id = id;
            this.Version = version;
            this.ChangedOn = changedOn;
            this.ChangedBy = changedBy;
            this.TemplateChanges = new Dictionary<string, Tuple<object, object, TemplateTypes>>();
            this.LinkedTemplateChanges = new Dictionary<string, Tuple<object, object, TemplateTypes>>();
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
