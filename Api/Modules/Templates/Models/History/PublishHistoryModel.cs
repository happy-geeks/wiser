using System;
using System.Globalization;
using Api.Modules.Templates.Models.Template;

namespace Api.Modules.Templates.Models.History
{
    public class PublishHistoryModel
    {
        public int Templateid { get; set; }
        public DateTime ChangedOn { get; set; }
        public string ChangedBy { get; set; }

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
